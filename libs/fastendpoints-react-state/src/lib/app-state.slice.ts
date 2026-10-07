import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export type ClientStack = 'hey-api' | 'orval';

export const APP_STATE_SLICE_FEATURE_KEY = 'appStateSlice';

export interface AppStateSliceState {
  activeStack: ClientStack;
  accessToken: string;
}

export const initialAppStateSliceState: AppStateSliceState = {
  activeStack: 'hey-api',
  accessToken: '',
};

export const appStateSlice = createSlice({
  name: APP_STATE_SLICE_FEATURE_KEY,
  initialState: initialAppStateSliceState,
  reducers: {
    setActiveStack(state, action: PayloadAction<ClientStack>) {
      state.activeStack = action.payload;
    },
    setAccessToken(state, action: PayloadAction<string>) {
      state.accessToken = action.payload;
    },
    clearAccessToken(state) {
      state.accessToken = '';
    },
  },
});

export const appStateSliceReducer = appStateSlice.reducer;
export const appStateSliceActions = appStateSlice.actions;

export const selectAppStateSliceState = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): AppStateSliceState => rootState[APP_STATE_SLICE_FEATURE_KEY];

export const selectActiveStack = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): ClientStack => selectAppStateSliceState(rootState).activeStack;

export const selectAccessToken = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): string => selectAppStateSliceState(rootState).accessToken;
