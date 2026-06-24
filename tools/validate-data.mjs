// Validate the shipped mapping data against its JSON Schema. Pure Node; run in
// `npm test`. Exits non-zero on any schema violation so bad data can't ship.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import Ajv from 'ajv';

const ROOT = resolve(process.cwd());
const schemaPath = resolve(ROOT, 'data/schema/mapping-table.schema.json');
const dataPath = resolve(ROOT, 'data/bijoy-sutonnymj.json');

const schema = JSON.parse(readFileSync(schemaPath, 'utf8'));
const data = JSON.parse(readFileSync(dataPath, 'utf8'));

const ajv = new Ajv({ allErrors: true, strict: false });
const validate = ajv.compile(schema);

if (!validate(data)) {
  console.error(`Mapping data ${dataPath} FAILED schema validation:`);
  for (const err of validate.errors ?? []) {
    console.error(`  ${err.instancePath || '(root)'} ${err.message}`);
  }
  process.exit(1);
}

console.log(`Mapping data OK — ${data.map.length} entries, id "${data.id}" v${data.version}.`);
