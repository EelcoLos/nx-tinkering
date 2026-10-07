import { selectAccessToken } from 'fastendpoints-react-state';

import { client as heyApiClient } from '../generated/hey-api/client.gen';
import { store } from './store';

const getAccessToken = () => selectAccessToken(store.getState()) || undefined;

// Hey API: `auth` is sent as `Authorization: Bearer <token>` for every
// operation that declares `security` in the OpenAPI spec.
heyApiClient.setConfig({ auth: getAccessToken });

// Orval: custom fetch mutator (see `override.mutator` in orval.config.ts).
// Adds the bearer token and throws on non-2xx so React Query reports errors.
export const orvalFetch = async <T>(
  url: string,
  options: RequestInit,
): Promise<T> => {
  const headers = new Headers(options.headers);
  const token = getAccessToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url, { ...options, headers });
  const body = [204, 205, 304].includes(response.status)
    ? ''
    : await response.text();
  const data = body ? JSON.parse(body) : undefined;

  if (!response.ok) {
    throw data ?? new Error(`${response.status} ${response.statusText}`);
  }

  return data as T;
};
