import { type ChangeEvent, type FormEvent, type ReactNode } from 'react';
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';

import {
  loginMutation as heyApiLoginMutation,
  validateTokenOptions as heyApiValidateTokenOptions,
} from '../generated/hey-api';
import {
  useLogin as useOrvalLogin,
  useValidateToken as useOrvalValidateToken,
} from '../generated/orval';
import {
  appStateSliceActions,
  selectAccessToken,
  selectActiveStack,
  selectDemoCount,
  selectEmail,
  selectPassword,
  type ClientStack,
} from 'fastendpoints-react-state';

import { useAppDispatch, useAppSelector } from './hooks';
import styles from './app.module.css';

type StatusTone = 'neutral' | 'positive' | 'warning' | 'negative';
type LoginState = 'idle' | 'pending' | 'success' | 'error';
type ValidationState = 'idle' | 'pending' | 'valid' | 'invalid' | 'error';

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
  { label: string; tone: StatusTone }
> = {
  idle: { label: 'Ready', tone: 'neutral' },
  pending: { label: 'Logging in', tone: 'warning' },
  success: { label: 'Token stored', tone: 'positive' },
  error: { label: 'Login failed', tone: 'negative' },
};

const validationStateMeta: Record<
  ValidationState,
  { label: string; tone: StatusTone }
> = {
  idle: { label: 'Waiting', tone: 'neutral' },
  pending: { label: 'Validating', tone: 'warning' },
  valid: { label: 'Token valid', tone: 'positive' },
  invalid: { label: 'Token invalid', tone: 'negative' },
  error: { label: 'Validation error', tone: 'negative' },
};

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

function AuthPanelView({
  stack,
  loginState,
  loginMessage,
  validationState,
  validationMessage,
  email,
  password,
  accessToken,
  demoCount,
  onEmailChange,
  onPasswordChange,
  onSubmit,
  onIncrementDemoCount,
  onClearToken,
}: {
  stack: ClientStack;
  loginState: LoginState;
  loginMessage: string;
  validationState: ValidationState;
  validationMessage: string;
  email: string;
  password: string;
  accessToken: string;
  demoCount: number;
  onEmailChange: (event: ChangeEvent<HTMLInputElement>) => void;
  onPasswordChange: (event: ChangeEvent<HTMLInputElement>) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onIncrementDemoCount: () => void;
  onClearToken: () => void;
}) {
  const info = stackInfo[stack];
  const loginMeta = loginStateMeta[loginState];
  const validationMeta = validationStateMeta[validationState];

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

      <form className={styles.form} onSubmit={onSubmit}>
        <label className={styles.field}>
          <span>Email</span>
          <input
            className={styles.input}
            type="email"
            value={email}
            onChange={onEmailChange}
          />
        </label>

        <label className={styles.field}>
          <span>Password</span>
          <input
            className={styles.input}
            type="password"
            value={password}
            onChange={onPasswordChange}
          />
        </label>

        <div className={styles.buttonRow}>
          <button
            className={styles.primaryButton}
            type="submit"
            disabled={loginState === 'pending'}
          >
            {loginState === 'pending' ? 'Logging in...' : 'Log in'}
          </button>
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={onIncrementDemoCount}
          >
            Demo counter +1
          </button>
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={onClearToken}
          >
            Clear token
          </button>
          <Link className={styles.linkButton} to="/protected">
            Open protected screen
          </Link>
        </div>
      </form>

      <div className={styles.summaryGrid}>
        <div>
          <span className={styles.summaryLabel}>Access token</span>
          <code className={styles.codeBlock}>
            {accessToken || 'No token stored yet'}
          </code>
        </div>
        <div>
          <span className={styles.summaryLabel}>Demo counter</span>
          <strong className={styles.metric}>{demoCount}</strong>
        </div>
        <div>
          <span className={styles.summaryLabel}>Login status</span>
          <p className={styles.summaryCopy}>{loginMessage}</p>
        </div>
        <div>
          <span className={styles.summaryLabel}>Validation status</span>
          <p className={styles.summaryCopy}>{validationMessage}</p>
        </div>
      </div>
    </section>
  );
}

function ProtectedPanelView({
  stack,
  validationState,
  validationMessage,
  accessToken,
  demoCount,
  onClearToken,
}: {
  stack: ClientStack;
  validationState: ValidationState;
  validationMessage: string;
  accessToken: string;
  demoCount: number;
  onClearToken: () => void;
}) {
  const info = stackInfo[stack];
  const validationMeta = validationStateMeta[validationState];

  return (
    <section className={styles.card}>
      <header className={styles.cardHeader}>
        <div>
          <p className={styles.kicker}>Protected screen</p>
          <h2 className={styles.cardTitle}>{info.label}</h2>
          <p className={styles.cardDescription}>
            This screen re-validates the stored token with the currently
            selected generated client.
          </p>
        </div>
        <StatusPill tone={validationMeta.tone}>{validationMeta.label}</StatusPill>
      </header>

      <div className={styles.summaryGrid}>
        <div>
          <span className={styles.summaryLabel}>Access token</span>
          <code className={styles.codeBlock}>
            {accessToken || 'No token stored yet'}
          </code>
        </div>
        <div>
          <span className={styles.summaryLabel}>Demo counter</span>
          <strong className={styles.metric}>{demoCount}</strong>
        </div>
        <div>
          <span className={styles.summaryLabel}>Validation detail</span>
          <p className={styles.summaryCopy}>{validationMessage}</p>
        </div>
      </div>

      <div className={styles.buttonRow}>
        <button
          className={styles.secondaryButton}
          type="button"
          onClick={onClearToken}
        >
          Clear token
        </button>
        <Link className={styles.linkButton} to="/">
          Back to comparison
        </Link>
      </div>
    </section>
  );
}

function HeyApiPanel() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const email = useAppSelector(selectEmail);
  const password = useAppSelector(selectPassword);
  const accessToken = useAppSelector(selectAccessToken);
  const demoCount = useAppSelector(selectDemoCount);

  const login = useMutation({
    ...heyApiLoginMutation(),
    onSuccess: (response) => {
      dispatch(appStateSliceActions.setAccessToken(response.accessToken));
      navigate('/protected');
    },
  });

  const validation = useQuery({
    ...heyApiValidateTokenOptions({ query: { token: accessToken } }),
    enabled: Boolean(accessToken),
  });

  const loginState: LoginState = login.isPending
    ? 'pending'
    : login.isError
      ? 'error'
      : login.isSuccess
        ? 'success'
        : 'idle';
  const validationState: ValidationState = !accessToken
    ? 'idle'
    : validation.isFetching
      ? 'pending'
      : validation.isError
        ? 'error'
        : validation.data?.isValid
          ? 'valid'
          : 'invalid';

  return (
    <AuthPanelView
      stack="hey-api"
      loginState={loginState}
      loginMessage={
        login.isError
          ? 'The login request was rejected by the API.'
          : login.isSuccess
            ? 'Access token stored in the generated Redux slice.'
            : 'Use the demo credentials and submit the Hey API mutation.'
      }
      validationState={validationState}
      validationMessage={
        !accessToken
          ? 'Log in first to run the token-validation query.'
          : validation.isFetching
            ? 'Checking the token with the Hey API query helpers...'
            : validation.isError
              ? 'The token-validation query failed.'
              : validation.data?.isValid
                ? 'The token is valid according to the backend.'
                : 'The backend rejected the token.'
      }
      email={email}
      password={password}
      accessToken={accessToken}
      demoCount={demoCount}
      onEmailChange={(event) =>
        dispatch(appStateSliceActions.setEmail(event.target.value))
      }
      onPasswordChange={(event) =>
        dispatch(appStateSliceActions.setPassword(event.target.value))
      }
      onSubmit={(event) => {
        event.preventDefault();
        login.mutate({ body: { email, password } });
      }}
      onIncrementDemoCount={() =>
        dispatch(appStateSliceActions.incrementDemoCount())
      }
      onClearToken={() => dispatch(appStateSliceActions.clearAccessToken())}
    />
  );
}

function OrvalPanel() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const email = useAppSelector(selectEmail);
  const password = useAppSelector(selectPassword);
  const accessToken = useAppSelector(selectAccessToken);
  const demoCount = useAppSelector(selectDemoCount);

  const login = useOrvalLogin({
    mutation: {
      onSuccess: (response) => {
        dispatch(
          appStateSliceActions.setAccessToken(response.data.accessToken),
        );
        navigate('/protected');
      },
    },
  });

  const validation = useOrvalValidateToken(
    { token: accessToken },
    {
      query: {
        enabled: Boolean(accessToken),
      },
    },
  );

  const loginState: LoginState = login.isPending
    ? 'pending'
    : login.isError
      ? 'error'
      : login.isSuccess
        ? 'success'
        : 'idle';
  const validationState: ValidationState = !accessToken
    ? 'idle'
    : validation.isFetching
      ? 'pending'
      : validation.isError
        ? 'error'
        : validation.data?.data.isValid
          ? 'valid'
          : 'invalid';

  return (
    <AuthPanelView
      stack="orval"
      loginState={loginState}
      loginMessage={
        login.isError
          ? 'The Orval mutation failed.'
          : login.isSuccess
            ? 'Access token stored in the generated Redux slice.'
            : 'Use the demo credentials and submit the Orval mutation.'
      }
      validationState={validationState}
      validationMessage={
        !accessToken
          ? 'Log in first to run the Orval React Query hook.'
          : validation.isFetching
            ? 'Checking the token with the Orval query hook...'
            : validation.isError
              ? 'The token-validation request failed.'
              : validation.data?.data.isValid
                ? 'The token is valid according to the backend.'
                : 'The backend rejected the token.'
      }
      email={email}
      password={password}
      accessToken={accessToken}
      demoCount={demoCount}
      onEmailChange={(event) =>
        dispatch(appStateSliceActions.setEmail(event.target.value))
      }
      onPasswordChange={(event) =>
        dispatch(appStateSliceActions.setPassword(event.target.value))
      }
      onSubmit={(event) => {
        event.preventDefault();
        login.mutate({ data: { email, password } });
      }}
      onIncrementDemoCount={() =>
        dispatch(appStateSliceActions.incrementDemoCount())
      }
      onClearToken={() => dispatch(appStateSliceActions.clearAccessToken())}
    />
  );
}

function ProtectedHeyApiPanel() {
  const dispatch = useAppDispatch();
  const accessToken = useAppSelector(selectAccessToken);
  const demoCount = useAppSelector(selectDemoCount);

  const validation = useQuery({
    ...heyApiValidateTokenOptions({ query: { token: accessToken } }),
    enabled: Boolean(accessToken),
  });

  const validationState: ValidationState = !accessToken
    ? 'idle'
    : validation.isFetching
      ? 'pending'
      : validation.isError
        ? 'error'
        : validation.data?.isValid
          ? 'valid'
          : 'invalid';

  return (
    <ProtectedPanelView
      stack="hey-api"
      validationState={validationState}
      validationMessage={
        !accessToken
          ? 'No access token is stored yet.'
          : validation.isFetching
            ? 'Validating the token with Hey API...'
            : validation.isError
              ? 'The protected screen could not validate the token.'
              : validation.data?.isValid
                ? 'The protected screen is unlocked.'
                : 'The stored token is not valid anymore.'
      }
      accessToken={accessToken}
      demoCount={demoCount}
      onClearToken={() => dispatch(appStateSliceActions.clearAccessToken())}
    />
  );
}

function ProtectedOrvalPanel() {
  const dispatch = useAppDispatch();
  const accessToken = useAppSelector(selectAccessToken);
  const demoCount = useAppSelector(selectDemoCount);

  const validation = useOrvalValidateToken(
    { token: accessToken },
    {
      query: {
        enabled: Boolean(accessToken),
      },
    },
  );

  const validationState: ValidationState = !accessToken
    ? 'idle'
    : validation.isFetching
      ? 'pending'
      : validation.isError
        ? 'error'
        : validation.data?.data.isValid
          ? 'valid'
          : 'invalid';

  return (
    <ProtectedPanelView
      stack="orval"
      validationState={validationState}
      validationMessage={
        !accessToken
          ? 'No access token is stored yet.'
          : validation.isFetching
            ? 'Validating the token with Orval...'
            : validation.isError
              ? 'The protected screen could not validate the token.'
              : validation.data?.data.isValid
                ? 'The protected screen is unlocked.'
                : 'The stored token is not valid anymore.'
      }
      accessToken={accessToken}
      demoCount={demoCount}
      onClearToken={() => dispatch(appStateSliceActions.clearAccessToken())}
    />
  );
}

function ActiveStackPanel() {
  const activeStack = useAppSelector(selectActiveStack);

  return activeStack === 'hey-api' ? <HeyApiPanel /> : <OrvalPanel />;
}

function ProtectedStackPanel() {
  const activeStack = useAppSelector(selectActiveStack);

  return activeStack === 'hey-api' ? (
    <ProtectedHeyApiPanel />
  ) : (
    <ProtectedOrvalPanel />
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
            The generated Redux slice keeps the stack toggle, token, and demo
            counter in one place while the backend is consumed through either
            Hey API + TanStack Query or Orval + React Query.
          </p>
          <div className={styles.heroRow}>
            <StatusPill tone="neutral">
              Current stack: {stackInfo[activeStack].label}
            </StatusPill>
            <StatusPill tone="positive">Generated state: Redux Toolkit</StatusPill>
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
