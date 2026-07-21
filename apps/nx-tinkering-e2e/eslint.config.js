const baseConfig = require('../../eslint.config.js');
const playwrightPlugin = require('eslint-plugin-playwright');

module.exports = [
  ...baseConfig,
  playwrightPlugin.configs['flat/recommended'],
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
    // Override or add rules here
    rules: {},
  },
  {
    files: ['**/*.ts', '**/*.tsx'],
    // Override or add rules here
    rules: {},
  },
  {
    files: ['**/*.js', '**/*.jsx'],
    // Override or add rules here
    rules: {},
  },
  {
    files: ['src/**/*.{ts,js,tsx,jsx}'],
    // Override or add rules here
    rules: {},
  },
];
