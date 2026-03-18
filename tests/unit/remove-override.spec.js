const { parseDirective, extractDirectiveFromEvent } = require('../../scripts/remove-override');

describe('parseDirective', () => {
  test('parses monitor-override: undici', () => {
    expect(parseDirective('monitor-override: undici')).toEqual({ type: 'monitor-override', pkg: 'undici' });
  });

  test('parses @override-bot check undici', () => {
    expect(parseDirective('@override-bot check undici')).toEqual({ type: 'monitor-override', pkg: 'undici' });
  });

  test('parses monitor: overrides', () => {
    expect(parseDirective('Please monitor: overrides')).toEqual({ type: 'monitor-all-overrides' });
  });

  test('returns null for unrelated text', () => {
    expect(parseDirective('hello world')).toBeNull();
  });
});

describe('extractDirectiveFromEvent', () => {
  test('parses comment body', () => {
    const event = { comment: { body: '@override-bot check undici' } };
    expect(extractDirectiveFromEvent(event)).toEqual({ type: 'monitor-override', pkg: 'undici' });
  });

  test('parses issue body', () => {
    const event = { issue: { body: 'monitor-override: undici' } };
    expect(extractDirectiveFromEvent(event)).toEqual({ type: 'monitor-override', pkg: 'undici' });
  });

  test('parses pull_request body', () => {
    const event = { pull_request: { body: 'monitor: overrides' } };
    expect(extractDirectiveFromEvent(event)).toEqual({ type: 'monitor-all-overrides' });
  });

  test('returns null for empty event', () => {
    expect(extractDirectiveFromEvent({})).toBeNull();
  });
});
