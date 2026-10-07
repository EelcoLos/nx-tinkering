import { test, expect } from '@playwright/test';

const accessToken = 'e2e.header.token';
const email = 'demo@fastendpoints.dev';

// Mock the FastEndpoints API so the e2e run doesn't need the .NET app.
test.beforeEach(async ({ page }) => {
  await page.route('**/api/login', (route) =>
    route.fulfill({ json: { accessToken } }),
  );
  await page.route('**/api/validate-token', (route) =>
    route.request().headers()['authorization'] === `Bearer ${accessToken}`
      ? route.fulfill({
          json: { email, expiresAt: '2030-01-01T00:00:00+00:00' },
        })
      : route.fulfill({ status: 401 }),
  );
});

test('shows the stack comparison shell', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: /Compare generated client stacks/i }),
  ).toBeVisible();
  await expect(page.getByRole('button', { name: /Switch to Orval/i })).toBeVisible();
});

for (const stack of ['Hey API + TanStack Query', 'Orval + React Query']) {
  test(`logs in and calls the protected endpoint with ${stack}`, async ({
    page,
  }) => {
    await page.goto('/');
    await page.getByRole('button', { name: stack }).click();
    await expect(page.getByText(`Current stack: ${stack}`)).toBeVisible();

    const loginRequest = page.waitForRequest('**/api/login');
    await page.getByRole('button', { name: 'Log in' }).click();
    expect((await loginRequest).postDataJSON()).toMatchObject({ email });

    await expect(page).toHaveURL(/\/protected$/);
    await expect(page.getByText(`Authenticated as ${email}.`)).toBeVisible();
    await expect(page.getByText(accessToken)).toHaveCount(0);
  });
}
