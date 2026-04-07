#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const removeOverride = require('./remove-override.js');

function parseArgs() {
  const args = {};
  process.argv.slice(2).forEach((arg) => {
    if (arg.startsWith('--')) {
      const eq = arg.indexOf('=');
      if (eq === -1) {
        args[arg.slice(2)] = true;
      } else {
        const key = arg.slice(2, eq);
        const val = arg.slice(eq + 1);
        args[key] = val;
      }
    }
  });
  return args;
}

function getRepoRemote() {
  try {
    const out = cp
      .execSync('git remote get-url origin', { encoding: 'utf8' })
      .trim();
    return out;
  } catch (err) {
    return null;
  }
}

function parseOwnerRepo(remoteUrl) {
  if (!remoteUrl) return null;
  const m = remoteUrl.match(/github\.com[:\\/](.+?)\/(.+?)(?:\.git)?$/i);
  if (m) return { owner: m[1], repo: m[2] };
  return null;
}

function fetchDependabotAlertsWithGh(owner, repo) {
  try {
    const cmd = `gh api /repos/${owner}/${repo}/dependabot/alerts?per_page=100 --paginate`;
    const out = cp.execSync(cmd, { encoding: 'utf8' });
    return JSON.parse(out);
  } catch (err) {
    // gh not available or failed
    return null;
  }
}

function fetchDependabotAlerts(owner, repo) {
  const ghOut = fetchDependabotAlertsWithGh(owner, repo);
  if (ghOut) return ghOut;
  // Fallback: if GITHUB_TOKEN exists, try making a minimal REST call using node https (simple, one page)
  const token = process.env.GITHUB_TOKEN || process.env.WRITE_TOKEN;
  if (!token) return [];

  const https = require('https');
  const opts = {
    hostname: 'api.github.com',
    path: `/repos/${owner}/${repo}/dependabot/alerts?per_page=100`,
    method: 'GET',
    headers: {
      'User-Agent': 'nx-tinkering-monitor/1.0',
      Accept: 'application/vnd.github+json',
      Authorization: `Bearer ${token}`,
    },
  };

  return new Promise((resolve) => {
    const req = https.request(opts, (res) => {
      let data = '';
      res.setEncoding('utf8');
      res.on('data', (chunk) => (data += chunk));
      res.on('end', () => {
        try {
          const json = JSON.parse(data || '[]');
          resolve(json);
        } catch (err) {
          resolve([]);
        }
      });
    });
    req.on('error', () => resolve([]));
    req.end();
  });
}

function mapAlertToCandidate(alert, pkgJsonPaths, repoRoot) {
  const pkgName =
    alert &&
    alert.dependency &&
    alert.dependency.package &&
    alert.dependency.package.name;
  if (!pkgName) return null;
  const root = repoRoot || process.cwd();
  const paths =
    Array.isArray(pkgJsonPaths) && pkgJsonPaths.length > 0
      ? pkgJsonPaths
      : removeOverride.findPackageJsons(root);
  const candidates = [];
  for (const p of paths) {
    try {
      const pj = JSON.parse(fs.readFileSync(p, 'utf8'));
      const deps = Object.assign(
        {},
        pj.dependencies || {},
        pj.devDependencies || {},
        pj.optionalDependencies || {},
        pj.peerDependencies || {},
      );
      if (deps && Object.prototype.hasOwnProperty.call(deps, pkgName)) {
        candidates.push({ path: p, reason: 'declares-dependency' });
        continue;
      }
      if (
        pj.overrides &&
        Object.prototype.hasOwnProperty.call(pj.overrides, pkgName)
      ) {
        candidates.push({ path: p, reason: 'already-overrides' });
        continue;
      }
      // Prefer any package.json that already has overrides section (for adding new overrides there)
      if (pj.overrides && Object.keys(pj.overrides).length > 0) {
        candidates.push({ path: p, reason: 'has-overrides' });
      }
    } catch (err) {
      // ignore parse errors
    }
  }

  if (candidates.length > 0) {
    // choose deepest path (most specific)
    candidates.sort(
      (a, b) => b.path.split(path.sep).length - a.path.split(path.sep).length,
    );
    return candidates[0];
  }

  // fallback: repo root package.json if present
  const repoRootPkg = paths.find(
    (p) => path.resolve(p) === path.resolve(path.join(root, 'package.json')),
  );
  if (repoRootPkg) return { path: repoRootPkg, reason: 'repo-root-fallback' };

  // final fallback
  return { path: paths[0], reason: 'first-found-fallback' };
}

function determineDependencyKind(alert, candidatePath) {
  const pkgName =
    alert &&
    alert.dependency &&
    alert.dependency.package &&
    alert.dependency.package.name;
  if (!pkgName || !candidatePath || !fs.existsSync(candidatePath))
    return 'unknown';

  try {
    const pj = JSON.parse(fs.readFileSync(candidatePath, 'utf8'));
    const sections = [
      'dependencies',
      'devDependencies',
      'optionalDependencies',
      'peerDependencies',
    ];
    for (const section of sections) {
      if (
        pj[section] &&
        Object.prototype.hasOwnProperty.call(pj[section], pkgName)
      ) {
        return 'direct';
      }
    }
    if (
      pj.overrides &&
      Object.prototype.hasOwnProperty.call(pj.overrides, pkgName)
    ) {
      return 'transitive';
    }
  } catch (err) {
    return 'unknown';
  }

  return 'unknown';
}

function buildPlanEntries(alerts, pkgJsonPaths, repoRoot) {
  const entries = [];
  for (const a of alerts) {
    const candidate = mapAlertToCandidate(a, pkgJsonPaths, repoRoot);
    const dependencyKind = determineDependencyKind(
      a,
      candidate ? candidate.path : null,
    );
    entries.push({
      advisory_url:
        a.html_url ||
        (a.security_advisory &&
          a.security_advisory.references &&
          a.security_advisory.references[0] &&
          a.security_advisory.references[0].url) ||
        null,
      package:
        (a.dependency && a.dependency.package && a.dependency.package.name) ||
        null,
      vulnerable_range: a.vulnerable_version_range || null,
      candidate_path: candidate ? candidate.path : null,
      reason: candidate ? candidate.reason : 'no-candidate',
      dependency_kind: dependencyKind,
    });
  }
  return entries;
}

function tryParseJson(maybeJson) {
  try {
    return JSON.parse(maybeJson);
  } catch (err) {
    return null;
  }
}

function runCommand(command, cwd) {
  try {
    const out = cp.execSync(command, { cwd, encoding: 'utf8' });
    return { ok: true, command, stdout: out || '', stderr: '' };
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

function runNpmLsCheck(cwd) {
  const res = runCommand('npm ls --all --json', cwd);
  const parsed = tryParseJson(res.stdout) || tryParseJson(res.stderr);
  const problems =
    parsed && Array.isArray(parsed.problems) ? parsed.problems : [];
  return {
    ok: res.ok && problems.length === 0,
    command: res.command,
    problems,
    stdout: res.stdout,
    stderr: res.stderr,
    error: res.error || null,
  };
}

function compareVersionStrings(a, b) {
  const clean = (v) =>
    String(v || '')
      .split('+')[0]
      .split('-')[0];
  const pa = clean(a)
    .split('.')
    .map((n) => Number.parseInt(n, 10));
  const pb = clean(b)
    .split('.')
    .map((n) => Number.parseInt(n, 10));
  const len = Math.max(pa.length, pb.length);

  for (let i = 0; i < len; i += 1) {
    const av = Number.isFinite(pa[i]) ? pa[i] : 0;
    const bv = Number.isFinite(pb[i]) ? pb[i] : 0;
    if (av > bv) return 1;
    if (av < bv) return -1;
  }

  // Keep ordering stable for non-standard versions.
  if (String(a) > String(b)) return 1;
  if (String(a) < String(b)) return -1;
  return 0;
}

function extractPackageNameFromLockPath(pkgPath) {
  if (!pkgPath || typeof pkgPath !== 'string') return null;
  const marker = 'node_modules/';
  const idx = pkgPath.lastIndexOf(marker);
  if (idx === -1) return null;
  const rest = pkgPath.slice(idx + marker.length);
  if (!rest) return null;

  const parts = rest.split('/');
  if (parts[0] && parts[0].startsWith('@') && parts.length > 1) {
    return `${parts[0]}/${parts[1]}`;
  }
  return parts[0] || null;
}

function detectHigherVersionSelectionWarnings(lockJson) {
  if (!lockJson || typeof lockJson !== 'object') {
    return { ok: true, total_conflicts: 0, warnings: [] };
  }

  const versionsByName = new Map();
  const selectedTopLevelVersion = new Map();

  if (lockJson.packages && typeof lockJson.packages === 'object') {
    for (const [pkgPath, meta] of Object.entries(lockJson.packages)) {
      if (!meta || typeof meta !== 'object' || !meta.version) continue;
      const name = extractPackageNameFromLockPath(pkgPath);
      if (!name) continue;

      if (!versionsByName.has(name)) versionsByName.set(name, new Set());
      versionsByName.get(name).add(String(meta.version));

      if (/^node_modules\/(?:@[^/]+\/)?[^/]+$/.test(pkgPath)) {
        selectedTopLevelVersion.set(name, String(meta.version));
      }
    }
  }

  const warnings = [];
  for (const [name, versionsSet] of versionsByName.entries()) {
    const versions = Array.from(versionsSet);
    if (versions.length < 2) continue;

    versions.sort((a, b) => compareVersionStrings(a, b));
    const lowest = versions[0];
    const highest = versions[versions.length - 1];
    const selected = selectedTopLevelVersion.get(name) || highest;
    const higherPicked = compareVersionStrings(selected, lowest) > 0;

    if (higherPicked && compareVersionStrings(selected, highest) === 0) {
      warnings.push({
        package: name,
        selected_version: selected,
        lower_versions: versions.filter(
          (v) => compareVersionStrings(v, selected) < 0,
        ),
        all_versions: versions,
        message: `Conflicting versions detected for ${name}; higher version ${selected} selected.`,
      });
    }
  }

  return {
    ok: true,
    total_conflicts: warnings.length,
    warnings,
  };
}

function writeReports(entries, root) {
  const repoRoot = root || process.cwd();
  const jsonPath = path.join(repoRoot, 'dependabot-override-plan.json');
  const txtPath = path.join(repoRoot, 'dependabot-override-plan.txt');
  fs.writeFileSync(jsonPath, JSON.stringify(entries, null, 2), 'utf8');
  const lines = entries.map(
    (e) =>
      `package: ${e.package} -> candidate: ${e.candidate_path} (reason: ${e.reason})\n  advisory: ${e.advisory_url || '(none)'}\n  vulnerable_range: ${e.vulnerable_range || '(none)'}\n`,
  );
  fs.writeFileSync(txtPath, lines.join('\n'), 'utf8');
  return { jsonPath, txtPath };
}

function isPlainObject(value) {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function formatOverridePath(pathSegments) {
  if (!Array.isArray(pathSegments) || pathSegments.length === 0) {
    return null;
  }
  const [head, ...tail] = pathSegments;
  let out = String(head);
  for (const segment of tail) {
    out += `[${JSON.stringify(segment)}]`;
  }
  return out;
}

function removeNestedOverrideByPackageName(overrides, pkgName) {
  if (!isPlainObject(overrides) || !pkgName) {
    return { removed: false, removedPath: null };
  }

  function visit(node, trail) {
    if (!isPlainObject(node)) return null;

    if (Object.prototype.hasOwnProperty.call(node, pkgName)) {
      delete node[pkgName];
      return [...trail, pkgName];
    }

    for (const key of Object.keys(node)) {
      const child = node[key];
      if (!isPlainObject(child)) continue;

      const removedTrail = visit(child, [...trail, key]);
      if (removedTrail) {
        if (isPlainObject(child) && Object.keys(child).length === 0) {
          delete node[key];
        }
        return removedTrail;
      }
    }

    return null;
  }

  const removedPath = visit(overrides, ['overrides']);
  return {
    removed: !!removedPath,
    removedPath: formatOverridePath(removedPath),
  };
}

function trialRemoveOverride(entry, options) {
  const os = require('os');
  const tmpBase = options && options.tmpBase ? options.tmpBase : os.tmpdir();
  const mkd = fs.mkdtempSync(path.join(tmpBase, 'override-trial-'));
  const candidatePkg = entry && entry.candidate_path;
  if (!candidatePkg || !fs.existsSync(candidatePkg)) {
    return { ok: false, reason: 'candidate-missing', entry };
  }
  const original = JSON.parse(fs.readFileSync(candidatePkg, 'utf8'));
  const pkgName = entry && entry.package;
  if (!pkgName) return { ok: false, reason: 'package-missing' };

  const modified = JSON.parse(JSON.stringify(original));
  const removal = removeNestedOverrideByPackageName(
    modified.overrides,
    pkgName,
  );
  if (!removal.removed) {
    return { ok: false, reason: 'no-override-present', entry };
  }
  if (modified.overrides && Object.keys(modified.overrides).length === 0) {
    delete modified.overrides;
  }

  // write modified package.json to temp dir
  fs.writeFileSync(
    path.join(mkd, 'package.json'),
    JSON.stringify(modified, null, 2),
    'utf8',
  );

  const result = {
    tmpdir: mkd,
    logs: '',
    ok: false,
    lockfile: null,
    checks: {
      install: { ok: false, command: 'npm install --package-lock-only' },
      dedup: { ok: false, command: 'npm dedup' },
      npmLs: { ok: false, command: 'npm ls --all --json', problems: [] },
      versionConflicts: { ok: true, total_conflicts: 0, warnings: [] },
      tests: { ok: true, skipped: true },
    },
    dependency_kind:
      entry && entry.dependency_kind ? entry.dependency_kind : 'unknown',
    removed_override_path: removal.removedPath,
  };
  try {
    const installCmd = 'npm install --package-lock-only';
    result.logs += `Running: ${installCmd}\n`;
    const installRes = runCommand(installCmd, mkd);
    result.checks.install = installRes;
    result.logs += installRes.stdout || '';
    if (installRes.stderr) {
      result.logs += '\n' + installRes.stderr;
    }
    if (!installRes.ok) {
      result.logs +=
        '\nERROR:\n' + (installRes.error || 'install failed') + '\n';
      result.ok = false;
      return result;
    }

    result.ok = true;

    const dedupCmd = 'npm dedup';
    result.logs += `\nRunning: ${dedupCmd}\n`;
    const dedupRes = runCommand(dedupCmd, mkd);
    result.checks.dedup = dedupRes;
    result.logs += dedupRes.stdout || '';
    if (dedupRes.stderr) {
      result.logs += '\n' + dedupRes.stderr;
    }
    if (!dedupRes.ok) {
      result.logs += '\nERROR:\n' + (dedupRes.error || 'dedup failed') + '\n';
      result.ok = false;
    }

    const npmLsRes = runNpmLsCheck(mkd);
    result.checks.npmLs = npmLsRes;
    if (npmLsRes.problems.length > 0) {
      result.logs +=
        '\nNPM_LS_PROBLEMS:\n' + npmLsRes.problems.join('\n') + '\n';
      result.ok = false;
    }
    if (!npmLsRes.ok) {
      if (npmLsRes.error || npmLsRes.stdout || npmLsRes.stderr) {
        result.logs +=
          '\nNPM_LS_FAILED:\n' +
          (npmLsRes.error || 'npm ls failed') +
          '\n' +
          (npmLsRes.stdout || '') +
          '\n' +
          (npmLsRes.stderr || '') +
          '\n';
      }
      result.ok = false;
    }

    const lockPath = path.join(mkd, 'package-lock.json');
    if (fs.existsSync(lockPath)) {
      result.lockfile = fs.readFileSync(lockPath, 'utf8');
      const parsedLock = tryParseJson(result.lockfile);
      const conflictCheck = detectHigherVersionSelectionWarnings(parsedLock);
      result.checks.versionConflicts = conflictCheck;
      if (conflictCheck.total_conflicts > 0) {
        result.logs += '\nVERSION_CONFLICT_WARNINGS:\n';
        for (const warning of conflictCheck.warnings) {
          result.logs += `- ${warning.message} Lower versions: ${warning.lower_versions.join(', ')}\n`;
        }
      }
    } else {
      result.ok = false;
    }

    // run smoke tests if package.json declares a test script
    if (modified.scripts && modified.scripts.test) {
      result.checks.tests = {
        ok: true,
        skipped: false,
        command: 'npm test --silent',
      };
      try {
        const testOut = cp.execSync('npm test --silent', {
          cwd: mkd,
          encoding: 'utf8',
        });
        result.logs += '\nTESTS:\n' + (testOut || '');
      } catch (err) {
        result.checks.tests.ok = false;
        result.checks.tests.error =
          err && err.message ? String(err.message) : 'tests failed';
        result.logs +=
          '\nTESTS_FAILED:\n' +
          (err && err.message) +
          '\n' +
          (err && err.stdout ? err.stdout : '') +
          '\n' +
          (err && err.stderr ? err.stderr : '');
        result.ok = false;
      }
    }
  } catch (err) {
    result.logs += '\nERROR:\n' + (err && err.message) + '\n';
    if (err && err.stdout) result.logs += '\n' + String(err.stdout);
    if (err && err.stderr) result.logs += '\n' + String(err.stderr);
    result.ok = false;
  }
  return result;
}

async function main() {
  const args = parseArgs();
  const dryRun =
    typeof args['dry-run'] !== 'undefined'
      ? String(args['dry-run']).toLowerCase() === 'true'
      : true;
  const repoRoot = process.cwd();

  const remote = getRepoRemote();
  const parsed = parseOwnerRepo(remote);
  if (!parsed) {
    console.error('Could not determine repo owner/repo from git remote.');
    process.exit(2);
  }

  let alerts = await fetchDependabotAlerts(parsed.owner, parsed.repo);
  if (!alerts) alerts = [];
  if (Array.isArray(alerts) && alerts.length === 0) {
    console.log('No Dependabot alerts found');
  }
  // If fetchDependabotAlerts returned a Promise (from REST fallback), await
  if (typeof alerts.then === 'function') {
    alerts = await alerts;
  }

  const pkgJsons = removeOverride.findPackageJsons(repoRoot);
  const entries = buildPlanEntries(alerts, pkgJsons, repoRoot);
  const written = writeReports(entries, repoRoot);
  console.log(
    `Wrote plan JSON to ${written.jsonPath} and text to ${written.txtPath}`,
  );

  if (!dryRun) {
    console.log(
      'LIVE mode requested — PR creation not implemented in this prototype.',
    );
  } else {
    console.log('DRY_RUN mode — no branches or PRs created.');
  }

  // Trial-removal mode: attempt to remove overrides and regenerate lockfiles
  if (args['trial']) {
    const limit = parseInt(args['limit'] || args['trial-limit'] || '5', 10);
    const trialDir = path.join(repoRoot, 'trial-results');
    fs.mkdirSync(trialDir, { recursive: true });
    const trialSummary = [];
    let count = 0;
    for (const e of entries) {
      if (count >= limit) break;
      console.log(
        `Trial removing override for ${e.package} (candidate: ${e.candidate_path})`,
      );
      const res = trialRemoveOverride(e, {});
      const outFile = path.join(
        trialDir,
        (e.package || 'unknown').replace(/[/\\:@]/g, '-') + '.json',
      );
      fs.writeFileSync(outFile, JSON.stringify(res, null, 2), 'utf8');
      trialSummary.push({
        package: e.package,
        candidate: e.candidate_path,
        ok: !!res.ok,
        reason: res.reason || null,
        dependency_kind: res.dependency_kind || e.dependency_kind || 'unknown',
        removed_override_path: res.removed_override_path || null,
        checks: {
          install: !!(
            res.checks &&
            res.checks.install &&
            res.checks.install.ok
          ),
          dedup: !!(res.checks && res.checks.dedup && res.checks.dedup.ok),
          npmLs: !!(res.checks && res.checks.npmLs && res.checks.npmLs.ok),
          tests: res.checks && res.checks.tests ? !!res.checks.tests.ok : null,
          version_conflicts:
            res.checks && res.checks.versionConflicts
              ? res.checks.versionConflicts.total_conflicts
              : null,
        },
        tmpdir: res.tmpdir || null,
      });
      count += 1;
    }
    fs.writeFileSync(
      path.join(trialDir, 'summary.json'),
      JSON.stringify(trialSummary, null, 2),
      'utf8',
    );
    console.log(`Wrote trial results to ${trialDir} (limit ${limit})`);
  }

  return 0;
}

if (require.main === module) {
  main().catch((err) => {
    console.error(err);
    process.exit(2);
  });
} else {
  function generateDraftPrsDryRun(entries, outDir, repoRoot) {
    const root = repoRoot || process.cwd();
    const base = path.join(root, outDir || 'pr-previews');
    fs.mkdirSync(base, { recursive: true });
    for (const e of entries) {
      const pkg = e.package || 'unknown-package';
      const safeName = pkg.replace(/[/\\:@]/g, '-');
      const dir = path.join(base, `${safeName}`);
      fs.mkdirSync(dir, { recursive: true });
      const title = `chore(security): temporary override for ${pkg}`;
      const bodyLines = [];
      bodyLines.push(
        `This draft PR pins ${pkg} to a temporary override to mitigate ${e.advisory_url || 'a security advisory'}.`,
      );
      bodyLines.push('');
      bodyLines.push(`Candidate package.json: ${e.candidate_path}`);
      bodyLines.push('');
      bodyLines.push(
        'Monitor: This override is temporary — monitored by .github/workflows/monitor-overrides.yml. The monitor will periodically attempt to remove this override and open a follow-up PR to remove it when it is no longer needed.',
      );
      if (e.vulnerable_range)
        bodyLines.push(`Vulnerable range: ${e.vulnerable_range}`);
      fs.writeFileSync(path.join(dir, 'pr-title.txt'), title, 'utf8');
      fs.writeFileSync(
        path.join(dir, 'pr-body.md'),
        bodyLines.join('\n'),
        'utf8',
      );
    }
    return base;
  }

  module.exports = {
    mapAlertToCandidate,
    determineDependencyKind,
    detectHigherVersionSelectionWarnings,
    removeNestedOverrideByPackageName,
    buildPlanEntries,
    fetchDependabotAlerts,
    writeReports,
    generateDraftPrsDryRun,
    trialRemoveOverride,
    main,
  };
}
