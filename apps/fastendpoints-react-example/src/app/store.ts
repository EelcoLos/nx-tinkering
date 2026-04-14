import { configureStore } from '@reduxjs/toolkit';
import {
  APP_STATE_SLICE_FEATURE_KEY,
  appStateSliceReducer,
} from 'fastendpoints-react-state';

export const store = configureStore({
  reducer: {
    [APP_STATE_SLICE_FEATURE_KEY]: appStateSliceReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
