const baseConfig = require('../eslint.config.js');

module.exports = [
  ...baseConfig,
  {
    // The specs exercise ../scripts and ../tools/nx-plugins, which are not Nx projects.
    files: ['**/*.{js,ts}'],
    rules: { '@nx/enforce-module-boundaries': 'off' },
  },
];
