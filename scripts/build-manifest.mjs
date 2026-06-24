#!/usr/bin/env node
/*
 * build-manifest.mjs — substitute the manifest base URL.
 *
 * Reads manifest.xml, replaces every __MUKTI_BASE_URL__ token with the value of
 * the MUKTI_BASE_URL environment variable (default https://localhost:3000 for
 * dev), and writes the result to dist/manifest.xml.
 *
 * Why: the committed manifest keeps the token so hosting can move without users
 * re-sideloading, and so a missing/malformed base URL FAILS THE BUILD rather than
 * shipping a localhost manifest to production (do-not-repeat M2).
 *
 * Usage:
 *   MUKTI_BASE_URL=https://mukti.example node scripts/build-manifest.mjs
 *   node scripts/build-manifest.mjs            # dev default (localhost:3000)
 *   node scripts/build-manifest.mjs --out path/to/manifest.xml
 */

import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const TOKEN = '__MUKTI_BASE_URL__';
const DEFAULT_DEV_URL = 'https://localhost:3000';

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');

function parseOutArg() {
  const i = process.argv.indexOf('--out');
  if (i !== -1 && process.argv[i + 1]) return resolve(process.argv[i + 1]);
  return resolve(repoRoot, 'dist', 'manifest.xml');
}

function validateBaseUrl(raw) {
  const value = (raw ?? '').trim();
  if (!value) {
    throw new Error(
      'MUKTI_BASE_URL is empty. Set it (e.g. https://your-domain) or unset it to use the dev default.',
    );
  }
  let url;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`MUKTI_BASE_URL is not a valid URL: "${value}"`);
  }
  if (url.protocol !== 'https:') {
    throw new Error(`MUKTI_BASE_URL must use https:// (got "${value}"). Office add-ins require HTTPS.`);
  }
  // Drop any trailing slash so the manifest's "/taskpane.html" never doubles up.
  return value.replace(/\/+$/, '');
}

async function main() {
  const baseUrl = validateBaseUrl(process.env.MUKTI_BASE_URL ?? DEFAULT_DEV_URL);

  const srcPath = resolve(repoRoot, 'manifest.xml');
  const template = await readFile(srcPath, 'utf8');

  if (!template.includes(TOKEN)) {
    throw new Error(`manifest.xml contains no ${TOKEN} token — nothing to substitute. Refusing to ship.`);
  }

  const output = template.split(TOKEN).join(baseUrl);

  if (output.includes(TOKEN)) {
    throw new Error('Substitution failed: token still present after replace.');
  }

  const outPath = parseOutArg();
  await mkdir(dirname(outPath), { recursive: true });
  await writeFile(outPath, output, 'utf8');

  console.log(`Wrote ${outPath} with base URL ${baseUrl}`);
}

main().catch((err) => {
  console.error('build-manifest failed:', err.message);
  process.exit(1);
});
