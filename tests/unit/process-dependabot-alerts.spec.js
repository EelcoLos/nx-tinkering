const path = require('path');
const processor = require('../../scripts/process-dependabot-alerts');

describe('process-dependabot-alerts mapping', () => {
  test('removeNestedOverrideByPackageName removes top-level override key', () => {
    const overrides = {
      undici: '5.7.1',
      '@nx/angular': {
        picomatch: '4.0.0',
      },
    };

    const res = processor.removeNestedOverrideByPackageName(
      overrides,
      'undici',
    );

    expect(res.removed).toBe(true);
    expect(res.removedPath).toBe('overrides["undici"]');
    expect(overrides.undici).toBeUndefined();
    expect(overrides['@nx/angular'].picomatch).toBe('4.0.0');
  });

  test('removeNestedOverrideByPackageName removes nested override key and reports path', () => {
    const overrides = {
      '@nx/angular': {
        picomatch: '4.0.0',
      },
    };

    const res = processor.removeNestedOverrideByPackageName(
      overrides,
      'picomatch',
    );

    expect(res.removed).toBe(true);
    expect(res.removedPath).toBe('overrides["@nx/angular"]["picomatch"]');
    expect(overrides['@nx/angular']).toBeUndefined();
  });

  test('removeNestedOverrideByPackageName returns not removed when key is absent', () => {
    const overrides = {
      '@nx/angular': {
        picomatch: '4.0.0',
      },
    };

    const res = processor.removeNestedOverrideByPackageName(
      overrides,
      'undici',
    );

    expect(res.removed).toBe(false);
    expect(res.removedPath).toBeNull();
    expect(overrides['@nx/angular'].picomatch).toBe('4.0.0');
  });

  test('warns when multiple versions exist and higher top-level version is selected', () => {
    const lock = {
      lockfileVersion: 3,
      packages: {
        '': { name: 'fixture', version: '1.0.0' },
        'node_modules/foo': { version: '2.0.0' },
        'node_modules/bar/node_modules/foo': { version: '1.5.0' },
      },
    };

    const res = processor.detectHigherVersionSelectionWarnings(lock);
    expect(res.ok).toBe(true);
    expect(res.total_conflicts).toBe(1);
    expect(res.warnings[0].package).toBe('foo');
    expect(res.warnings[0].selected_version).toBe('2.0.0');
    expect(res.warnings[0].lower_versions).toContain('1.5.0');
  });

  test('does not warn when only one version exists', () => {
    const lock = {
      lockfileVersion: 3,
      packages: {
        '': { name: 'fixture', version: '1.0.0' },
        'node_modules/foo': { version: '2.0.0' },
      },
    };

    const res = processor.detectHigherVersionSelectionWarnings(lock);
    expect(res.ok).toBe(true);
    expect(res.total_conflicts).toBe(0);
    expect(res.warnings).toHaveLength(0);
  });

  test('marks transitive dependency when package only appears in overrides', () => {
    const fixturePkg = path.resolve(
      __dirname,
      '../fixtures/repo-with-override/package.json',
    );
    const alert = {
      dependency: { package: { name: 'undici', ecosystem: 'npm' } },
    };
    const entries = processor.buildPlanEntries(
      [alert],
      [fixturePkg],
      path.resolve(__dirname, '../../'),
    );
    expect(entries).toHaveLength(1);
    expect(entries[0].dependency_kind).toBe('transitive');
  });

  test('marks direct dependency when package is declared in dependency sections', () => {
    const repoRootPkg = path.resolve(__dirname, '../../package.json');
    const alert = {
      dependency: { package: { name: '@angular/common', ecosystem: 'npm' } },
    };
    const entries = processor.buildPlanEntries(
      [alert],
      [repoRootPkg],
      path.resolve(__dirname, '../../'),
    );
    expect(entries).toHaveLength(1);
    expect(entries[0].dependency_kind).toBe('direct');
  });

  test('chooses fixture package.json with undici override', () => {
    const fixturePkg = path.resolve(
      __dirname,
      '../fixtures/repo-with-override/package.json',
    );
    const alert = {
      dependency: { package: { name: 'undici', ecosystem: 'npm' } },
      html_url: 'https://example.com/advisory/1',
      vulnerable_version_range: '<=5.7.0',
    };
    const candidate = processor.mapAlertToCandidate(
      alert,
      [fixturePkg],
      path.resolve(__dirname, '../../'),
    );
    expect(candidate).not.toBeNull();
    expect(candidate.path).toBe(fixturePkg);
    // reason should be either already-overrides or has-overrides or declares-dependency
    expect([
      'already-overrides',
      'has-overrides',
      'declares-dependency',
    ]).toContain(candidate.reason);
  });

  test('falls back to repo root when no matches', () => {
    const repoRootPkg = path.resolve(__dirname, '../../package.json');
    const alert = {
      dependency: { package: { name: 'nonexistent-pkg', ecosystem: 'npm' } },
    };
    const candidate = processor.mapAlertToCandidate(
      alert,
      [repoRootPkg],
      path.resolve(__dirname, '../../'),
    );
    expect(candidate).not.toBeNull();
    expect(candidate.path).toBe(repoRootPkg);
    expect(candidate.reason).toMatch(/fallback/);
  });
});
