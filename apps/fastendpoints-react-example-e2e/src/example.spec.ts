import { test, expect } from '@playwright/test';

test('shows the stack comparison shell', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: /Compare generated client stacks/i }),
  ).toBeVisible();
  await expect(page.getByRole('button', { name: /Switch to Orval/i })).toBeVisible();
});
