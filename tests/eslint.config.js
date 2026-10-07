const baseConfig = require('../eslint.config.js');

module.exports = [
  ...baseConfig,
  {
    // The specs exercise ../scripts, which is not an Nx project.
    files: ['**/*.js'],
    rules: { '@nx/enforce-module-boundaries': 'off' },
  },
];
