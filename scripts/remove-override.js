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

function parseDirective(text) {
  if (!text) return null;
  const m = text.match(/monitor-override:\s*(\S+)/i);
  if (m) return { type: 'monitor-override', pkg: m[1] };
  const m2 = text.match(/@override-bot\s+check\s+(\S+)/i);
  if (m2) return { type: 'monitor-override', pkg: m2[1] };
  if (/monitor:\s*overrides/i.test(text)) return { type: 'monitor-all-overrides' };
  return null;
}

function extractDirectiveFromEvent(event) {
  try {
    if (!event || typeof event !== 'object') return null;
    if (event.comment && event.comment.body) return parseDirective(event.comment.body);
    if (event.issue && event.issue.body) return parseDirective(event.issue.body);
    if (event.pull_request && event.pull_request.body) return parseDirective(event.pull_request.body);
    if (event.inputs && event.inputs['monitor-override']) {
      return { type: 'monitor-override', pkg: event.inputs['monitor-override'] };
    }
  } catch (err) {
    // ignore
  }
  return null;
}

async function main() {
  const args = parseArgs();
  const eventPath = args['event-path'] || process.env.GITHUB_EVENT_PATH || null;
  const dryRun = (typeof args['dry-run'] !== 'undefined') ? (String(args['dry-run']).toLowerCase() === 'true') : true;
  const explicitPkg = args['pkg'] || null;

  const repoRoot = process.cwd();
  const pkgPath = path.join(repoRoot, 'package.json');
  const reportPath = path.join(repoRoot, 'monitor-report.txt');
  const out = [];

  out.push(`Event path: ${eventPath || '(none)'}`);
  out.push(`Dry run: ${dryRun}`);

  let eventObj = null;
  if (eventPath && fs.existsSync(eventPath)) {
    try {
      eventObj = JSON.parse(fs.readFileSync(eventPath, 'utf8'));
      out.push('Loaded event JSON from ' + eventPath);
    } catch (err) {
      out.push('Failed to parse event JSON: ' + String(err && err.message));
    }
  }

  if (!fs.existsSync(pkgPath)) {
    out.push(`No package.json found at ${pkgPath}`);
    fs.writeFileSync(reportPath, out.join('\n'), 'utf8');
    console.log(out.join('\n'));
    return 0;
  }

  let pkg;
  try {
    pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
  } catch (err) {
    console.error('Failed to parse package.json:', err && err.message);
    return 2;
  }

  const overrides = pkg.overrides || {};
  const allPkgs = Object.keys(overrides);
  out.push(`Found ${allPkgs.length} override(s)`);

  let directive = null;
  if (explicitPkg) {
    directive = { type: 'monitor-override', pkg: explicitPkg };
    out.push(`Explicit pkg from CLI: ${explicitPkg}`);
  } else if (eventObj) {
    directive = extractDirectiveFromEvent(eventObj);
    out.push(`Parsed directive from event: ${directive ? JSON.stringify(directive) : '(none)'}`);
  } else {
    out.push('No explicit package and no event JSON provided; defaulting to all overrides');
  }

  let targets = allPkgs.slice();
  if (directive && directive.type === 'monitor-override') {
    targets = targets.filter(t => t === directive.pkg);
  } else if (directive && directive.type === 'monitor-all-overrides') {
    // keep all
  }

  out.push(`Targets to check: ${targets.length > 0 ? targets.join(', ') : '(none)'}`);
  targets.forEach(t => out.push(`${t}: ${JSON.stringify(overrides[t])}`));

  out.push('Note: trial removal and PR creation not implemented in this prototype.');

  fs.writeFileSync(reportPath, out.join('\n'), 'utf8');
  console.log(`Wrote report to ${reportPath}`);
  return 0;
}

if (require.main === module) {
  main().catch(err => {
    console.error(err);
    process.exit(2);
  });
} else {
  module.exports = {
    parseArgs,
    parseDirective,
    extractDirectiveFromEvent,
    main,
  };
}
