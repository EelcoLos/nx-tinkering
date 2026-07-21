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
    {
      files: ['src/generated/**/*.{ts,tsx,js,jsx}'],
      // Disable rules that are newly enabled by ESLint v9/TypeScript ESLint v8 defaults for generated code
      rules: {
        '@typescript-eslint/no-explicit-any': 'off', // Newly enabled by typescript-eslint v8 recommended
        '@typescript-eslint/no-non-null-assertion': 'off', // Newly enabled by typescript-eslint v8 recommended
        '@eslint-react/no-unnecessary-use-prefix': 'off', // Newly enabled by @eslint-react recommended
      },
    },
  ];
})();
