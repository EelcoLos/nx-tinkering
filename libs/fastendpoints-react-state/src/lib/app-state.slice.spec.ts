import {
  appStateSliceActions,
  appStateSliceReducer,
  initialAppStateSliceState,
} from './app-state.slice';

describe('appStateSlice reducer', () => {
  it('should handle initial state', () => {
    expect(appStateSliceReducer(undefined, { type: '' })).toEqual(
      initialAppStateSliceState,
    );
  });

  it('should update the comparison stack and auth form state', () => {
    const state = appStateSliceReducer(
      appStateSliceReducer(
        appStateSliceReducer(
          appStateSliceReducer(undefined, { type: '' }),
          appStateSliceActions.setActiveStack('orval'),
        ),
        appStateSliceActions.setEmail('agent@example.com'),
      ),
      appStateSliceActions.setPassword('changed-password'),
    );

    expect(state.activeStack).toBe('orval');
    expect(state.email).toBe('agent@example.com');
    expect(state.password).toBe('changed-password');
  });

  it('should track the token and demo counter', () => {
    const state = appStateSliceReducer(
      appStateSliceReducer(
        undefined,
        appStateSliceActions.setAccessToken('token-123'),
      ),
      appStateSliceActions.incrementDemoCount(),
    );

    expect(state.accessToken).toBe('token-123');
    expect(state.demoCount).toBe(1);
  });
});
