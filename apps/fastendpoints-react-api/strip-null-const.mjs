// Workaround: FastEndpoints.OpenApi 8.3.0 emits `"const": null` on schemas it clones
// (e.g. those with FluentValidation rules), which makes Orval/Hey API type every field as
// `null`. Fixed upstream (FastEndpoints#1169, 8.4.0). Delete this script after upgrading.
import { readFileSync, writeFileSync } from 'node:fs';

const file = new URL('./wwwroot/api/v1.json', import.meta.url);

const strip = (node) => {
  if (Array.isArray(node)) {
    node.forEach(strip);
  } else if (node && typeof node === 'object') {
    if (node.const === null) delete node.const;
    Object.values(node).forEach(strip);
  }
};

const doc = JSON.parse(readFileSync(file, 'utf8'));
strip(doc);
writeFileSync(file, `${JSON.stringify(doc, null, 2)}\n`);
