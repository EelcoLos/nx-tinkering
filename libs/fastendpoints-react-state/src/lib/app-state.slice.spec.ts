import {
  appStateSliceActions,
  appStateSliceReducer,
  initialAppStateSliceState,
} from './app-state.slice';

describe('appStateSlice reducer', () => {
  it('should preserve the active stack when storing an access token', () => {
    const state = appStateSliceReducer(
      undefined,
      appStateSliceActions.setActiveStack('orval'),
    );
    const withToken = appStateSliceReducer(
      state,
      appStateSliceActions.setAccessToken('token-123'),
    );
    expect(withToken.activeStack).toBe('orval');
  });

  it('should handle initial state', () => {
    expect(appStateSliceReducer(undefined, { type: '' })).toEqual(
      initialAppStateSliceState,
    );
  });

  it('should switch the comparison stack', () => {
    const state = appStateSliceReducer(
      undefined,
      appStateSliceActions.setActiveStack('orval'),
    );

    expect(state.activeStack).toBe('orval');
  });

  it('should store and clear the access token', () => {
    const withToken = appStateSliceReducer(
      undefined,
      appStateSliceActions.setAccessToken('token-123'),
    );
    expect(withToken.accessToken).toBe('token-123');

    const cleared = appStateSliceReducer(
      withToken,
      appStateSliceActions.clearAccessToken(),
    );
    expect(cleared.accessToken).toBe('');
  });
});
