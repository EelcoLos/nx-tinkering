const fs = require('fs');
const os = require('os');
const path = require('path');
const processor = require('../../scripts/reconcile-dependabot-overrides');

describe('parseArgs', () => {
  const originalArgv = process.argv.slice();

  afterEach(() => {
    process.argv = originalArgv.slice();
  });

  test('supports key=value and key value forms', () => {
    process.argv = [
      ...originalArgv.slice(0, 2),
      '--updated-dependencies-json',
      '{"foo":1}',
      '--flag',
      '--dependency-names=undici,axios',
    ];

    expect(processor.parseArgs()).toEqual({
      'updated-dependencies-json': '{"foo":1}',
      flag: true,
      'dependency-names': 'undici,axios',
    });
  });
});

describe('collectUpdatedDependencyNames', () => {
  test('extracts dependency names from Dependabot metadata and strings', () => {
    const names = processor.collectUpdatedDependencyNames(
      JSON.stringify([
        { 'dependency-name': 'undici' },
        { dependency: { package: { name: 'yaml' } } },
      ]),
      'axios, lodash',
    );

    expect(names).toEqual(['undici', 'yaml', 'axios', 'lodash']);
  });
});

describe('restoreWorkspace', () => {
  test('uses npm install without package lock when no original lockfile exists', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'override-restore-'));
    const packageJsonPath = path.join(tmpDir, 'package.json');
    const packageLockPath = path.join(tmpDir, 'package-lock.json');
    fs.writeFileSync(packageJsonPath, '{"name":"fixture"}\n', 'utf8');
    fs.writeFileSync(packageLockPath, 'stale-lock', 'utf8');

    const childProcess = require('child_process');
    const originalExecSync = childProcess.execSync;
    const commands = [];

    childProcess.execSync = (command) => {
      commands.push(command);
      return '';
    };

    try {
      processor.restoreWorkspace(
        {
          packageJsonPath,
          packageJson: '{"name":"fixture","private":true}\n',
          packageLockPath,
          packageLock: null,
        },
        tmpDir,
      );
    } finally {
      childProcess.execSync = originalExecSync;
    }

    expect(commands).toEqual(['npm install --no-package-lock']);
    expect(fs.readFileSync(packageJsonPath, 'utf8')).toBe(
      '{"name":"fixture","private":true}\n',
    );
    expect(fs.existsSync(packageLockPath)).toBe(false);
  });
});

describe('removeMatchingOverrides', () => {
  test('removes top-level and nested override keys', () => {
    const overrides = {
      undici: '5.7.1',
      '@nx/angular': {
        picomatch: '4.0.0',
      },
    };

    const removed = processor.removeMatchingOverrides(overrides, [
      'undici',
      'picomatch',
      'missing',
    ]);

    expect(removed).toEqual([
      { dependencyName: 'undici', removedPath: 'overrides["undici"]' },
      {
        dependencyName: 'picomatch',
        removedPath: 'overrides["@nx/angular"]["picomatch"]',
      },
    ]);
    expect(overrides).toEqual({});
  });
});

describe('summarizeAuditResult', () => {
  test('extracts the vulnerability total from npm audit json', () => {
    const summary = processor.summarizeAuditResult({
      metadata: {
        vulnerabilities: {
          total: 3,
          low: 1,
          moderate: 2,
          high: 0,
          critical: 0,
        },
      },
    });

    expect(summary).toEqual({
      ok: true,
      total: 3,
      vulnerabilities: {
        total: 3,
        low: 1,
        moderate: 2,
        high: 0,
        critical: 0,
      },
    });
  });

  test('returns an unusable summary when vulnerabilities are missing', () => {
    expect(processor.summarizeAuditResult({})).toEqual({
      ok: false,
      total: null,
      vulnerabilities: null,
    });
  });
});
