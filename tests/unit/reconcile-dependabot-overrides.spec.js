const processor = require('../../scripts/reconcile-dependabot-overrides');

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
