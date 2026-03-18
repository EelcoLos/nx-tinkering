#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const removeOverride = require('./remove-override.js');

function parseArgs() {
  const args = {};
  process.argv.slice(2).forEach(arg => {
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
    const out = cp.execSync('git remote get-url origin', { encoding: 'utf8' }).trim();
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
      'Accept': 'application/vnd.github+json',
      'Authorization': `Bearer ${token}`
    }
  };

  return new Promise((resolve) => {
    const req = https.request(opts, (res) => {
      let data = '';
      res.setEncoding('utf8');
      res.on('data', (chunk) => data += chunk);
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
  const pkgName = alert && alert.dependency && alert.dependency.package && alert.dependency.package.name;
  if (!pkgName) return null;
  const root = repoRoot || process.cwd();
  const paths = Array.isArray(pkgJsonPaths) && pkgJsonPaths.length > 0 ? pkgJsonPaths : removeOverride.findPackageJsons(root);
  const candidates = [];
  for (const p of paths) {
    try {
      const pj = JSON.parse(fs.readFileSync(p, 'utf8'));
      const deps = Object.assign({}, pj.dependencies || {}, pj.devDependencies || {}, pj.optionalDependencies || {}, pj.peerDependencies || {});
      if (deps && Object.prototype.hasOwnProperty.call(deps, pkgName)) {
        candidates.push({ path: p, reason: 'declares-dependency' });
        continue;
      }
      if (pj.overrides && Object.prototype.hasOwnProperty.call(pj.overrides, pkgName)) {
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
    candidates.sort((a, b) => b.path.split(path.sep).length - a.path.split(path.sep).length);
    return candidates[0];
  }

  // fallback: repo root package.json if present
  const repoRootPkg = paths.find(p => path.resolve(p) === path.resolve(path.join(root, 'package.json')));
  if (repoRootPkg) return { path: repoRootPkg, reason: 'repo-root-fallback' };

  // final fallback
  return { path: paths[0], reason: 'first-found-fallback' };
}

function buildPlanEntries(alerts, pkgJsonPaths, repoRoot) {
  const entries = [];
  for (const a of alerts) {
    const candidate = mapAlertToCandidate(a, pkgJsonPaths, repoRoot);
    entries.push({
      advisory_url: a.html_url || (a.security_advisory && a.security_advisory.references && a.security_advisory.references[0] && a.security_advisory.references[0].url) || null,
      package: (a.dependency && a.dependency.package && a.dependency.package.name) || null,
      vulnerable_range: a.vulnerable_version_range || null,
      candidate_path: candidate ? candidate.path : null,
      reason: candidate ? candidate.reason : 'no-candidate'
    });
  }
  return entries;
}

function writeReports(entries, root) {
  const repoRoot = root || process.cwd();
  const jsonPath = path.join(repoRoot, 'dependabot-override-plan.json');
  const txtPath = path.join(repoRoot, 'dependabot-override-plan.txt');
  fs.writeFileSync(jsonPath, JSON.stringify(entries, null, 2), 'utf8');
  const lines = entries.map(e => `package: ${e.package} -> candidate: ${e.candidate_path} (reason: ${e.reason})\n  advisory: ${e.advisory_url || '(none)'}\n  vulnerable_range: ${e.vulnerable_range || '(none)'}\n`);
  fs.writeFileSync(txtPath, lines.join('\n'), 'utf8');
  return { jsonPath, txtPath };
}

async function main() {
  const args = parseArgs();
  const dryRun = (typeof args['dry-run'] !== 'undefined') ? (String(args['dry-run']).toLowerCase() === 'true') : true;
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
  console.log(`Wrote plan JSON to ${written.jsonPath} and text to ${written.txtPath}`);

  if (!dryRun) {
    console.log('LIVE mode requested — PR creation not implemented in this prototype.');
  } else {
    console.log('DRY_RUN mode — no branches or PRs created.');
  }

  return 0;
}

if (require.main === module) {
  main().catch(err => {
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
      const safeName = pkg.replace(/[\/\\:@]/g, '-');
      const dir = path.join(base, `${safeName}`);
      fs.mkdirSync(dir, { recursive: true });
      const title = `chore(security): temporary override for ${pkg}`;
      const bodyLines = [];
      bodyLines.push(`This draft PR pins ${pkg} to a temporary override to mitigate ${e.advisory_url || 'a security advisory'}.`);
      bodyLines.push('');
      bodyLines.push(`Candidate package.json: ${e.candidate_path}`);
      bodyLines.push('');
      bodyLines.push('Monitor: This override is temporary — monitored by .github/workflows/monitor-overrides.yml. The monitor will periodically attempt to remove this override and open a follow-up PR to remove it when it is no longer needed.');
      if (e.vulnerable_range) bodyLines.push(`Vulnerable range: ${e.vulnerable_range}`);
      fs.writeFileSync(path.join(dir, 'pr-title.txt'), title, 'utf8');
      fs.writeFileSync(path.join(dir, 'pr-body.md'), bodyLines.join('\n'), 'utf8');
    }
    return base;
  }

  module.exports = {
    mapAlertToCandidate,
    buildPlanEntries,
    fetchDependabotAlerts,
    writeReports,
    generateDraftPrsDryRun,
    main
  };
}
