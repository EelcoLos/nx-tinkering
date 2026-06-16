const baseConfig = require('../../eslint.config.js');
const nx = require('@nx/eslint-plugin');

module.exports = [
  // Include global base settings
  ...baseConfig,

  // 1. Unpack Angular TypeScript rules directly into the array
  ...nx.configs['flat/angular'],

  // 2. Apply your custom rules to TypeScript files
  {
    files: ['**/*.ts'],
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'app', style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],
      '@angular-eslint/prefer-standalone': 'off',
    },
  },

  // 3. Unpack Angular template HTML rules directly into the array
  ...nx.configs['flat/angular-template'],

  // 4. Apply your custom rules to HTML files
  {
    files: ['**/*.html'],
    rules: {},
  },
];
