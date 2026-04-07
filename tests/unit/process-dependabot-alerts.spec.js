const path = require('path');
const processor = require('../../scripts/process-dependabot-alerts');

describe('process-dependabot-alerts mapping', () => {
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
