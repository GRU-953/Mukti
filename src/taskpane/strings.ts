/**
 * Bilingual string table for Mukti's taskpane.
 *
 * Source of truth: docs/phase2/UI-UX.md §3. Bangla is the default; English is the
 * toggle. Keys are stable; values carry both languages so the UI never hard-codes
 * a sentence. Placeholders like {runs}/{total} are filled by `t()` below.
 *
 * Numerals: Bangla mode renders Bangla-Indic digits (০১২৩…); English mode uses
 * ASCII digits. `formatNumber()` handles the conversion.
 */

export type Lang = 'bn' | 'en';

/** Every UI string, keyed by id, with Bangla (default) + English. */
export const STRINGS = {
  'app.name': { bn: 'মুক্তি', en: 'Mukti' },
  'app.tagline': {
    bn: 'বিজয়/সুটনিএমজে লেখা ইউনিকোডে রূপান্তর',
    en: 'Convert Bijoy/SutonnyMJ to Unicode',
  },
  'lang.toggle.label': { bn: 'ভাষা', en: 'Language' },
  'lang.option.bn': { bn: 'বাংলা', en: 'Bangla' },
  'lang.option.en': { bn: 'ইংরেজি', en: 'English' },
  'idle.heading': {
    bn: 'বিজয়/সুটনিএমজে লেখা ইউনিকোডে রূপান্তর করুন',
    en: 'Convert Bijoy/SutonnyMJ text to Unicode',
  },
  'idle.helper': {
    bn: 'এই কাজটি পুরো নথিতে চলবে। পরিবর্তন প্রয়োগের আগে আপনি প্রিভিউ দেখতে পাবেন।',
    en: 'This runs on the whole document. You will see a preview before any change is applied.',
  },
  'idle.reassure': {
    bn: 'পরিবর্তন প্রয়োগের আগে কিছুই বদলাবে না।',
    en: 'Nothing changes until you apply.',
  },
  'btn.convert': { bn: 'নথি রূপান্তর করুন', en: 'Convert document' },
  'status.preparing': { bn: 'প্রস্তুত হচ্ছে…', en: 'Getting ready…' },
  'status.scanning': { bn: 'নথি স্ক্যান করা হচ্ছে…', en: 'Scanning your document…' },
  'scan.progress': { bn: 'অনুচ্ছেদ {done} / {total}', en: 'Paragraph {done} / {total}' },
  'scan.nochange': { bn: 'কোনো পরিবর্তন এখনো করা হয়নি।', en: 'No changes have been made yet.' },
  'btn.cancel': { bn: 'বাতিল', en: 'Cancel' },
  'preview.heading': { bn: 'প্রিভিউ — যা পরিবর্তন হবে', en: 'Preview — what will change' },
  'preview.count.convertible': {
    bn: 'রূপান্তরযোগ্য: {runs} অংশ ({chars} অক্ষর)',
    en: 'Convertible: {runs} runs ({chars} chars)',
  },
  'preview.count.asis': {
    bn: 'অপরিবর্তিত (ইউনিকোড/ইংরেজি): {runs} অংশ',
    en: 'Left as-is (Unicode/English): {runs} runs',
  },
  'preview.sample.label': { bn: 'নমুনা — আগে → পরে', en: 'Sample — before → after' },
  'preview.sample.before': { bn: 'আগে (বিজয়)', en: 'Before (Bijoy)' },
  'preview.sample.after': { bn: 'পরে (ইউনিকোড)', en: 'After (Unicode)' },
  'btn.sample.next': { bn: 'পরবর্তী নমুনা', en: 'Next sample' },
  'btn.apply': { bn: 'প্রয়োগ করুন', en: 'Apply' },
  'warn.unsupported.title': {
    bn: 'অসমর্থিত ফন্ট পাওয়া গেছে ({n}টি)',
    en: 'Unsupported font found ({n})',
  },
  'warn.unsupported.body': {
    bn: 'এই ফন্টগুলো পরিচিত বিজয় তালিকায় নেই, তাই নিরাপত্তার জন্য রূপান্তর করা হবে না:',
    en: 'These fonts are not on the known Bijoy list, so they will NOT be converted:',
  },
  'warn.unsupported.item': { bn: '"{font}" — {runs} অংশ', en: '"{font}" — {runs} runs' },
  'warn.unsupported.why': {
    bn: 'এগুলো অপরিবর্তিত থাকবে। ভুল রূপান্তর এড়াতে আমরা অনুমান করি না।',
    en: 'They are left untouched — we never guess.',
  },
  'notscanned.title': { bn: 'যা স্ক্যান করা হয়নি ({n}টি ধরন)', en: 'Not scanned ({n} kinds)' },
  'notscanned.body': {
    bn: 'এই অংশগুলো এই সংস্করণে পরীক্ষা করা হয় না এবং স্ক্যান করা হয়নি:',
    en: 'These regions are out of scope this version and were NOT scanned:',
  },
  'notscanned.footnote': { bn: 'ফুটনোট', en: 'Footnotes' },
  'notscanned.endnote': { bn: 'এন্ডনোট', en: 'Endnotes' },
  'notscanned.textbox': { bn: 'টেক্সট বক্স', en: 'Text boxes' },
  'notscanned.comment': { bn: 'মন্তব্য', en: 'Comments' },
  'notscanned.field': { bn: 'ফিল্ড', en: 'Fields' },
  'notscanned.smartart': { bn: 'স্মার্টআর্ট', en: 'SmartArt' },
  // A run whose font Word could not resolve to a single value (review: O1).
  'notscanned.mixedfont': {
    bn: 'মিশ্র-ফন্ট অংশ (নিরাপত্তার জন্য বাদ)',
    en: 'Mixed-font runs (skipped for safety)',
  },
  'notscanned.headerfooter': {
    bn: 'হেডার/ফুটার: এই সংস্করণে মুলতবি।',
    en: 'Headers/footers: pending this version.',
  },
  'notscanned.inscope': {
    bn: 'মূল লেখা ও টেবিল স্ক্যান করা হয়েছে।',
    en: 'Body text and tables were scanned.',
  },
  // Plain-language meaning of the not-scanned report (review: a11y — explain, not counts).
  'notscanned.meaning': {
    bn: 'এই অংশগুলোর কোনো লেখা পরিবর্তন হবে না — সেগুলো যেমন আছে তেমনই থাকবে।',
    en: 'No text in these regions will change — they are left exactly as they are.',
  },
  'btn.viewmore': { bn: 'দেখুন', en: 'View' },
  'btn.viewless': { bn: 'লুকান', en: 'Hide' },
  'status.applying': { bn: 'পরিবর্তন প্রয়োগ করা হচ্ছে…', en: 'Applying changes…' },
  'apply.progress': { bn: '{done} / {total} অংশ লেখা হচ্ছে', en: 'Writing {done} / {total} runs' },
  'done.heading': { bn: 'রূপান্তর সম্পন্ন', en: 'Conversion complete' },
  'done.count': { bn: '{runs} অংশ ইউনিকোডে রূপান্তরিত হয়েছে।', en: '{runs} runs converted to Unicode.' },
  // Reported when the document changed under us mid-apply (TOCTOU; contract: skippedStale).
  'done.skipped': {
    bn: '{n}টি অংশ বাদ পড়েছে কারণ প্রয়োগের সময় নথি বদলে গিয়েছিল।',
    en: '{n} runs were skipped because the document changed during apply.',
  },
  'done.font': { bn: 'ফন্ট সেট করা হয়েছে: Noto Sans Bengali', en: 'Output font set to: Noto Sans Bengali' },
  'btn.revert': { bn: 'মুক্তির পরিবর্তন ফিরিয়ে নিন', en: 'Revert Mukti changes' },
  'btn.startover': { bn: 'নতুন করে শুরু', en: 'Start over' },
  // Snapshot revert is PRIMARY; the platform undo is only a fallback (review: a11y).
  'done.revertnote': {
    bn: 'ফিরিয়ে নিলে মুক্তির করা পরিবর্তনগুলো সরিয়ে নথি আগের অবস্থায় ফেরানো হবে।',
    en: 'Revert removes Mukti’s changes and restores the document to its earlier state.',
  },
  'done.undonote': {
    bn: 'বিকল্প হিসেবে {undo} চাপতে পারেন (কত বার চাপতে হবে তা Word ঠিক করে)।',
    en: 'As a fallback you can press {undo} (Word decides how many presses).',
  },
  'status.reverting': { bn: 'পরিবর্তন ফিরিয়ে নেওয়া হচ্ছে…', en: 'Reverting changes…' },
  'reverted.heading': { bn: 'পরিবর্তন ফিরিয়ে নেওয়া হয়েছে', en: 'Changes reverted' },
  'reverted.body': {
    bn: 'মুক্তির করা পরিবর্তনগুলো সরিয়ে নথি আগের অবস্থায় ফেরানো হয়েছে।',
    en: 'Mukti’s changes were removed and the document was restored.',
  },
  'btn.convertagain': { bn: 'আবার রূপান্তর করুন', en: 'Convert again' },
  // Shown before reverting when the document no longer matches the snapshot (review: a11y U1).
  'revert.confirm.heading': {
    bn: 'ফিরিয়ে নেওয়ার আগে নিশ্চিত করুন',
    en: 'Confirm before reverting',
  },
  'revert.confirm.body': {
    bn: 'রূপান্তরের পর আপনি নথিতে পরিবর্তন করেছেন বলে মনে হচ্ছে। ফিরিয়ে নিলে মুক্তির রূপান্তরিত অংশগুলো আগের লেখায় ফিরবে এবং সেখানে আপনার পরে করা সম্পাদনা হারিয়ে যেতে পারে।',
    en: 'It looks like you edited the document after converting. Reverting will restore Mukti’s converted runs to their earlier text, and any edits you made there afterwards may be lost.',
  },
  'btn.revert.confirm': { bn: 'হ্যাঁ, ফিরিয়ে নিন', en: 'Yes, revert' },
  'btn.keepchanges': { bn: 'না, রেখে দিন', en: 'No, keep my changes' },
  'empty.heading': { bn: 'রূপান্তরযোগ্য কিছু পাওয়া যায়নি', en: 'Nothing to convert' },
  'empty.body': {
    bn: 'মূল লেখা ও টেবিলে কোনো সমর্থিত বিজয় লেখা পাওয়া যায়নি।',
    en: 'No supported Bijoy text was found in the body or tables.',
  },
  'error.heading': { bn: 'কিছু একটা ভুল হয়েছে', en: 'Something went wrong' },
  'error.body.generic': {
    bn: 'রূপান্তর সম্পূর্ণ করা যায়নি। নথিতে কোনো পরিবর্তন করা হয়নি।',
    en: 'The conversion could not be completed. No changes were made to your document.',
  },
  'error.body.applyfail': {
    bn: 'পরিবর্তন প্রয়োগে সমস্যা হয়েছে। অনুগ্রহ করে নথিতে {undo} চাপুন এবং আবার চেষ্টা করুন।',
    en: 'Applying changes failed. Please press {undo} in the document and try again.',
  },
  'error.body.unsupported': {
    bn: 'এই Word সংস্করণটি মুক্তির জন্য প্রয়োজনীয় সুবিধা (WordApi ১.৩) সমর্থন করে না।',
    en: 'This version of Word does not support the features Mukti needs (WordApi 1.3).',
  },
  'btn.retry': { bn: 'আবার চেষ্টা করুন', en: 'Try again' },
  'about.title': { bn: 'পরিচিতি', en: 'About' },
  'about.privacy': {
    bn: 'আপনার নথির লেখা কখনো আপনার ডিভাইস ছাড়ে না — কোনো লেখা, ফাইলের নাম বা মেটাডেটা পাঠানো হয় না; কোনো টেলিমেট্রি নেই।',
    en: 'Your document content never leaves your device — no text, filenames, or metadata are transmitted; no telemetry.',
  },
  'about.online': {
    bn: 'মুক্তি অনলাইন-ফার্স্ট: চালু হলে কোডটি মাইক্রোসফটের CDN ও প্রকল্পের হোস্টিং থেকে লোড হয়। (এটি "অফলাইন" নয়।)',
    en: 'Mukti is online-first: code loads from Microsoft’s CDN and project hosting at launch. (This is not “offline”.)',
  },
  // Disclosure that the revert snapshot travels inside the .docx (security F1.3).
  'about.snapshot': {
    bn: 'ফেরানোর জন্য আগের লেখার একটি কপি নথির ভেতরে রাখা হয়, তাই ফাইল শেয়ার করলে তা সঙ্গে যায়। এটি ডিভাইস ছাড়ে না।',
    en: 'A copy of the earlier text is stored inside the document so it can be reverted, so it travels if you share the file. It never leaves your device.',
  },
  'about.font': {
    bn: 'আউটপুট ফন্ট: Noto Sans Bengali (SIL OFL)। লাইসেন্স অ্যাপের সাথে দেওয়া আছে।',
    en: 'Output font: Noto Sans Bengali (SIL OFL). Licence is bundled with the app.',
  },
  'about.version': { bn: 'সংস্করণ {version}', en: 'Version {version}' },
} as const;

export type StringKey = keyof typeof STRINGS;

const BANGLA_DIGITS = ['০', '১', '২', '৩', '৪', '৫', '৬', '৭', '৮', '৯'];

/** Render a number in the active language's digits (Bangla-Indic in bn, ASCII in en). */
export function formatNumber(value: number, lang: Lang): string {
  const ascii = String(value);
  if (lang === 'en') return ascii;
  return ascii.replace(/[0-9]/g, (d) => BANGLA_DIGITS[Number(d)]);
}

type Vars = Readonly<Record<string, string | number>>;

/**
 * Look up a string for the active language and fill {placeholders}.
 * Numeric variables are localised to the active language's digits.
 */
export function t(key: StringKey, lang: Lang, vars?: Vars): string {
  let out: string = STRINGS[key][lang];
  if (vars) {
    for (const name of Object.keys(vars)) {
      const raw = vars[name];
      const value = typeof raw === 'number' ? formatNumber(raw, lang) : raw;
      out = out.split('{' + name + '}').join(value);
    }
  }
  return out;
}
