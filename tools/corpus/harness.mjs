// Corpus harness: run a converter over corpus cases and report metrics.
// Pure Node, no Office.js. The converter is injected so this harness is reused
// by the spike (reference converter) and later by the real engine.
//
// Usage:
//   import { runCorpus } from './harness.mjs';
//   const report = await runCorpus({ convert, dir, threshold: 0.99 });
//
// CLI:
//   node tools/corpus/harness.mjs --dir corpus/visible --converter tools/corpus/reference-converter.mjs

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import {
  characterAccuracy,
  wordAccuracy,
  isNFC,
  exact,
} from './metrics.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));

/** Load all .jsonl cases from a directory. Throws on malformed JSON. */
export function loadCases(dir) {
  const abs = resolve(dir);
  const files = readdirSync(abs).filter((f) => f.endsWith('.jsonl'));
  const cases = [];
  for (const f of files) {
    const text = readFileSync(join(abs, f), 'utf8');
    text.split('\n').forEach((line, i) => {
      const trimmed = line.trim();
      if (!trimmed) return;
      let obj;
      try {
        obj = JSON.parse(trimmed);
      } catch (e) {
        throw new Error(`${f}:${i + 1}: invalid JSON — ${e.message}`);
      }
      obj.__file = f;
      cases.push(obj);
    });
  }
  return cases;
}

const sourceString = (c) => String.fromCodePoint(...(c.source || []));

function blankAgg() {
  return { n: 0, char: 0, word: 0, exact: 0, nfcFail: 0, idemFail: 0, noopFail: 0 };
}

/**
 * Run `convert` over every case under `dir`.
 * @param {(s:string)=>string} convert
 * @returns aggregate + per-category + per-feature metrics and a list of failures.
 */
export function runCorpus({ convert, dir, threshold = 0.99 }) {
  const cases = loadCases(dir);
  const overall = blankAgg();
  const byCategory = {};
  const byFeature = {};
  const failures = [];

  for (const c of cases) {
    const src = sourceString(c);
    const expected = c.expected ?? '';

    // Expected must be NFC (corpus-authoring guard).
    if (!isNFC(expected)) {
      failures.push({ id: c.id, kind: 'expected-not-nfc' });
    }

    let actual;
    try {
      actual = convert(src);
    } catch (e) {
      failures.push({ id: c.id, kind: 'threw', detail: e.message });
      actual = '';
    }

    const ca = characterAccuracy(expected, actual);
    const wa = wordAccuracy(expected, actual);
    const ex = exact(expected, actual);
    const nfcOk = isNFC(actual);

    // Idempotency: convert(convert(src)) === convert(src)
    let idemOk = true;
    try {
      idemOk = exact(actual, convert(actual));
    } catch {
      idemOk = false;
    }
    // No-op on already-Unicode: convert(expected) === expected
    let noopOk = true;
    try {
      noopOk = exact(expected, convert(expected));
    } catch {
      noopOk = false;
    }

    const tally = (agg) => {
      agg.n++;
      agg.char += ca;
      agg.word += wa;
      agg.exact += ex ? 1 : 0;
      agg.nfcFail += nfcOk ? 0 : 1;
      agg.idemFail += idemOk ? 0 : 1;
      agg.noopFail += noopOk ? 0 : 1;
    };
    tally(overall);
    (byCategory[c.category] ??= blankAgg());
    tally(byCategory[c.category]);
    for (const feat of c.features || []) {
      (byFeature[feat] ??= blankAgg());
      tally(byFeature[feat]);
    }

    if (!ex || !nfcOk || !idemOk || !noopOk) {
      failures.push({
        id: c.id,
        kind: 'mismatch',
        expected,
        actual,
        charAcc: +ca.toFixed(4),
        nfcOk,
        idemOk,
        noopOk,
      });
    }
  }

  const finalize = (agg) => ({
    n: agg.n,
    charAccuracy: agg.n ? +(agg.char / agg.n).toFixed(4) : 1,
    wordAccuracy: agg.n ? +(agg.word / agg.n).toFixed(4) : 1,
    exactRate: agg.n ? +(agg.exact / agg.n).toFixed(4) : 1,
    nfcFailures: agg.nfcFail,
    idempotencyFailures: agg.idemFail,
    noopFailures: agg.noopFail,
  });

  const o = finalize(overall);
  const pass =
    o.charAccuracy >= threshold &&
    o.wordAccuracy >= threshold &&
    o.nfcFailures === 0 &&
    o.idempotencyFailures === 0 &&
    o.noopFailures === 0;

  return {
    dir,
    threshold,
    pass,
    overall: o,
    byCategory: Object.fromEntries(
      Object.entries(byCategory).map(([k, v]) => [k, finalize(v)])
    ),
    byFeature: Object.fromEntries(
      Object.entries(byFeature).map(([k, v]) => [k, finalize(v)])
    ),
    failures,
  };
}

/** Fuzz: random inputs must never throw and must be NFC-stable. */
export function fuzz({ convert, iterations = 2000, seed = 12345 }) {
  let s = seed >>> 0;
  const rnd = () => ((s = (s * 1664525 + 1013904223) >>> 0) / 0x100000000);
  const pools = [
    Array.from({ length: 95 }, (_, i) => 32 + i), // printable ASCII
    Array.from({ length: 128 }, (_, i) => 128 + i), // high bytes (Bijoy range)
    [0x0980 + 0, 0x09be, 0x09cd, 0x0995, 0x09bf, 0x2018, 0x2019], // mix unicode
  ];
  const problems = [];
  for (let i = 0; i < iterations; i++) {
    const len = Math.floor(rnd() * 24);
    let cps = [];
    for (let j = 0; j < len; j++) {
      const pool = pools[Math.floor(rnd() * pools.length)];
      cps.push(pool[Math.floor(rnd() * pool.length)]);
    }
    const input = String.fromCodePoint(...cps);
    try {
      const out = convert(input);
      if (out !== out.normalize('NFC')) {
        problems.push({ iteration: i, reason: 'output-not-nfc', input });
      }
    } catch (e) {
      problems.push({ iteration: i, reason: 'threw', detail: e.message, input });
    }
  }
  return { iterations, problems, pass: problems.length === 0 };
}

// --- CLI ---
if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  const args = process.argv.slice(2);
  const get = (flag, def) => {
    const i = args.indexOf(flag);
    return i >= 0 ? args[i + 1] : def;
  };
  const dir = get('--dir', 'corpus/visible');
  const converterPath = get('--converter', join(__dirname, 'reference-converter.mjs'));
  const threshold = parseFloat(get('--threshold', '0.99'));

  const mod = await import(pathToFileURL(resolve(converterPath)).href);
  const convert = mod.convert ?? mod.default;
  if (typeof convert !== 'function') {
    console.error(`Converter module ${converterPath} must export 'convert' or default function.`);
    process.exit(2);
  }

  const report = runCorpus({ convert, dir, threshold });
  const fz = fuzz({ convert });
  report.fuzz = { iterations: fz.iterations, pass: fz.pass, problems: fz.problems.length };

  console.log(JSON.stringify(report, null, 2));
  console.log(
    `\n${report.pass && fz.pass ? 'PASS' : 'FAIL'} — char ${(
      report.overall.charAccuracy * 100
    ).toFixed(2)}%, word ${(report.overall.wordAccuracy * 100).toFixed(2)}%, ` +
      `${report.overall.n} cases, ${report.failures.length} failing, fuzz ${
        fz.pass ? 'ok' : fz.problems.length + ' problems'
      }`
  );
  process.exit(report.pass && fz.pass ? 0 : 1);
}
