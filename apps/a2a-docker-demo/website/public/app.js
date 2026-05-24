// Keep API/observability origins aligned with the page scheme to avoid
// mixed-content failures when the demo is served over HTTPS.
const API_HOST = window.location.hostname;
const PAGE_SCHEME = window.location.protocol === 'https:' ? 'https' : 'http';
const API_URL = `${PAGE_SCHEME}://${API_HOST}:5056`;
const GRAFANA_URL = `${PAGE_SCHEME}://${API_HOST}:3001`;
const GRAFANA_OVERVIEW_URL = `${GRAFANA_URL}/d/a2a-tool-calling/a2a-tool-calling-overview?orgId=1&refresh=10s`;

const DASHBOARD_URL = 'index.html';
const LOGIN_URL = 'login.html';
const appState = {
  login: null,
  dashboard: null,
};

console.log('Website Debug Info:');
console.log('- Hostname:', window.location.hostname);
console.log('- Full URL:', window.location.href);
console.log('- API_HOST:', API_HOST);
console.log('- API_URL:', API_URL);

window.addEventListener('load', () => {
  initializePage().catch((err) => {
    console.error('Failed to initialize page:', err);
  });
});

fetch(`${API_URL}`, {
  mode: 'no-cors',
  headers: { Origin: window.location.origin },
}).catch(() => {
  // API not ready yet, that's OK
});

async function initializePage() {
  const page = document.body.dataset.page;

  if (page === 'login') {
    initializeLoginPage();
    return;
  }

  if (page === 'dashboard') {
    await initializeDashboardPage();
  }
}

function initializeLoginPage() {
  if (localStorage.getItem('token')) {
    redirectToDashboard();
    return;
  }

  appState.login = {
    loginForm: document.getElementById('loginForm'),
    loginError: document.getElementById('loginError'),
    loginSuccess: document.getElementById('loginSuccess'),
  };

  appState.login.loginForm.addEventListener('submit', handleLogin);
}

async function initializeDashboardPage() {
  if (!localStorage.getItem('token')) {
    redirectToLogin();
    return;
  }

  await loadDashboardFragments();

  const tabs = Array.from(document.querySelectorAll('[role="tab"]'));
  const panels = Array.from(document.querySelectorAll('[role="tabpanel"]'));

  appState.dashboard = {
    observabilityLink: document.getElementById('observabilityLink'),
    logoutButton: document.getElementById('logoutButton'),
    submitTriageButton: document.getElementById('submitTriageButton'),
    triageError: document.getElementById('triageError'),
    triageDescription: document.getElementById('triageDescription'),
    historyList: document.getElementById('historyList'),
    historyStatus: document.getElementById('historyStatus'),
    servicesGrid: document.getElementById('servicesGrid'),
    tabs,
    panels,
  };

  appState.dashboard.observabilityLink.setAttribute(
    'href',
    GRAFANA_OVERVIEW_URL,
  );
  appState.dashboard.logoutButton.addEventListener('click', logout);
  appState.dashboard.submitTriageButton.addEventListener('click', submitTriage);
  appState.dashboard.triageDescription.addEventListener('input', () =>
    clearMessage(appState.dashboard.triageError),
  );

  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () =>
      activateTab(tab.dataset.tab, { focusTab: false }),
    );
    tab.addEventListener('keydown', (event) => handleTabKeydown(event, index));
  });

  activateTab('services', { focusTab: false });
  await loadServices();
}

async function loadDashboardFragments() {
  const panels = Array.from(document.querySelectorAll('[data-fragment]'));

  await Promise.all(
    panels.map(async (panel) => {
      const response = await fetch(panel.dataset.fragment, {
        cache: 'no-store',
      });
      if (!response.ok) {
        throw new Error(`Unable to load ${panel.dataset.fragment}`);
      }

      panel.innerHTML = await response.text();
    }),
  );
}

async function handleLogin(event) {
  event.preventDefault();
  clearMessage(appState.login.loginError);
  clearMessage(appState.login.loginSuccess);

  const username = document.getElementById('username').value;
  const password = document.getElementById('password').value;

  console.log('Login attempt to:', `${API_URL}/auth/login`);

  try {
    const response = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });

    const data = await response.json().catch(() => ({}));

    if (response.ok && data.token) {
      localStorage.setItem('token', data.token);
      localStorage.setItem('userId', data.user_id);
      showMessage(
        appState.login.loginSuccess,
        'Login succeeded. Redirecting...',
      );
      redirectToDashboard();
      return;
    }

    console.error('Login failed:', data);
    showMessage(appState.login.loginError, 'Invalid credentials');
  } catch (err) {
    console.error('Login error:', err.message, err);
    showMessage(appState.login.loginError, `Login failed: ${err.message}`);
  }
}

function logout() {
  localStorage.removeItem('token');
  localStorage.removeItem('userId');

  if (appState.dashboard?.triageError) {
    clearMessage(appState.dashboard.triageError);
  }

  redirectToLogin();
}

function escapeHtml(str) {
  return String(str ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function activateTab(tabName, options = {}) {
  const { focusTab = true, focusPanel = false } = options;
  const { tabs, panels } = appState.dashboard;

  tabs.forEach((tab) => {
    const isActive = tab.dataset.tab === tabName;
    tab.classList.toggle('active', isActive);
    tab.setAttribute('aria-selected', String(isActive));
    tab.tabIndex = isActive ? 0 : -1;
    if (isActive && focusTab) {
      tab.focus();
    }
  });

  panels.forEach((panel) => {
    const isActive = panel.id === `${tabName}Tab`;
    panel.classList.toggle('active', isActive);
    panel.hidden = !isActive;
    if (isActive && focusPanel) {
      panel.focus();
    }
  });
}

function handleTabKeydown(event, currentIndex) {
  const { tabs } = appState.dashboard;
  const keyActions = {
    ArrowRight: () => focusTabByIndex((currentIndex + 1) % tabs.length),
    ArrowLeft: () =>
      focusTabByIndex((currentIndex - 1 + tabs.length) % tabs.length),
    Home: () => focusTabByIndex(0),
    End: () => focusTabByIndex(tabs.length - 1),
    Enter: () =>
      activateTab(tabs[currentIndex].dataset.tab, { focusTab: true }),
    ' ': () => activateTab(tabs[currentIndex].dataset.tab, { focusTab: true }),
  };

  const action = keyActions[event.key];
  if (!action) {
    return;
  }

  event.preventDefault();
  action();
}

function focusTabByIndex(index) {
  appState.dashboard.tabs[index].focus();
}

async function loadServices() {
  try {
    const response = await fetch(`${API_URL}/api/services`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (response.status === 401) {
      logout();
      return;
    }

    if (!response.ok) {
      throw new Error('Unable to load services');
    }

    const services = await response.json();
    renderServices(services);
  } catch (err) {
    console.error('Failed to load services:', err);
    renderServicesError();
  }
}

async function submitTriage() {
  clearMessage(appState.dashboard.triageError);
  const input = appState.dashboard.triageDescription.value.trim();

  if (!input) {
    showMessage(appState.dashboard.triageError, 'Please enter a description');
    appState.dashboard.triageDescription.focus();
    return;
  }

  try {
    const response = await fetch(`${API_URL}/api/triage`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify({ input }),
    });

    if (response.status === 401) {
      logout();
      return;
    }

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      showMessage(
        appState.dashboard.triageError,
        `Failed: ${errorData.error || 'Unknown error'}`,
      );
      return;
    }

    const data = await response.json();
    appState.dashboard.triageDescription.value = '';
    addToHistory(data);
    activateTab('history', { focusTab: true, focusPanel: true });
  } catch (err) {
    showMessage(
      appState.dashboard.triageError,
      `Triage processing failed: ${err.message}`,
    );
  }
}

function addToHistory(request) {
  if (appState.dashboard.historyList.textContent.includes('No requests yet')) {
    appState.dashboard.historyList.innerHTML = '';
  }

  const item = document.createElement('article');
  item.className = 'request-item';
  item.setAttribute('tabindex', '0');

  let traceHtml = '';
  if (request.trace && request.trace.length > 0) {
    traceHtml = '<div class="trace-list">';
    traceHtml += '<strong>Service Trace:</strong><br>';
    request.trace.forEach((entry, idx) => {
      traceHtml += `<div class="trace-row">${idx + 1}. <strong>${escapeHtml(entry.service)}</strong>: ${escapeHtml(entry.result)} (${escapeHtml(entry.timestamp_ms)}ms)</div>`;
    });
    traceHtml += '</div>';
  }

  item.innerHTML = `
    <h3>Request #${Math.random().toString(36).slice(2, 11).toUpperCase()}</h3>
    <p><strong>Input:</strong> ${escapeHtml(request.input) || 'Processing...'}</p>
    <p><strong>Classification:</strong> ${escapeHtml(request.classification) || 'Pending'}</p>
    <p><strong>Priority:</strong> ${escapeHtml(request.priority) || 'Pending'}</p>
    <p><strong>Status:</strong> ${escapeHtml(request.status) || 'In Progress'}</p>
    <p><strong>Ticket ID:</strong> ${escapeHtml(request.ticket_id) || 'N/A'}</p>
    <div class="flow" aria-label="Workflow path">
      Classifier <span class="flow-arrow" aria-hidden="true">→</span>
      Assessor <span class="flow-arrow" aria-hidden="true">→</span>
      Router <span class="flow-arrow" aria-hidden="true">→</span>
      Handler
    </div>
    ${traceHtml}
  `;

  appState.dashboard.historyList.insertBefore(
    item,
    appState.dashboard.historyList.firstChild,
  );
  appState.dashboard.historyStatus.textContent =
    'A triage request was added to request history.';
}

function renderServices(services) {
  if (!Array.isArray(services) || services.length === 0) {
    appState.dashboard.servicesGrid.innerHTML = `
      <article class="service-card" role="listitem">
        <p class="empty-state">No services are currently available.</p>
      </article>
    `;
    return;
  }

  appState.dashboard.servicesGrid.innerHTML = services
    .map((service) => {
      const skills = Array.isArray(service.skills) ? service.skills : [];
      const skillsHtml = skills.length
        ? `<div class="skill-list">${skills.map((skill) => `<span class="skill-chip">${escapeHtml(skill)}</span>`).join('')}</div>`
        : '<p class="service-meta">No published skills</p>';

      return `
        <article class="service-card" role="listitem">
          <h3>${escapeHtml(service.name || service.service_id || 'Unknown service')}</h3>
          <p class="service-description">${escapeHtml(service.description || 'No description available.')}</p>
          <p class="service-meta">Base URL: ${escapeHtml(service.base_url || 'N/A')}</p>
          <p class="service-meta">Port: ${escapeHtml(service.port ?? 'N/A')}</p>
          <span class="status healthy">Healthy</span>
          ${skillsHtml}
        </article>
      `;
    })
    .join('');
}

function renderServicesError() {
  appState.dashboard.servicesGrid.innerHTML = `
    <article class="service-card" role="listitem">
      <h3>Service list unavailable</h3>
      <p class="service-description">
        The website could not load the current A2A service catalog.
      </p>
      <span class="status unhealthy">Unavailable</span>
    </article>
  `;
}

function showMessage(element, message) {
  element.textContent = message;
  element.hidden = false;
  element.style.display = 'block';
}

function clearMessage(element) {
  element.textContent = '';
  element.hidden = true;
  element.style.display = 'none';
}

function redirectToDashboard() {
  window.location.replace(DASHBOARD_URL);
}

function redirectToLogin() {
  window.location.replace(LOGIN_URL);
}
