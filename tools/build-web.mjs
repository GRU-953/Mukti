// Bundle the task pane + commands into browser JS for the Word add-in, and copy
// the static HTML/CSS into dist/. Uses esbuild (one dependency, no config) — the
// simplest bundler that produces valid browser output. Type-checking is done
// separately by `tsc -p tsconfig.web.json` (esbuild only strips/bundles).
import { build } from 'esbuild';
import { copyFileSync, mkdirSync, existsSync, cpSync } from 'node:fs';

mkdirSync('dist', { recursive: true });

await build({
  entryPoints: ['src/taskpane/taskpane.ts', 'src/commands/commands.ts'],
  bundle: true,
  format: 'esm',
  outdir: 'dist',
  target: ['chrome100', 'edge100', 'safari15'],
  sourcemap: true,
  logLevel: 'info',
});

// Static files the bundles are referenced from.
for (const f of [
  ['src/taskpane/taskpane.html', 'dist/taskpane.html'],
  ['src/taskpane/taskpane.css', 'dist/taskpane.css'],
  ['src/commands/commands.html', 'dist/commands.html'],
]) {
  copyFileSync(f[0], f[1]);
}

// Bundled assets (the self-hosted Noto Sans Bengali woff2 lands here at packaging).
if (existsSync('assets')) cpSync('assets', 'dist/assets', { recursive: true });

console.log('Built dist/taskpane.js, dist/commands.js (+ html/css).');
