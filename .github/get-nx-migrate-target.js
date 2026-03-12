#!/usr/bin/env node
'use strict';

const fs = require('fs');

const GITHUB_OUTPUT = process.env.GITHUB_OUTPUT || null;
function writeOutput(name, value) {
  const line = `${name}=${value}\n`;
  if (GITHUB_OUTPUT) {
    fs.appendFileSync(GITHUB_OUTPUT, line);
  } else {
    console.log(`${name}=${value}`);
  }
}

function normalizeVersion(v) {
  if (!v) return '';
  return String(v).replace(/^[\^~v]+/, '');
}

try {
  const updatedDepsStr = process.env.UPDATED_DEPS || '';
  let migrateTarget = '';
  let nxVersion = '';
  let skip = true;
  let reason = '';

  if (updatedDepsStr && updatedDepsStr !== 'null') {
    try {
      const arr = JSON.parse(updatedDepsStr);
      const entry = arr.find(e => /^(nx|@nx\/.+)$/.test(e['dependency-name']));
      if (entry && entry['new-version']) {
        nxVersion = normalizeVersion(entry['new-version']);
        migrateTarget = `${entry['dependency-name']}@${nxVersion}`;
        skip = false;
      }
    } catch (e) {
      // ignore JSON parse errors
    }
  }

  if (skip) {
    // fallback to package.json
    const pkg = JSON.parse(fs.readFileSync('package.json', 'utf8'));
    const deps = Object.assign({}, pkg.dependencies || {}, pkg.devDependencies || {});
    if (deps['nx']) {
      nxVersion = normalizeVersion(deps['nx']);
      migrateTarget = `nx@${nxVersion}`;
      skip = false;
    } else {
      const nxKeys = Object.keys(deps).filter(k => k.startsWith('@nx/'));
      if (nxKeys.length) {
        const key = nxKeys[0];
        nxVersion = normalizeVersion(deps[key]);
        migrateTarget = `${key}@${nxVersion}`;
        skip = false;
      } else {
        skip = true;
        reason = 'Could not find nx or @nx/* in package.json';
      }
    }
  }

  if (skip) {
    writeOutput('skip', 'true');
    writeOutput('reason', reason || 'Could not determine NX version from Dependabot metadata or package.json');
  } else {
    writeOutput('migrate-target', migrateTarget);
    writeOutput('nx-version', nxVersion);
    writeOutput('skip', 'false');
  }
} catch (err) {
  writeOutput('skip', 'true');
  writeOutput('reason', `exception: ${err && err.message}`);
  process.exit(0);
}
