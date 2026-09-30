import { mergeTests } from '@playwright/test';
import { test as compileTest } from './compile.fixture';
import { test as coverageTest } from './coverage.fixture';

export const test = mergeTests(coverageTest, compileTest);
export { expect } from '@playwright/test';
