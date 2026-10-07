import { useActionState, type ReactNode } from 'react';
import { useFormStatus } from 'react-dom';
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router';
import { useMutation, useQuery } from '@tanstack/react-query';

import {
  loginMutation as heyApiLoginMutation,
  validateTokenOptions as heyApiValidateTokenOptions,
} from '../generated/hey-api';
import {
  useLogin as useOrvalLogin,
  getValidateTokenQueryKey as getOrvalValidateTokenQueryKey,
  useValidateToken as useOrvalValidateToken,
} from '../generated/orval';
import {
  appStateSliceActions,
  selectAccessToken,
  selectActiveStack,
  type ClientStack,
} from 'fastendpoints-react-state';

import { useAppDispatch, useAppSelector } from './hooks';
import styles from './app.module.css';

type StatusTone = 'neutral' | 'positive' | 'warning' | 'negative';
type LoginState = 'idle' | 'success' | 'error';
type ValidationState = 'idle' | 'pending' | 'valid' | 'error';

type Credentials = { email: string; password: string };

/** Demo credentials accepted by fastendpoints-react-api (see LoginEndpoint.cs). */
const demoCredentials: Credentials = {
  email: 'demo@fastendpoints.dev',
  password: 'SecureDevPassword123!',
};

/** What each stack exposes to the shared panels. */
interface StackClient {
  login: (credentials: Credentials) => Promise<string>;
  validation: { state: ValidationState; email?: string };
}

const stackInfo: Record<
  ClientStack,
  { label: string; description: string; buttonText: string }
> = {
  'hey-api': {
    label: 'Hey API + TanStack Query',
    description:
      'Generated query/mutation options and query keys from the FastEndpoints OpenAPI spec.',
    buttonText: 'Switch to Hey API',
  },
  orval: {
    label: 'Orval + React Query',
    description:
      'Generated React Query hooks from the same FastEndpoints OpenAPI spec.',
    buttonText: 'Switch to Orval',
  },
};

const statusToneClass: Record<StatusTone, string> = {
  neutral: styles.statusNeutral,
  positive: styles.statusPositive,
  warning: styles.statusWarning,
  negative: styles.statusNegative,
};

const loginStateMeta: Record<
  LoginState,
  { label: string; tone: StatusTone; message: string }
> = {
  idle: {
    label: 'Ready',
    tone: 'neutral',
    message: 'Use the demo credentials and submit the login mutation.',
  },
  success: {
    label: 'Token stored',
    tone: 'positive',
    message: 'Access token stored in the Redux slice.',
  },
  error: {
    label: 'Login failed',
    tone: 'negative',
    message: 'The login request was rejected by the API.',
  },
};

const validationStateMeta: Record<
  ValidationState,
  { label: string; tone: StatusTone }
> = {
  idle: { label: 'Waiting', tone: 'neutral' },
  pending: { label: 'Validating', tone: 'warning' },
  valid: { label: 'Token valid', tone: 'positive' },
  error: { label: 'Token rejected', tone: 'negative' },
};

function validationMessage({ state, email }: StackClient['validation']) {
  switch (state) {
    case 'idle':
      return 'Log in first to call the protected endpoint.';
    case 'pending':
      return 'Calling /api/validate-token with the bearer token...';
    case 'valid':
      return `Authenticated as ${email}.`;
    case 'error':
      return 'The backend rejected the token (missing, expired or invalid).';
  }
}

function maskToken(token: string) {
  return token
    ? `${token.slice(0, 12)}…${token.slice(-6)}`
    : 'No token stored yet';
}

function toValidationState(
  accessToken: string,
  query: { isFetching: boolean; isError: boolean; data?: unknown },
): ValidationState {
  if (!accessToken) return 'idle';
  if (query.isFetching) return 'pending';
  if (query.isError) return 'error';
  return query.data ? 'valid' : 'idle';
}

function useHeyApiClient(): StackClient {
  const accessToken = useAppSelector(selectAccessToken);
  const login = useMutation(heyApiLoginMutation());
  // Bind `auth` to this token and key the query by it, so a new token never
  // reuses cached or in-flight data from the previous identity.
  const validateOptions = heyApiValidateTokenOptions({ auth: accessToken });
  const validation = useQuery({
    ...validateOptions,
    // `tags` is part of Hey API's typed query key, so it changes the key hash.
    queryKey: [{ ...validateOptions.queryKey[0], tags: [accessToken] }],
    enabled: Boolean(accessToken),
  });

  return {
    login: async (credentials) =>
      (await login.mutateAsync({ body: credentials })).accessToken,
    validation: {
      state: toValidationState(accessToken, validation),
      email: validation.data?.email,
    },
  };
}

function useOrvalClient(): StackClient {
  const accessToken = useAppSelector(selectAccessToken);
  const login = useOrvalLogin();
  const validation = useOrvalValidateToken({
    query: {
      enabled: Boolean(accessToken),
      queryKey: [...getOrvalValidateTokenQueryKey(), accessToken],
    },
  });

  return {
    login: async (credentials) =>
      (await login.mutateAsync({ data: credentials })).accessToken,
    validation: {
      state: toValidationState(accessToken, validation),
      email: validation.data?.email,
    },
  };
}

function StatusPill({
  tone,
  children,
}: {
  tone: StatusTone;
  children: ReactNode;
}) {
  return (
    <span className={`${styles.statusPill} ${statusToneClass[tone]}`}>
      {children}
    </span>
  );
}

function StackSwitcher() {
  const activeStack = useAppSelector(selectActiveStack);
  const dispatch = useAppDispatch();

  return (
    <div className={styles.switcher}>
      {(Object.entries(stackInfo) as Array<[ClientStack, (typeof stackInfo)[ClientStack]]>).map(
        ([stack, info]) => (
          <button
            key={stack}
            type="button"
            className={`${styles.switchButton} ${
              activeStack === stack ? styles.switchButtonActive : ''
            }`}
            aria-pressed={activeStack === stack}
            onClick={() =>
              dispatch(appStateSliceActions.setActiveStack(stack))
            }
          >
            <span>{info.label}</span>
            <small>
              {activeStack === stack ? 'Currently active' : info.buttonText}
            </small>
          </button>
        ),
      )}
    </div>
  );
}

function SubmitButton() {
  const { pending } = useFormStatus();

  return (
    <button className={styles.primaryButton} type="submit" disabled={pending}>
      {pending ? 'Logging in...' : 'Log in'}
    </button>
  );
}

function ClearTokenButton() {
  const dispatch = useAppDispatch();

  return (
    <button
      className={styles.secondaryButton}
      type="button"
      onClick={() => dispatch(appStateSliceActions.clearAccessToken())}
    >
      Clear token
    </button>
  );
}

function AuthPanel({ stack, client }: { stack: ClientStack; client: StackClient }) {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const accessToken = useAppSelector(selectAccessToken);

  const [loginState, loginAction, isPending] = useActionState(
    async (_previous: LoginState, formData: FormData): Promise<LoginState> => {
      try {
        const token = await client.login({
          email: String(formData.get('email')),
          password: String(formData.get('password')),
        });
        dispatch(appStateSliceActions.setAccessToken(token));
        navigate('/protected');
        return 'success';
      } catch {
        return 'error';
      }
    },
    'idle',
  );

  const info = stackInfo[stack];
  const loginMeta = isPending
    ? { label: 'Logging in', tone: 'warning' as const, message: 'Submitting...' }
    : loginStateMeta[loginState];
  const validationMeta = validationStateMeta[client.validation.state];

  return (
    <section className={styles.card}>
      <header className={styles.cardHeader}>
        <div>
          <p className={styles.kicker}>Selected stack</p>
          <h2 className={styles.cardTitle}>{info.label}</h2>
          <p className={styles.cardDescription}>{info.description}</p>
        </div>
        <div className={styles.badgeGroup}>
          <StatusPill tone={loginMeta.tone}>{loginMeta.label}</StatusPill>
          <StatusPill tone={validationMeta.tone}>
            {validationMeta.label}
          </StatusPill>
        </div>
      </header>

      <form className={styles.form} action={loginAction}>
        <label className={styles.field}>
          <span>Email</span>
          <input
            className={styles.input}
            name="email"
            type="email"
            required
            defaultValue={demoCredentials.email}
          />
        </label>

        <label className={styles.field}>
          <span>Password</span>
          <input
            className={styles.input}
            name="password"
            type="password"
            required
            defaultValue={demoCredentials.password}
          />
        </label>

        <div className={styles.buttonRow}>
          <SubmitButton />
          <ClearTokenButton />
          <Link className={styles.linkButton} to="/protected">
            Open protected screen
          </Link>
        </div>
      </form>

      <div className={styles.summaryGrid}>
        <div>
          <span className={styles.summaryLabel}>Access token</span>
          <code className={styles.codeBlock}>{maskToken(accessToken)}</code>
        </div>
        <div>
          <span className={styles.summaryLabel}>Login status</span>
          <p className={styles.summaryCopy}>{loginMeta.message}</p>
        </div>
        <div>
          <span className={styles.summaryLabel}>Validation status</span>
          <p className={styles.summaryCopy}>
            {validationMessage(client.validation)}
          </p>
        </div>
      </div>
    </section>
  );
}

function ProtectedPanel({
  stack,
  client,
}: {
  stack: ClientStack;
  client: StackClient;
}) {
  const accessToken = useAppSelector(selectAccessToken);
  const info = stackInfo[stack];
  const validationMeta = validationStateMeta[client.validation.state];

  return (
    <section className={styles.card}>
      <header className={styles.cardHeader}>
        <div>
          <p className={styles.kicker}>Protected screen</p>
          <h2 className={styles.cardTitle}>{info.label}</h2>
          <p className={styles.cardDescription}>
            This screen calls the protected endpoint with the stored token,
            sent as an Authorization: Bearer header by the selected client.
          </p>
        </div>
        <StatusPill tone={validationMeta.tone}>{validationMeta.label}</StatusPill>
      </header>

      <div className={styles.summaryGrid}>
        <div>
          <span className={styles.summaryLabel}>Access token</span>
          <code className={styles.codeBlock}>{maskToken(accessToken)}</code>
        </div>
        <div>
          <span className={styles.summaryLabel}>Validation detail</span>
          <p className={styles.summaryCopy}>
            {validationMessage(client.validation)}
          </p>
        </div>
      </div>

      <div className={styles.buttonRow}>
        <ClearTokenButton />
        <Link className={styles.linkButton} to="/">
          Back to comparison
        </Link>
      </div>
    </section>
  );
}

function HeyApiAuthPanel() {
  return <AuthPanel stack="hey-api" client={useHeyApiClient()} />;
}

function OrvalAuthPanel() {
  return <AuthPanel stack="orval" client={useOrvalClient()} />;
}

function HeyApiProtectedPanel() {
  return <ProtectedPanel stack="hey-api" client={useHeyApiClient()} />;
}

function OrvalProtectedPanel() {
  return <ProtectedPanel stack="orval" client={useOrvalClient()} />;
}

function ActiveStackPanel() {
  const activeStack = useAppSelector(selectActiveStack);

  return activeStack === 'hey-api' ? <HeyApiAuthPanel /> : <OrvalAuthPanel />;
}

function ProtectedStackPanel() {
  const activeStack = useAppSelector(selectActiveStack);

  return activeStack === 'hey-api' ? (
    <HeyApiProtectedPanel />
  ) : (
    <OrvalProtectedPanel />
  );
}

export function App() {
  const activeStack = useAppSelector(selectActiveStack);

  return (
    <div className={styles.shell}>
      <main className={styles.frame}>
        <header className={styles.hero}>
          <p className={styles.kicker}>FastEndpoints auth comparison</p>
          <h1 className={styles.pageTitle}>
            Compare generated client stacks against the same backend.
          </h1>
          <p className={styles.pageCopy}>
            A small Redux Toolkit slice keeps the stack toggle and access token
            in one place while the backend is consumed through either Hey API +
            TanStack Query or Orval + React Query, both generated from the same
            OpenAPI spec.
          </p>
          <div className={styles.heroRow}>
            <StatusPill tone="neutral">
              Current stack: {stackInfo[activeStack].label}
            </StatusPill>
            <StatusPill tone="positive">Shared state: Redux Toolkit</StatusPill>
          </div>
        </header>

        <StackSwitcher />

        <Routes>
          <Route path="/" element={<ActiveStackPanel />} />
          <Route path="/protected" element={<ProtectedStackPanel />} />
          <Route path="*" element={<Navigate replace to="/" />} />
        </Routes>

        <footer className={styles.footer}>
          <Link className={styles.footerLink} to="/protected">
            Protected screen
          </Link>
          <Link className={styles.footerLink} to="/">
            Comparison view
          </Link>
        </footer>
      </main>
    </div>
  );
}

export default App;
