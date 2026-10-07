# fastendpoints-react-state

Redux Toolkit slice used by `apps/fastendpoints-react-example`. It keeps only
the state that has to be shared across screens:

- `activeStack`: which generated client is in use (`'hey-api' | 'orval'`)
- `accessToken`: the JWT returned by `/api/login`

Exports the reducer (`appStateSliceReducer`, mounted under
`APP_STATE_SLICE_FEATURE_KEY`), the actions (`setActiveStack`, `setAccessToken`,
`clearAccessToken`) and selectors (`selectActiveStack`, `selectAccessToken`).
Form fields stay in the form, not in Redux.

Run `npx nx test fastendpoints-react-state` to execute the unit tests with Vitest.
