const baseConfig = require('../../eslint.config.js');

module.exports = (async () => {
  const { default: eslintReact } = await import('@eslint-react/eslint-plugin');

  return [
    {
      ...eslintReact.configs['recommended-typescript'],
      files: ['src/**/*.{ts,tsx,js,jsx}'],
    },
    ...baseConfig,
    {
      files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
      // Override or add rules here
      rules: {},
    },
  ];
})();
