// ESLint flat config. Its load-bearing job (BUILD-CI.md §5.3, D-0010): enforce
// the engine-purity boundary — `src/engine/**` must NOT import Office.js or
// anything under src/host/** or src/taskpane/**, nor reach across layers.
//
// Two complementary rules:
//   - no-restricted-imports  : blocks the office-js packages + host/taskpane paths
//   - import/no-restricted-paths : blocks engine -> host/taskpane by file zone

import js from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import tsparser from '@typescript-eslint/parser';
import importPlugin from 'eslint-plugin-import';

export default [
  {
    // dist/ is build output; host/taskpane/commands are owned by other agents
    // and carry their own (Office.js/DOM) lint surface — this config only
    // governs the engine + tooling that the engine owner is responsible for.
    ignores: [
      'dist/**',
      'node_modules/**',
      'src/host/**',
      'src/taskpane/**',
      'src/commands/**',
    ],
  },

  // Engine TypeScript sources (the only src/ this config gates).
  {
    files: ['src/engine/**/*.ts'],
    ...js.configs.recommended,
    languageOptions: {
      parser: tsparser,
      parserOptions: { sourceType: 'module', ecmaVersion: 2022 },
    },
    plugins: {
      '@typescript-eslint': tseslint,
      import: importPlugin,
    },
    rules: {
      ...tseslint.configs.recommended.rules,
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },

  // ── Engine purity: src/engine/** may NOT import Office.js or cross layers. ──
  {
    files: ['src/engine/**/*.ts'],
    plugins: { import: importPlugin },
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            { name: 'office-js', message: 'Engine must be pure: no Office.js.' },
            {
              name: '@microsoft/office-js',
              message: 'Engine must be pure: no Office.js.',
            },
          ],
          patterns: [
            '**/host/**',
            '**/taskpane/**',
            '../host/*',
            '../taskpane/*',
          ],
        },
      ],
      'import/no-restricted-paths': [
        'error',
        {
          zones: [
            {
              target: 'src/engine',
              from: 'src/host',
              message: 'engine -> host import forbidden (engine is a pure leaf).',
            },
            {
              target: 'src/engine',
              from: 'src/taskpane',
              message: 'engine -> taskpane import forbidden (engine is a pure leaf).',
            },
          ],
        },
      ],
      // No browser/DOM globals in the engine (it runs under bare Node).
      'no-restricted-globals': [
        'error',
        { name: 'window', message: 'Engine must be pure: no DOM globals.' },
        { name: 'document', message: 'Engine must be pure: no DOM globals.' },
        { name: 'Office', message: 'Engine must be pure: no Office.js global.' },
        { name: 'Word', message: 'Engine must be pure: no Office.js global.' },
      ],
    },
  },

  // Node-based tooling and tests (CommonJS-free ESM, Node globals allowed).
  {
    files: ['tools/**/*.mjs', 'test/**/*.mjs', 'eslint.config.js'],
    languageOptions: {
      sourceType: 'module',
      ecmaVersion: 2022,
      globals: {
        process: 'readonly',
        console: 'readonly',
        URL: 'readonly',
      },
    },
    rules: {
      'no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
];
