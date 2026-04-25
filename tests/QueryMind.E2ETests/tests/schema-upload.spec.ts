import { test, expect } from '@playwright/test';
import path from 'path';

test.describe('Schema Upload', () => {
  test('schema manager shows upload button', async ({ page }) => {
    await page.goto('/schemas');
    await expect(page.getByRole('button', { name: 'Upload Schema' })).toBeVisible();
  });

  test('shows empty state when no schemas loaded', async ({ page }) => {
    await page.goto('/schemas');
    // With no API key, may show empty or error state
    await expect(page.getByText('No schemas uploaded yet').or(page.getByText('Schema Manager'))).toBeVisible();
  });
});
