/**
 * Mukti taskpane — the single-screen state machine.
 *
 * Builds exactly to docs/phase2/UI-UX.md (states S0..S6, E0; flows §4; a11y §5)
 * and honours the Phase-3 review fixes (docs/phase3/REVIEW.md):
 *   - one dedicated visually-hidden live region for status (NOT body-wide aria-live);
 *   - never put initial focus on a disabled control (Convert shows "Getting ready…"
 *     and keeps focus until enabled);
 *   - Revert confirms first when documentMatchesSnapshot() is false; snapshot revert
 *     is the PRIMARY action, the platform undo (Ctrl+Z / ⌘Z) is only a fallback;
 *   - unsupported-font / not-scanned reports explain in plain language;
 *   - lang-of-parts: Bangla and Latin fragments carry their own lang attribute.
 *
 * Office.js is the ONLY platform dependency and is loaded via the CDN script tag
 * in taskpane.html. The host adapter (src/host) is built by another agent; here we
 * program against the WordHost interface only.
 */

import type {
  WordHost,
  ScanReport,
  ConversionPlan,
  PlannedEdit,
  UnscannedKind,
} from '../host/contracts';
import { t, formatNumber, type Lang, type StringKey } from './strings';

// Office is provided by the CDN script in taskpane.html.
declare const Office: any;

// ── Language & lang-of-parts ─────────────────────────────────────────────────

const LANG_KEY = 'mukti.lang';
let lang: Lang = 'bn';

/** Read the per-user language preference (a preference only — never document content). */
function loadLangPreference(): void {
  try {
    const saved = Office?.context?.roamingSettings?.get?.(LANG_KEY);
    if (saved === 'bn' || saved === 'en') lang = saved;
  } catch {
    /* settings unavailable before Office.onReady — keep the default. */
  }
}

function saveLangPreference(): void {
  try {
    const rs = Office?.context?.roamingSettings;
    if (!rs) return;
    rs.set(LANG_KEY, lang);
    rs.saveAsync?.();
  } catch {
    /* best effort; the session value still applies. */
  }
}

// ── Tiny DOM helpers (no framework — YAGNI) ──────────────────────────────────

type El = HTMLElement;

function el(tag: string, attrs: Record<string, string> = {}, children: Array<El | string> = []): El {
  const node = document.createElement(tag);
  for (const name of Object.keys(attrs)) node.setAttribute(name, attrs[name]);
  for (const child of children) {
    node.append(typeof child === 'string' ? document.createTextNode(child) : child);
  }
  return node;
}

/** A text fragment tagged with its own lang (lang-of-parts; WCAG 3.1.2). */
function frag(text: string, partLang: 'bn' | 'en'): El {
  return el('span', { lang: partLang }, [text]);
}

function byId(id: string): HTMLElement {
  const node = document.getElementById(id);
  if (!node) throw new Error('missing element: ' + id);
  return node;
}

// ── The state machine ────────────────────────────────────────────────────────

type State =
  | 'idle'
  | 'scanning'
  | 'preview'
  | 'applying'
  | 'done'
  | 'reverted'
  | 'error'
  | 'empty';

interface AppModel {
  state: State;
  ready: boolean; // engine/Office ready — gates the Convert button
  scan: ScanReport | null;
  plan: ConversionPlan | null;
  sampleIndex: number;
  changedRuns: number;
  skippedStale: number;
  snapshotId: string | null;
  errorKind: 'generic' | 'applyfail' | 'unsupported';
}

const model: AppModel = {
  state: 'idle',
  ready: false,
  scan: null,
  plan: null,
  sampleIndex: 0,
  changedRuns: 0,
  skippedStale: 0,
  snapshotId: null,
  errorKind: 'generic',
};

let host: WordHost | null = null;
let scanCancelled = false;

/** Platform-aware label for the native undo key (Mac uses ⌘Z). */
function undoKeyLabel(): string {
  const isMac = typeof Office !== 'undefined' && Office?.context?.platform === 'Mac';
  return isMac ? '⌘Z' : 'Ctrl+Z';
}

// ── Announcements (one dedicated hidden live region — review: a11y) ──────────

function announce(message: string, assertive = false): void {
  const region = byId(assertive ? 'a11y-alert' : 'a11y-status');
  // Clear then set so repeated identical messages are still announced.
  region.textContent = '';
  window.setTimeout(() => {
    region.textContent = message;
  }, 30);
}

// ── Render ───────────────────────────────────────────────────────────────────

function setLang(next: Lang): void {
  lang = next;
  document.documentElement.lang = next;
  saveLangPreference();
  render();
  announce(t('lang.option.' + next as StringKey, lang));
}

function render(): void {
  renderHeader();
  renderBody();
  renderFooter();
  moveFocusToPrimary();
}

function renderHeader(): void {
  const header = byId('header');
  header.replaceChildren();

  header.append(
    el('div', { class: 'brand' }, [
      el('span', { class: 'wordmark', lang: 'bn' }, [t('app.name', 'bn')]),
      el('span', { class: 'wordmark-latin', lang: 'en' }, ['Mukti']),
    ]),
  );

  // Language toggle (two buttons with aria-pressed — simplest accessible toggle).
  const group = el('div', { class: 'lang-toggle', role: 'group', 'aria-label': t('lang.toggle.label', lang) });
  (['bn', 'en'] as Lang[]).forEach((code) => {
    const btn = el('button', {
      type: 'button',
      class: 'lang-btn',
      'aria-pressed': String(lang === code),
      lang: code,
    }, [t('lang.option.' + code as StringKey, lang)]);
    btn.addEventListener('click', () => {
      if (lang !== code) setLang(code);
    });
    group.append(btn);
  });
  header.append(group);
}

function renderFooter(): void {
  const footer = byId('footer');
  footer.replaceChildren();

  const about = el('details', { class: 'about' }, []);
  const summary = el('summary', {}, [
    el('span', { 'aria-hidden': 'true' }, ['ℹ️ ']),
    frag(t('about.title', lang), lang),
  ]);
  about.append(summary);
  about.append(
    el('p', { class: 'privacy' }, [frag(t('about.privacy', lang), lang)]),
    el('p', {}, [frag(t('about.online', lang), lang)]),
    el('p', {}, [frag(t('about.snapshot', lang), lang)]),
    el('p', {}, [frag(t('about.font', lang), lang)]),
  );
  footer.append(about);
  // Always-visible privacy one-liner (truncated by CSS; full text is in About).
  footer.append(el('p', { class: 'privacy-line' }, [frag(t('about.privacy', lang), lang)]));
}

/** Build a bilingual-aware heading. */
function heading(key: StringKey, icon?: string): El {
  const h = el('h1', { class: 'state-heading' }, []);
  if (icon) h.append(el('span', { class: 'state-icon', 'aria-hidden': 'true' }, [icon + ' ']));
  h.append(frag(t(key, lang), lang));
  return h;
}

function primaryButton(id: string, key: StringKey, onClick: () => void, opts: { disabled?: boolean } = {}): El {
  const attrs: Record<string, string> = { type: 'button', class: 'btn btn-primary', id };
  if (opts.disabled) attrs.disabled = 'true';
  const btn = el('button', attrs, [frag(t(key, lang), lang)]);
  btn.addEventListener('click', onClick);
  return btn;
}

function secondaryButton(id: string, key: StringKey, onClick: () => void): El {
  const btn = el('button', { type: 'button', class: 'btn btn-secondary', id }, [frag(t(key, lang), lang)]);
  btn.addEventListener('click', onClick);
  return btn;
}

function renderBody(): void {
  const body = byId('body');
  body.replaceChildren();
  switch (model.state) {
    case 'idle': body.append(viewIdle()); break;
    case 'scanning': body.append(viewScanning()); break;
    case 'preview': body.append(viewPreview()); break;
    case 'applying': body.append(viewApplying()); break;
    case 'done': body.append(viewDone()); break;
    case 'reverted': body.append(viewReverted()); break;
    case 'empty': body.append(viewEmpty()); break;
    case 'error': body.append(viewError()); break;
  }
}

// ── S0 Idle ──────────────────────────────────────────────────────────────────

function viewIdle(): El {
  const wrap = el('section', {}, [
    heading('idle.heading'),
    el('p', { class: 'helper' }, [frag(t('idle.helper', lang), lang)]),
  ]);

  // Convert is disabled until the host signals ready; focus still lands on it
  // (review: never focus a *different* disabled control; keep focus here so it
  // activates the instant it enables).
  const convert = primaryButton('btn-convert', 'btn.convert', () => void startScan(), {
    disabled: !model.ready,
  });
  wrap.append(convert);

  if (!model.ready) {
    wrap.append(el('p', { class: 'note', id: 'preparing-note' }, [
      el('span', { 'aria-hidden': 'true' }, ['⏳ ']),
      frag(t('status.preparing', lang), lang),
    ]));
  }

  wrap.append(el('p', { class: 'reassure' }, [
    el('span', { 'aria-hidden': 'true' }, ['ⓘ ']),
    frag(t('idle.reassure', lang), lang),
  ]));
  return wrap;
}

// ── S1 Scanning ──────────────────────────────────────────────────────────────

function viewScanning(): El {
  const wrap = el('section', {}, [
    el('h1', { class: 'state-heading' }, [frag(t('status.scanning', lang), lang)]),
  ]);
  // Indeterminate progress: respect reduced motion → a static, stepped bar.
  wrap.append(el('div', {
    class: 'progress',
    role: 'progressbar',
    'aria-busy': 'true',
    'aria-label': t('status.scanning', lang),
  }, []));
  wrap.append(el('p', { class: 'note' }, [frag(t('scan.nochange', lang), lang)]));
  wrap.append(secondaryButton('btn-cancel', 'btn.cancel', cancelScan));
  return wrap;
}

// ── S2 Preview ───────────────────────────────────────────────────────────────

function totalConvertibleChars(scan: ScanReport): number {
  let chars = 0;
  for (const run of scan.convertible) chars += run.text.length;
  return chars;
}

function unsupportedFontCounts(scan: ScanReport): Array<{ font: string; runs: number }> {
  const counts = new Map<string, number>();
  for (const run of scan.unsupported) {
    const name = run.format.fontName ?? '—';
    counts.set(name, (counts.get(name) ?? 0) + 1);
  }
  return Array.from(counts, ([font, runs]) => ({ font, runs }));
}

const UNSCANNED_LABEL: Record<UnscannedKind, StringKey | null> = {
  footnote: 'notscanned.footnote',
  endnote: 'notscanned.endnote',
  textbox: 'notscanned.textbox',
  comment: 'notscanned.comment',
  field: 'notscanned.field',
  smartart: 'notscanned.smartart',
  'mixed-font-unresolved': 'notscanned.mixedfont',
  // Headers/footers are "pending", reported distinctly in the body below.
  'header-footer-pending': null,
};

function changedEdits(plan: ConversionPlan): PlannedEdit[] {
  return plan.edits.filter((e) => e.changed);
}

function viewPreview(): El {
  const scan = model.scan!;
  const plan = model.plan!;
  const wrap = el('section', {}, [heading('preview.heading')]);

  // Summary counts (icon + text — never colour alone; WCAG 1.4.1).
  const counts = el('ul', { class: 'counts' }, []);
  counts.append(el('li', { class: 'count-convertible' }, [
    el('span', { 'aria-hidden': 'true' }, ['✔ ']),
    frag(t('preview.count.convertible', lang, {
      runs: plan.changedRuns,
      chars: totalConvertibleChars(scan),
    }), lang),
  ]));
  counts.append(el('li', { class: 'count-asis' }, [
    el('span', { 'aria-hidden': 'true' }, ['• ']),
    frag(t('preview.count.asis', lang, { runs: scan.convertible.length - plan.changedRuns + scan.unsupported.length }), lang),
  ]));
  wrap.append(counts);

  wrap.append(viewSample(plan));

  if (scan.unsupported.length > 0) wrap.append(viewUnsupported(scan));
  if (hasUnscanned(scan)) wrap.append(viewNotScanned(scan));

  const actions = el('div', { class: 'actions' }, []);
  actions.append(primaryButton('btn-apply', 'btn.apply', () => void startApply()));
  actions.append(secondaryButton('btn-cancel', 'btn.cancel', startOver));
  wrap.append(actions);
  return wrap;
}

function viewSample(plan: ConversionPlan): El {
  const samples = changedEdits(plan);
  const region = el('div', { class: 'sample', role: 'group', 'aria-label': t('preview.sample.label', lang) }, []);
  region.append(el('h2', { class: 'sample-heading' }, [frag(t('preview.sample.label', lang), lang)]));

  if (samples.length === 0) return region;
  const idx = Math.min(model.sampleIndex, samples.length - 1);
  const edit = samples[idx];

  // "before" is the raw Bijoy in its original font; "after" is Unicode in Noto.
  region.append(el('p', { class: 'sample-row' }, [
    el('span', { class: 'sample-label' }, [frag(t('preview.sample.before', lang), lang)]),
    el('span', { class: 'sample-before', lang: 'bn', style: fontStyle(edit) }, [edit.before]),
  ]));
  region.append(el('p', { class: 'sample-row' }, [
    el('span', { class: 'sample-label' }, [frag(t('preview.sample.after', lang), lang)]),
    el('span', { class: 'sample-after', lang: 'bn' }, [edit.after]),
  ]));

  if (samples.length > 1) {
    const next = secondaryButton('btn-next-sample', 'btn.sample.next', () => {
      model.sampleIndex = (idx + 1) % samples.length;
      renderBody();
      byId('btn-next-sample').focus();
    });
    region.append(next);
  }
  return region;
}

/** Inline style that previews the run's original Bijoy font (so users recognise their text). */
function fontStyle(edit: PlannedEdit): string {
  const name = edit.format.fontName;
  return name ? 'font-family:"' + name.replace(/"/g, '') + '";' : '';
}

function viewUnsupported(scan: ScanReport): El {
  const items = unsupportedFontCounts(scan);
  const details = el('details', { class: 'report report-warning' }, []);
  details.append(el('summary', {}, [
    el('span', { 'aria-hidden': 'true' }, ['⚠ ']),
    frag(t('warn.unsupported.title', lang, { n: items.length }), lang),
  ]));
  details.append(el('p', {}, [frag(t('warn.unsupported.body', lang), lang)]));
  const list = el('ul', {}, []);
  for (const item of items) {
    list.append(el('li', {}, [
      // Font name is Latin; tag it so it is not read as Bangla.
      frag(t('warn.unsupported.item', lang, { font: item.font, runs: item.runs }), 'en'),
    ]));
  }
  details.append(list);
  details.append(el('p', {}, [frag(t('warn.unsupported.why', lang), lang)]));
  return details;
}

function hasUnscanned(scan: ScanReport): boolean {
  return scan.unscanned.length > 0;
}

function viewNotScanned(scan: ScanReport): El {
  const kinds = scan.unscanned.filter((r) => r.kind !== 'header-footer-pending');
  const hasHeaderFooter = scan.unscanned.some((r) => r.kind === 'header-footer-pending');
  const details = el('details', { class: 'report report-info' }, []);
  details.append(el('summary', {}, [
    el('span', { 'aria-hidden': 'true' }, ['◔ ']),
    frag(t('notscanned.title', lang, { n: kinds.length }), lang),
  ]));
  details.append(el('p', {}, [frag(t('notscanned.body', lang), lang)]));

  const list = el('ul', {}, []);
  for (const region of kinds) {
    const labelKey = UNSCANNED_LABEL[region.kind];
    if (!labelKey) continue;
    list.append(el('li', {}, [
      frag(t(labelKey, lang), lang),
      document.createTextNode(' — ' + formatNumber(region.count, lang)),
    ]));
  }
  details.append(list);

  if (hasHeaderFooter) details.append(el('p', {}, [frag(t('notscanned.headerfooter', lang), lang)]));
  // Plain-language meaning, then the in-scope confirmation (review: explain, not counts).
  details.append(el('p', { class: 'meaning' }, [frag(t('notscanned.meaning', lang), lang)]));
  details.append(el('p', { class: 'inscope' }, [frag(t('notscanned.inscope', lang), lang)]));
  return details;
}

// ── S3 Applying ──────────────────────────────────────────────────────────────

function viewApplying(): El {
  const wrap = el('section', {}, [
    el('h1', { class: 'state-heading' }, [frag(t('status.applying', lang), lang)]),
  ]);
  wrap.append(el('div', {
    class: 'progress',
    role: 'progressbar',
    'aria-busy': 'true',
    'aria-label': t('status.applying', lang),
  }, []));
  // No cancel mid-write (UI-UX §4 / §8): Revert is the recovery path.
  return wrap;
}

// ── S4 Done ──────────────────────────────────────────────────────────────────

function viewDone(): El {
  const wrap = el('section', {}, [heading('done.heading', '✔')]);
  wrap.append(el('p', { class: 'count' }, [frag(t('done.count', lang, { runs: model.changedRuns }), lang)]));
  if (model.skippedStale > 0) {
    wrap.append(el('p', { class: 'note' }, [frag(t('done.skipped', lang, { n: model.skippedStale }), lang)]));
  }
  wrap.append(el('p', { class: 'note' }, [
    el('span', { 'aria-hidden': 'true' }, ['ⓘ ']),
    frag(t('done.font', lang), 'en'),
  ]));

  // Revert is PRIMARY (snapshot-based). Native undo is only a fallback note.
  wrap.append(primaryButton('btn-revert', 'btn.revert', () => void startRevert()));
  wrap.append(el('p', { class: 'note' }, [frag(t('done.revertnote', lang), lang)]));
  wrap.append(secondaryButton('btn-startover', 'btn.startover', startOver));
  wrap.append(el('p', { class: 'note undo-note' }, [
    el('span', { 'aria-hidden': 'true' }, ['ⓘ ']),
    frag(t('done.undonote', lang, { undo: undoKeyLabel() }), lang),
  ]));
  return wrap;
}

// ── S5 Reverted ──────────────────────────────────────────────────────────────

function viewReverted(): El {
  const wrap = el('section', {}, [heading('reverted.heading', '↩')]);
  wrap.append(el('p', {}, [frag(t('reverted.body', lang), lang)]));
  wrap.append(primaryButton('btn-convertagain', 'btn.convertagain', () => void startScan()));
  wrap.append(secondaryButton('btn-startover', 'btn.startover', startOver));
  return wrap;
}

// ── E0 Empty ──────────────────────────────────────────────────────────────────

function viewEmpty(): El {
  const wrap = el('section', {}, [heading('empty.heading')]);
  wrap.append(el('p', {}, [frag(t('empty.body', lang), lang)]));
  // If everything was unsupported font, show *why* nothing converted (UI-UX §7).
  if (model.scan && model.scan.unsupported.length > 0) wrap.append(viewUnsupported(model.scan));
  if (model.scan && hasUnscanned(model.scan)) wrap.append(viewNotScanned(model.scan));
  wrap.append(secondaryButton('btn-startover', 'btn.startover', startOver));
  return wrap;
}

// ── S6 Error ──────────────────────────────────────────────────────────────────

function viewError(): El {
  // Error uses role="alert" via the dedicated assertive live region.
  const wrap = el('section', { class: 'error' }, [heading('error.heading', '✖')]);
  const bodyKey: StringKey =
    model.errorKind === 'applyfail' ? 'error.body.applyfail'
    : model.errorKind === 'unsupported' ? 'error.body.unsupported'
    : 'error.body.generic';
  const vars = model.errorKind === 'applyfail' ? { undo: undoKeyLabel() } : undefined;
  wrap.append(el('p', {}, [frag(t(bodyKey, lang, vars), lang)]));

  if (model.errorKind !== 'unsupported') {
    wrap.append(primaryButton('btn-retry', 'btn.retry', () => void startScan()));
  }
  wrap.append(secondaryButton('btn-startover', 'btn.startover', startOver));
  return wrap;
}

// ── Revert confirm dialog (review: a11y U1 — confirm before data loss) ───────

function showRevertConfirm(): void {
  const body = byId('body');
  const dialog = el('div', {
    class: 'confirm',
    role: 'alertdialog',
    'aria-modal': 'true',
    'aria-labelledby': 'confirm-heading',
    'aria-describedby': 'confirm-body',
  }, []);
  dialog.append(el('h2', { id: 'confirm-heading' }, [frag(t('revert.confirm.heading', lang), lang)]));
  dialog.append(el('p', { id: 'confirm-body' }, [frag(t('revert.confirm.body', lang), lang)]));

  const actions = el('div', { class: 'actions' }, []);
  const confirm = primaryButton('btn-revert-confirm', 'btn.revert.confirm', () => void doRevert());
  const keep = secondaryButton('btn-keep', 'btn.keepchanges', () => {
    dialog.remove();
    byId('btn-revert').focus();
  });
  actions.append(confirm, keep);
  dialog.append(actions);

  body.append(dialog);
  announce(t('revert.confirm.heading', lang), true);
  confirm.focus();
}

// ── Focus management (§4.5) ────────────────────────────────────────────────────

const PRIMARY_FOCUS: Partial<Record<State, string>> = {
  idle: 'btn-convert',
  scanning: 'btn-cancel',
  preview: 'btn-apply',
  done: 'btn-revert',
  reverted: 'btn-convertagain',
  empty: 'btn-startover',
  error: 'btn-retry',
};

function moveFocusToPrimary(): void {
  const id = PRIMARY_FOCUS[model.state];
  if (!id) return;
  const node = document.getElementById(id);
  // Rule 4: focus may land on the disabled Convert button (so it activates the
  // moment it enables), but never on any *other* disabled control.
  if (!node) return;
  if (model.state !== 'idle' && (node as HTMLButtonElement).disabled) return;
  node.focus();
}

// ── Transitions ────────────────────────────────────────────────────────────────

function go(state: State): void {
  model.state = state;
  render();
}

function startOver(): void {
  model.scan = null;
  model.plan = null;
  model.sampleIndex = 0;
  go('idle');
  announce(t('idle.heading', lang));
}

async function startScan(): Promise<void> {
  if (!host || !model.ready) return;
  scanCancelled = false;
  model.sampleIndex = 0;
  go('scanning');
  announce(t('status.scanning', lang));
  try {
    const scan = await host.scanDocument();
    if (scanCancelled) return;
    model.scan = scan;

    if (scan.convertible.length === 0) {
      go('empty');
      announce(t('empty.heading', lang));
      return;
    }
    const plan = await host.buildPlan(scan);
    if (scanCancelled) return;
    model.plan = plan;

    if (plan.changedRuns === 0) {
      go('empty');
      announce(t('empty.heading', lang));
      return;
    }
    go('preview');
    announce(t('preview.heading', lang));
  } catch {
    model.errorKind = 'generic';
    go('error');
    announce(t('error.body.generic', lang), true);
  }
}

function cancelScan(): void {
  // Scan is read-only; cancelling makes no document change (UI-UX §4.1).
  scanCancelled = true;
  startOver();
}

async function startApply(): Promise<void> {
  if (!host || !model.plan) return;
  go('applying');
  announce(t('status.applying', lang));
  try {
    const result = await host.applyPlan(model.plan);
    model.changedRuns = result.changedRuns;
    model.skippedStale = result.skippedStale;
    model.snapshotId = result.snapshotId;
    go('done');
    announce(t('done.heading', lang));
  } catch {
    model.errorKind = 'applyfail';
    go('error');
    announce(t('error.body.applyfail', lang, { undo: undoKeyLabel() }), true);
  }
}

async function startRevert(): Promise<void> {
  if (!host) return;
  const snapshotId = model.snapshotId ?? (await safeLatestSnapshotId());
  if (!snapshotId) {
    model.errorKind = 'generic';
    go('error');
    return;
  }
  model.snapshotId = snapshotId;
  // Confirm first if the document no longer matches the snapshot (avoid silent loss).
  let matches = true;
  try {
    matches = await host.documentMatchesSnapshot(snapshotId);
  } catch {
    matches = false; // unknown → err on the side of confirming.
  }
  if (!matches) {
    showRevertConfirm();
    return;
  }
  await doRevert();
}

async function safeLatestSnapshotId(): Promise<string | null> {
  try {
    return host ? await host.latestSnapshotId() : null;
  } catch {
    return null;
  }
}

async function doRevert(): Promise<void> {
  if (!host || !model.snapshotId) return;
  // Reverting is a transient screen (not a persisted state); paint it directly.
  byId('body').replaceChildren(viewRevertingTransient());
  announce(t('status.reverting', lang));
  try {
    await host.revert(model.snapshotId);
    go('reverted');
    announce(t('reverted.heading', lang));
  } catch {
    model.errorKind = 'applyfail';
    go('error');
    announce(t('error.body.applyfail', lang, { undo: undoKeyLabel() }), true);
  }
}

function viewRevertingTransient(): El {
  return el('section', {}, [
    el('h1', { class: 'state-heading' }, [frag(t('status.reverting', lang), lang)]),
    el('div', { class: 'progress', role: 'progressbar', 'aria-busy': 'true', 'aria-label': t('status.reverting', lang) }, []),
  ]);
}

// ── Global keyboard (Esc closes menus/expanders/dialogs) ─────────────────────

function onKeydown(event: KeyboardEvent): void {
  if (event.key !== 'Escape') return;
  // Close the revert confirm dialog if open.
  const dialog = document.querySelector('.confirm');
  if (dialog) {
    dialog.remove();
    document.getElementById('btn-revert')?.focus();
    return;
  }
  // Cancel an in-progress scan.
  if (model.state === 'scanning') cancelScan();
}

// ── Boot ─────────────────────────────────────────────────────────────────────

/** Wire the host adapter. Exposed so the host module can inject its instance. */
export function attachHost(adapter: WordHost): void {
  host = adapter;
}

export function start(): void {
  loadLangPreference();
  document.documentElement.lang = lang;
  document.addEventListener('keydown', onKeydown);
  render(); // paints the idle screen fast; Convert disabled until ready.

  Office.onReady(async () => {
    // The host module registers itself on window before/around onReady; fall back
    // to a global if attachHost was not called (keeps the UI decoupled from impl).
    if (!host && (window as any).muktiHost) host = (window as any).muktiHost as WordHost;
    loadLangPreference(); // settings are reliable now.

    if (host && !host.isSupported()) {
      model.errorKind = 'unsupported';
      go('error');
      announce(t('error.body.unsupported', lang), true);
      return;
    }
    model.ready = true;
    renderBody(); // re-render idle so Convert enables; focus is preserved on it.
    byId('btn-convert')?.focus();
  });
}

// Auto-start when loaded in the browser/taskpane (not under tsc type-check).
if (typeof document !== 'undefined' && typeof Office !== 'undefined') {
  start();
}
