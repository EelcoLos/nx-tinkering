import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export type ClientStack = 'hey-api' | 'orval';

export const APP_STATE_SLICE_FEATURE_KEY = 'appStateSlice';

export interface AppStateSliceState {
  activeStack: ClientStack;
  email: string;
  password: string;
  accessToken: string;
  demoCount: number;
}

export const initialAppStateSliceState: AppStateSliceState = {
  activeStack: 'hey-api',
  email: 'demo@fastendpoints.dev',
  password: 'SecureDevPassword123!',
  accessToken: '',
  demoCount: 0,
};

export const appStateSlice = createSlice({
  name: APP_STATE_SLICE_FEATURE_KEY,
  initialState: initialAppStateSliceState,
  reducers: {
    setActiveStack(state, action: PayloadAction<ClientStack>) {
      state.activeStack = action.payload;
    },
    setEmail(state, action: PayloadAction<string>) {
      state.email = action.payload;
    },
    setPassword(state, action: PayloadAction<string>) {
      state.password = action.payload;
    },
    setAccessToken(state, action: PayloadAction<string>) {
      state.accessToken = action.payload;
    },
    clearAccessToken(state) {
      state.accessToken = '';
    },
    incrementDemoCount(state) {
      state.demoCount += 1;
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

export const selectEmail = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): string => selectAppStateSliceState(rootState).email;

export const selectPassword = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): string => selectAppStateSliceState(rootState).password;

export const selectAccessToken = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): string => selectAppStateSliceState(rootState).accessToken;

export const selectDemoCount = (rootState: {
  [APP_STATE_SLICE_FEATURE_KEY]: AppStateSliceState;
}): number => selectAppStateSliceState(rootState).demoCount;
