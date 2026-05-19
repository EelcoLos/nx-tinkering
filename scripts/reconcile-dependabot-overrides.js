#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const {
  removeNestedOverrideByPackageName,
} = require('./process-dependabot-alerts');

function parseArgs() {
  const args = {};
  const argv = process.argv.slice(2);
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (!arg.startsWith('--')) continue;
    const eq = arg.indexOf('=');
    if (eq === -1) {
      const key = arg.slice(2);
      const next = argv[i + 1];
      if (next && !next.startsWith('--')) {
        args[key] = next;
        i += 1;
      } else {
        args[key] = true;
      }
      continue;
    }
    args[arg.slice(2, eq)] = arg.slice(eq + 1);
  }
  return args;
}

function tryParseJson(text) {
  if (typeof text !== 'string' || !text.trim()) return null;
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function getDependencyName(entry) {
  if (!entry || typeof entry !== 'object') return null;
  const candidates = [
    entry['dependency-name'],
    entry.dependency_name,
    entry['package-name'],
    entry.package_name,
    entry.name,
    entry.package,
    entry.dependency &&
      entry.dependency.package &&
      entry.dependency.package.name,
  ];
  for (const candidate of candidates) {
    if (typeof candidate === 'string' && candidate.trim()) {
      return candidate.trim();
    }
  }
  return null;
}

function collectUpdatedDependencyNames(updatedDependenciesJson, dependencyNames) {
  const names = new Set();

  const addName = (value) => {
    if (typeof value === 'string' && value.trim()) {
      names.add(value.trim());
    }
  };

  const addFromArray = (arr) => {
    if (!Array.isArray(arr)) return;
    for (const entry of arr) {
      const name = getDependencyName(entry);
      if (name) addName(name);
    }
  };

  const parsedUpdatedDeps = tryParseJson(updatedDependenciesJson);
  if (Array.isArray(parsedUpdatedDeps)) {
    addFromArray(parsedUpdatedDeps);
  } else if (parsedUpdatedDeps && typeof parsedUpdatedDeps === 'object') {
    addFromArray([parsedUpdatedDeps]);
  }

  if (typeof dependencyNames === 'string' && dependencyNames.trim()) {
    for (const name of dependencyNames.split(',')) {
      addName(name);
    }
  } else if (Array.isArray(dependencyNames)) {
    for (const name of dependencyNames) addName(name);
  }

  return Array.from(names);
}

function runCommand(command, cwd) {
  try {
    const stdout = cp.execSync(command, {
      cwd,
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    return { ok: true, command, stdout: stdout || '', stderr: '' };
  } catch (err) {
    return {
      ok: false,
      command,
      stdout: err && err.stdout ? String(err.stdout) : '',
      stderr: err && err.stderr ? String(err.stderr) : '',
      error: err && err.message ? String(err.message) : 'command failed',
    };
  }
}

function writeJsonFile(filePath, value) {
  fs.writeFileSync(filePath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
}

function summarizeAuditResult(raw) {
  const vulnerabilities =
    raw &&
    raw.metadata &&
    raw.metadata.vulnerabilities &&
    typeof raw.metadata.vulnerabilities === 'object'
      ? raw.metadata.vulnerabilities
      : null;

  if (!vulnerabilities) {
    return {
      ok: false,
      total: null,
      vulnerabilities: null,
    };
  }

  const total = Number.parseInt(String(vulnerabilities.total || 0), 10);
  return {
    ok: Number.isFinite(total),
    total: Number.isFinite(total) ? total : null,
    vulnerabilities,
  };
}

function runNpmAudit(cwd) {
  const res = runCommand('npm audit --json', cwd);
  const parsed = tryParseJson(res.stdout) || tryParseJson(res.stderr);
  const summary = summarizeAuditResult(parsed);
  return {
    ok: summary.ok,
    command: res.command,
    stdout: res.stdout,
    stderr: res.stderr,
    error: res.error || null,
    raw: parsed,
    summary,
  };
}

function removeMatchingOverrides(overrides, dependencyNames) {
  if (!overrides || typeof overrides !== 'object' || Array.isArray(overrides)) {
    return [];
  }

  const removed = [];
  const seen = new Set();
  for (const dependencyName of dependencyNames) {
    if (seen.has(dependencyName)) continue;
    seen.add(dependencyName);

    const result = removeNestedOverrideByPackageName(overrides, dependencyName);
    if (result.removed) {
      removed.push({
        dependencyName,
        removedPath: result.removedPath,
      });
    }
  }

  return removed;
}

function runNxValidation(cwd) {
  const checks = {};
  for (const target of ['build', 'test', 'lint']) {
    const res = runCommand(`npx nx affected --target=${target}`, cwd);
    checks[target] = res;
    if (!res.ok) {
      return {
        ok: false,
        checks,
        failedTarget: target,
      };
    }
  }

  return {
    ok: true,
    checks,
    failedTarget: null,
  };
}

function restoreWorkspace(snapshot, cwd) {
  fs.writeFileSync(snapshot.packageJsonPath, snapshot.packageJson, 'utf8');
  const hasPackageLockSnapshot = typeof snapshot.packageLock === 'string';
  if (snapshot.packageLockPath) {
    if (hasPackageLockSnapshot) {
      fs.writeFileSync(snapshot.packageLockPath, snapshot.packageLock, 'utf8');
    } else if (fs.existsSync(snapshot.packageLockPath)) {
      fs.unlinkSync(snapshot.packageLockPath);
    }
  }

  const restore = runCommand(
    hasPackageLockSnapshot ? 'npm ci' : 'npm install --no-package-lock',
    cwd,
  );
  if (!restore.ok) {
    throw new Error(
      `Failed to restore workspace: ${restore.error || restore.stderr || restore.stdout}`,
    );
  }
}

function buildReport(reportPath, report) {
  fs.mkdirSync(path.dirname(reportPath), { recursive: true });
  writeJsonFile(reportPath, report);
}

async function main() {
  const args = parseArgs();
  const repoRoot = process.cwd();
  const packageJsonPath = path.resolve(
    repoRoot,
    args['package-json-path'] || 'package.json',
  );
  const packageLockPath = path.resolve(
    path.dirname(packageJsonPath),
    args['package-lock-path'] || 'package-lock.json',
  );
  const reportPath = path.resolve(
    repoRoot,
    args['report-path'] || path.join('tmp', 'dependabot-override-report.json'),
  );

  const updatedDependenciesJson =
    args['updated-dependencies-json'] ||
    process.env.UPDATED_DEPS_JSON ||
    process.env.UPDATED_DEPS ||
    '';
  const dependencyNames =
    args['dependency-names'] || process.env.DEPENDENCY_NAMES || '';

  const updatedDependencyNames = collectUpdatedDependencyNames(
    updatedDependenciesJson,
    dependencyNames,
  );

  const report = {
    applied: false,
    reverted: false,
    reason: 'no-updated-dependencies',
    packageJsonPath: path.relative(repoRoot, packageJsonPath),
    packageLockPath: path.relative(repoRoot, packageLockPath),
    updatedDependencyNames,
    removedOverrides: [],
    auditBefore: null,
    auditAfter: null,
    validation: null,
  };

  if (!updatedDependencyNames.length) {
    buildReport(reportPath, report);
    console.log('No Dependabot dependency names were provided.');
    return 0;
  }

  if (!fs.existsSync(packageJsonPath)) {
    report.reason = 'package-json-missing';
    buildReport(reportPath, report);
    console.log('Root package.json not found; skipping override reconciliation.');
    return 0;
  }

  const originalPackageJsonText = fs.readFileSync(packageJsonPath, 'utf8');
  const originalPackageJson = JSON.parse(originalPackageJsonText);
  const originalPackageLock = fs.existsSync(packageLockPath)
    ? fs.readFileSync(packageLockPath, 'utf8')
    : null;

  if (!originalPackageJson.overrides) {
    report.reason = 'no-overrides-present';
    buildReport(reportPath, report);
    console.log('package.json has no overrides section; nothing to reconcile.');
    return 0;
  }

  const baselineAudit = runNpmAudit(repoRoot);
  report.auditBefore = baselineAudit.summary;

  const modifiedPackageJson = JSON.parse(originalPackageJsonText);
  const removedOverrides = removeMatchingOverrides(
    modifiedPackageJson.overrides,
    updatedDependencyNames,
  );

  if (modifiedPackageJson.overrides &&
    Object.keys(modifiedPackageJson.overrides).length === 0) {
    delete modifiedPackageJson.overrides;
  }

  if (!removedOverrides.length) {
    report.reason = 'no-matching-overrides';
    buildReport(reportPath, report);
    console.log('No matching overrides were found for the Dependabot update.');
    return 0;
  }

  writeJsonFile(packageJsonPath, modifiedPackageJson);

  const installRes = runCommand('npm i', repoRoot);
  if (!installRes.ok) {
    restoreWorkspace(
      {
        packageJsonPath,
        packageJson: originalPackageJsonText,
        packageLockPath,
        packageLock: originalPackageLock,
      },
      repoRoot,
    );
    report.reverted = true;
    report.reason = 'npm-install-failed';
    report.removedOverrides = removedOverrides;
    buildReport(reportPath, report);
    console.log('npm i failed after removing overrides; changes were reverted.');
    return 0;
  }

  const dedupRes = runCommand('npm dedup', repoRoot);
  if (!dedupRes.ok) {
    restoreWorkspace(
      {
        packageJsonPath,
        packageJson: originalPackageJsonText,
        packageLockPath,
        packageLock: originalPackageLock,
      },
      repoRoot,
    );
    report.reverted = true;
    report.reason = 'npm-dedup-failed';
    report.removedOverrides = removedOverrides;
    buildReport(reportPath, report);
    console.log('npm dedup failed after removing overrides; changes were reverted.');
    return 0;
  }

  const validation = runNxValidation(repoRoot);
  report.validation = validation;
  if (!validation.ok) {
    restoreWorkspace(
      {
        packageJsonPath,
        packageJson: originalPackageJsonText,
        packageLockPath,
        packageLock: originalPackageLock,
      },
      repoRoot,
    );
    report.reverted = true;
    report.reason = `nx-${validation.failedTarget}-failed`;
    report.removedOverrides = removedOverrides;
    buildReport(reportPath, report);
    console.log(
      `Nx ${validation.failedTarget} failed after removing overrides; changes were reverted.`,
    );
    return 0;
  }

  const postAudit = runNpmAudit(repoRoot);
  report.auditAfter = postAudit.summary;
  report.removedOverrides = removedOverrides;

  if (
    !postAudit.ok ||
    !baselineAudit.ok ||
    postAudit.summary.total === null ||
    baselineAudit.summary.total === null ||
    postAudit.summary.total > baselineAudit.summary.total
  ) {
    restoreWorkspace(
      {
        packageJsonPath,
        packageJson: originalPackageJsonText,
        packageLockPath,
        packageLock: originalPackageLock,
      },
      repoRoot,
    );
    report.reverted = true;
    report.reason = 'audit-regressed';
    buildReport(reportPath, report);
    console.log(
      'Audit regressed after removing overrides; changes were reverted.',
    );
    return 0;
  }

  report.applied = true;
  report.reason = 'override-removed';
  buildReport(reportPath, report);
  console.log(
    `Removed overrides for ${removedOverrides
      .map((entry) => entry.dependencyName)
      .join(', ')}.`,
  );
  return 0;
}

if (require.main === module) {
  main().catch((err) => {
    console.error(err);
    process.exit(2);
  });
} else {
  module.exports = {
    parseArgs,
    tryParseJson,
    getDependencyName,
    collectUpdatedDependencyNames,
    runCommand,
    summarizeAuditResult,
    runNpmAudit,
    removeMatchingOverrides,
    runNxValidation,
    restoreWorkspace,
    main,
  };
}
