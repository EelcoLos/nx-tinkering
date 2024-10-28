const PROXY_CONFIG = [
  {
    context: [
      '/api',
      '/scim',
      '/signout',
      '/signin-oidc',
      '/swagger',
    ],
    target: 'https://localhost:5001',
    secure: false,
    logLevel: 'debug',
  },
];

module.exports = PROXY_CONFIG;
