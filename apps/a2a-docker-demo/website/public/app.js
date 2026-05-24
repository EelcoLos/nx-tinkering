// Keep API/observability origins aligned with the page scheme to avoid
// mixed-content failures when the demo is served over HTTPS.
const API_HOST = window.location.hostname;
const PAGE_SCHEME = window.location.protocol === 'https:' ? 'https' : 'http';
const API_URL = `${PAGE_SCHEME}://${API_HOST}:5056`;
const GRAFANA_URL = `${PAGE_SCHEME}://${API_HOST}:3001`;
const GRAFANA_OVERVIEW_URL = `${GRAFANA_URL}/d/a2a-tool-calling/a2a-tool-calling-overview?orgId=1&refresh=10s`;

const loginPage = document.getElementById('loginPage');
const dashboard = document.getElementById('dashboard');
const loginForm = document.getElementById('loginForm');
const loginError = document.getElementById('loginError');
const loginSuccess = document.getElementById('loginSuccess');
const triageError = document.getElementById('triageError');
const triageDescription = document.getElementById('triageDescription');
const historyList = document.getElementById('historyList');
const historyStatus = document.getElementById('historyStatus');
const observabilityLink = document.getElementById('observabilityLink');
const logoutButton = document.getElementById('logoutButton');
const submitTriageButton = document.getElementById('submitTriageButton');
const tabs = Array.from(document.querySelectorAll('[role="tab"]'));
const panels = Array.from(document.querySelectorAll('[role="tabpanel"]'));

observabilityLink.setAttribute('href', GRAFANA_OVERVIEW_URL);

console.log('Website Debug Info:');
console.log('- Hostname:', window.location.hostname);
console.log('- Full URL:', window.location.href);
console.log('- API_HOST:', API_HOST);
console.log('- API_URL:', API_URL);

loginForm.addEventListener('submit', handleLogin);
logoutButton.addEventListener('click', logout);
submitTriageButton.addEventListener('click', submitTriage);
triageDescription.addEventListener('input', () => clearMessage(triageError));

tabs.forEach((tab, index) => {
  tab.addEventListener('click', () =>
    activateTab(tab.dataset.tab, { focusTab: false }),
  );
  tab.addEventListener('keydown', (event) => handleTabKeydown(event, index));
});

window.addEventListener('load', () => {
  const token = localStorage.getItem('token');
  if (token) {
    showDashboard();
    loadServices();
  }
});

fetch(`${API_URL}`, {
  mode: 'no-cors',
  headers: { Origin: window.location.origin },
}).catch(() => {
  // API not ready yet, that's OK
});

async function handleLogin(event) {
  event.preventDefault();
  clearMessage(loginError);
  clearMessage(loginSuccess);

  const username = document.getElementById('username').value;
  const password = document.getElementById('password').value;

  console.log('Login attempt to:', `${API_URL}/auth/login`);

  try {
    const response = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });

    const data = await response.json();

    if (response.ok && data.token) {
      localStorage.setItem('token', data.token);
      localStorage.setItem('userId', data.user_id);
      showMessage(loginSuccess, 'Login succeeded. Loading dashboard...');
      showDashboard();
      activateTab('services', { focusTab: false });
      await loadServices();
      return;
    }

    console.error('Login failed:', data);
    showMessage(loginError, 'Invalid credentials');
  } catch (err) {
    console.error('Login error:', err.message, err);
    showMessage(loginError, `Login failed: ${err.message}`);
  }
}

function logout() {
  localStorage.removeItem('token');
  localStorage.removeItem('userId');
  clearMessage(loginError);
  clearMessage(loginSuccess);
  clearMessage(triageError);
  loginForm.reset();
  hideDashboard();
  document.getElementById('username').focus();
}

function showDashboard() {
  loginPage.hidden = true;
  loginPage.style.display = 'none';
  dashboard.hidden = false;
  dashboard.style.display = 'block';
}

function hideDashboard() {
  loginPage.hidden = false;
  loginPage.style.display = 'flex';
  dashboard.hidden = true;
  dashboard.style.display = 'none';
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
  tabs[index].focus();
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
    }
  } catch (err) {
    console.error('Failed to load services:', err);
  }
}

async function submitTriage() {
  clearMessage(triageError);
  const input = triageDescription.value.trim();

  if (!input) {
    showMessage(triageError, 'Please enter a description');
    triageDescription.focus();
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
      const errorData = await response.json();
      showMessage(triageError, `Failed: ${errorData.error || 'Unknown error'}`);
      return;
    }

    const data = await response.json();
    triageDescription.value = '';
    addToHistory(data);
    activateTab('history', { focusTab: true, focusPanel: true });
  } catch (err) {
    showMessage(triageError, `Triage processing failed: ${err.message}`);
  }
}

function addToHistory(request) {
  if (historyList.textContent.includes('No requests yet')) {
    historyList.innerHTML = '';
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

  historyList.insertBefore(item, historyList.firstChild);
  historyStatus.textContent = 'A triage request was added to request history.';
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
