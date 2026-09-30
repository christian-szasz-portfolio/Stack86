import { test as base } from '@playwright/test';
import { addCoverageReport } from 'monocart-reporter';

export const test = base.extend({
  page: async ({ page, browserName }, use, testInfo) => {
    const supportsCoverage = browserName === 'chromium';
    if (supportsCoverage) {
      try {
        await page.coverage.startJSCoverage({ resetOnNavigation: false });
      } catch {
        // best-effort
      }
    }

    await use(page);

    if (supportsCoverage) {
      try {
        const entries = await page.coverage.stopJSCoverage();
        if (entries && entries.length > 0) {
          await addCoverageReport(entries, testInfo);
        }
      } catch {
        // best-effort
      }
    }
  },
});

export { expect } from '@playwright/test';
