#!/usr/bin/env node
'use strict';

// One-time local setup. Safe to re-run: every step skips what is already done.

const { execFileSync } = require('child_process');
const { randomBytes } = require('crypto');
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..');
const dotnet = (...args) =>
  execFileSync('dotnet', args, { cwd: root, encoding: 'utf8' });

// 1. HTTPS dev cert for the .NET APIs, exported as PEM for the angular-auth-example dev server
//    (creates the cert first when there is none).
if (
  !fs.existsSync(path.join(root, 'apps/angular-auth-example/ssl/localhost.pem'))
) {
  // dev-certs refuses to create the target directory itself.
  fs.mkdirSync(path.join(root, 'apps/angular-auth-example/ssl'), {
    recursive: true,
  });
  dotnet(
    'dev-certs',
    'https',
    '-ep',
    'apps/angular-auth-example/ssl/localhost.pem',
    '-np',
    '--format',
    'pem',
  );
  console.log('Exported dev cert for angular-auth-example');
}

// 2. JWT signing keys (user-secrets) for the FastEndpoints JWT APIs. Never overwrites an existing key.
for (const project of ['apps/dotnet-fe-auth', 'apps/fastendpoints-react-api']) {
  if (
    dotnet('user-secrets', 'list', '--project', project).includes(
      'Jwt:SigningKey',
    )
  ) {
    console.log(`${project}: Jwt:SigningKey already set`);
    continue;
  }
  const key = randomBytes(32).toString('base64');
  dotnet('user-secrets', 'set', 'Jwt:SigningKey', key, '--project', project);
  console.log(`${project}: generated Jwt:SigningKey`);
}

// 3. .env for the a2a-docker-demo compose stack (demo values from .env.example).
const env = path.join(root, 'apps/a2a-docker-demo/.env');
if (!fs.existsSync(env)) {
  fs.copyFileSync(`${env}.example`, env);
  console.log('Created apps/a2a-docker-demo/.env from .env.example');
}

console.log(
  'Setup done. Trust the dev cert once with `dotnet dev-certs https --trust` if your browser warns.',
);
