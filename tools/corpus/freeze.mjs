// Freeze the corpus: write MANIFEST.json with a SHA-256 and case count for
// every .jsonl data file under corpus/visible and corpus/heldout. Re-running
// after any edit changes the manifest — that diff is the deliberate, reviewable
// act of re-freezing. Run: node tools/corpus/freeze.mjs [--check]
//
// --check exits non-zero if the on-disk manifest is stale (for CI).

import { createHash } from 'node:crypto';
import { readFileSync, readdirSync, writeFileSync, existsSync } from 'node:fs';
import { join, resolve } from 'node:path';

const ROOT = resolve(process.cwd());
const CORPUS = join(ROOT, 'corpus');
const MANIFEST = join(CORPUS, 'MANIFEST.json');
const VERSION = '1';

function fileEntries(subdir) {
  const dir = join(CORPUS, subdir);
  if (!existsSync(dir)) return {};
  const out = {};
  for (const f of readdirSync(dir).filter((x) => x.endsWith('.jsonl')).sort()) {
    const buf = readFileSync(join(dir, f));
    const sha256 = createHash('sha256').update(buf).digest('hex');
    const cases = buf
      .toString('utf8')
      .split('\n')
      .filter((l) => l.trim()).length;
    out[`${subdir}/${f}`] = { sha256, cases, bytes: buf.length };
  }
  return out;
}

function build() {
  const files = { ...fileEntries('visible'), ...fileEntries('heldout') };
  const totalCases = Object.values(files).reduce((s, e) => s + e.cases, 0);
  return {
    corpusVersion: VERSION,
    frozenAt: new Date().toISOString().slice(0, 10),
    totalCases,
    files,
    note:
      'Frozen gold-standard corpus. Changing any data file changes its sha256 ' +
      'here; that is a deliberate re-freeze. heldout/ must not be read while ' +
      'building the converter.',
  };
}

const check = process.argv.includes('--check');
const next = build();

if (check) {
  if (!existsSync(MANIFEST)) {
    console.error('MANIFEST.json missing — run: node tools/corpus/freeze.mjs');
    process.exit(1);
  }
  const cur = JSON.parse(readFileSync(MANIFEST, 'utf8'));
  const a = JSON.stringify(cur.files);
  const b = JSON.stringify(next.files);
  if (a !== b) {
    console.error('Corpus is out of sync with MANIFEST.json. Re-freeze with: node tools/corpus/freeze.mjs');
    process.exit(1);
  }
  console.log(`Corpus manifest OK — ${cur.totalCases} cases across ${Object.keys(cur.files).length} files.`);
} else {
  writeFileSync(MANIFEST, JSON.stringify(next, null, 2) + '\n');
  console.log(`Wrote ${MANIFEST}: ${next.totalCases} cases across ${Object.keys(next.files).length} files.`);
}
