import tseslint from 'typescript-eslint';
import angular from 'angular-eslint';

export default tseslint.config(
  {
    files: ['**/*.ts'],
    extends: [...tseslint.configs.recommended, ...angular.configs.tsRecommended],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'emu',
          style: 'kebab-case',
        },
      ],
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'emu',
          style: 'camelCase',
        },
      ],
      // A leading underscore is the codebase's existing marker for "deliberately unused"
      // (e.g. a positional callback argument that must be declared to reach the next one).
      // Without this, the recommended preset flags those as errors.
      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
          caughtErrorsIgnorePattern: '^_',
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
  },
  {
    // e2e tests are not Angular components — relax framework rules and use generic TS rules.
    files: ['e2e/**/*.ts'],
    extends: [...tseslint.configs.recommended],
    rules: {
      '@angular-eslint/component-selector': 'off',
      '@angular-eslint/directive-selector': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
    },
  },
  {
    ignores: ['e2e/dist/**', 'e2e/coverage/**', 'e2e/playwright-report/**', 'e2e/test-results/**'],
  },
);
