import { test, expect } from '@playwright/test';

test.describe('Dashboard', () => {
  test('loads dashboard page', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('Dashboard')).toBeVisible();
    await expect(page.getByText('QueryMind')).toBeVisible();
  });

  test('navigation links are visible', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('Schemas')).toBeVisible();
    await expect(page.getByText('Query Studio')).toBeVisible();
    await expect(page.getByText('History')).toBeVisible();
    await expect(page.getByText('Settings')).toBeVisible();
  });

  test('can navigate to schema manager', async ({ page }) => {
    await page.goto('/');
    await page.click('text=Schemas');
    await expect(page).toHaveURL('/schemas');
    await expect(page.getByText('Schema Manager')).toBeVisible();
  });

  test('schema upload dropzone is visible', async ({ page }) => {
    await page.goto('/schemas');
    await expect(page.getByText('Drop your schema file here')).toBeVisible();
  });

  test('can navigate to query studio', async ({ page }) => {
    await page.goto('/studio');
    await expect(page.getByText('New Session')).toBeVisible();
  });

  test('can navigate to settings', async ({ page }) => {
    await page.goto('/settings');
    await expect(page.getByText('API Authentication')).toBeVisible();
    await expect(page.getByPlaceholder('qm_sk_...')).toBeVisible();
  });
});
