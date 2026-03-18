#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');

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

async function main() {
  const args = parseArgs();
  const eventPath = args['event-path'] || process.env.GITHUB_EVENT_PATH || null;
  const dryRun = (typeof args['dry-run'] !== 'undefined') ? (String(args['dry-run']).toLowerCase() === 'true') : true;

  const repoRoot = process.cwd();
  const pkgPath = path.join(repoRoot, 'package.json');
  const reportPath = path.join(repoRoot, 'monitor-report.txt');
  const out = [];

  out.push(`Event path: ${eventPath || '(none)'}`);
  out.push(`Dry run: ${dryRun}`);

  if (!fs.existsSync(pkgPath)) {
    out.push(`No package.json found at ${pkgPath}`);
    fs.writeFileSync(reportPath, out.join('\n'), 'utf8');
    console.log(out.join('\n'));
    process.exit(0);
  }

  let pkg;
  try {
    pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
  } catch (err) {
    console.error('Failed to parse package.json:', err && err.message);
    process.exit(2);
  }

  const overrides = pkg.overrides || {};
  const keys = Object.keys(overrides);
  out.push(`Found ${keys.length} override(s)`);
  keys.forEach(k => {
    out.push(`${k}: ${JSON.stringify(overrides[k])}`);
  });

  // Placeholder for future trial removal logic.
  out.push('Note: trial removal and PR creation not implemented in this prototype.');

  fs.writeFileSync(reportPath, out.join('\n'), 'utf8');
  console.log(`Wrote report to ${reportPath}`);
  process.exit(0);
}

main().catch(err => {
  console.error(err);
  process.exit(2);
});
