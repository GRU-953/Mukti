# Decision Log

Engineering decisions that would be non-obvious to a future contributor.

---

## D-0001 — No reverse conversion (Unicode → Bijoy)

**Decision:** Mukti will never implement Unicode-to-Bijoy reverse conversion.

**Why:** The original Mukti v1 shipped a reverse converter. It caused silent data loss — converted text looked correct on screen but had subtle glyph mismatches that weren't detectable until printing or sharing. Users lost work. The decision is permanent: encode forward (Bijoy → Unicode) only, and provide Undo as the only way to reverse a conversion.

**Impact:** The Undo button stores the original document content in memory for the session. Once the document is closed or the session ends, the conversion is irreversible.

---

## D-0002 — Offline-only, no telemetry

**Decision:** Mukti makes no outbound network connections during document conversion.

**Why:** Mukti processes sensitive documents — HR forms, legal text, financial records, and organisational reports in Bengali. Sending any document fragment to a server would be a hard privacy violation. The only network call is the update check (`api.github.com/repos/GRU-953/Mukti/releases/latest`), which sends no document content and is fire-and-forget.

**Impact:** No analytics, no crash reporting, no document-content telemetry of any kind.

---

## D-0003 — Corpus gate: all mapping changes require ≥99% test pass rate

**Decision:** Any change to `data/bijoy-sutonnymj.json` must leave `dotnet test` reporting ≥99% pass rate on the 387-case corpus before merging.

**Why:** The glyph map has cascading effects — a single wrong entry can corrupt thousands of runs across hundreds of documents. The 387-case corpus (`src/Mukti.Engine.Tests/`) is the only objective gate. Before the gate existed, breaking changes shipped silently.

**How:** Run `dotnet test src/Mukti.Engine.Tests/Mukti.Engine.Tests.csproj` before submitting a PR. CI enforces this on every push. PRs that drop the pass rate below 99% will not be merged regardless of other rationale.

---

## D-0004 — ARM64 build is framework-dependent, not self-contained

**Decision:** The ARM64 Windows build uses `--no-self-contained`, while the x64 build uses `--self-contained true`.

**Why:** `--self-contained true` requires `<EnableComHosting>true</EnableComHosting>` to bundle the .NET runtime inside the COM-host DLL. The .NET 8 SDK cross-compilation target for `win-arm64` does not support this combination — the assets file has no `net8.0-windows/win-arm64` target when self-contained is set for a COM hosting project. Framework-dependent works and keeps the ARM64 installer small (2.7 MB vs ~45 MB).

**Impact:** ARM64 users must have .NET 8 runtime installed. The ARM64 installer shows a notice about this. Since ARM64-native Office is not yet available (Office runs x64 emulated on ARM64 Windows), this is an acceptable trade-off for a preview build.

---

## D-0005 — Mukti ships unsigned, by design (code signing abandoned)

**Decision:** Mukti's installers are distributed unsigned on both Windows and macOS. Code signing is not pursued — not deferred, abandoned.

**Why:** Authenticode (Windows) and Apple Developer (macOS) signing both add ongoing cost and process overhead with no benefit that matters for this project. Mukti is free, fully offline, and open-source: the complete source is public and reproducible from the GitHub Actions workflow, which is a stronger trust signal for this audience than a paid certificate. The earlier SignPath Foundation route added a third-party dependency and connector configuration that wasn't worth the maintenance burden, so it was dropped entirely.

**Impact:** Windows users see a one-time SmartScreen prompt ("Windows protected your PC" → More info → Run anyway). macOS users right-click → Open to bypass Gatekeeper on first launch. Both are documented in the README install steps. No signing secrets, connectors, or external accounts are part of the build.

**Consequence for the release workflow:** `release.yml` builds and publishes installers directly with no signing step, and none should be re-added.

---

## D-0006 — Bijoy detection is an exact-match allowlist, never an "MJ-suffix" heuristic

**Decision:** A font is converted only if its (comma-stripped, normalised) name is on the curated Bijoy allowlist in `FontRegistry`. The "...MJ" suffix is never treated as proof that a font is legacy-Bijoy.

**Why:** An empirical pass over the 757-document test library (`D:\Test_files`) showed the MJ suffix is unreliable. Fonts whose names end in "MJ" but whose runs contain **already-Unicode Bengali** text:
- `TangonMotaMJ` — runs like `এ এন্টারপ্রাইজের ব্যাকওয়ার্ড লিংকেজ…` (U+0980–U+09FF)
- `ArhialkhanMJ` — `য়ীরা`
- `SonkhoMJ` — `৬`
- `NikoshMJ`, `SutonnyOMJ` — documented Unicode fonts from the prior codebase

Converting any of these would corrupt valid Unicode. So MJ-suffix fuzzy matching is permanently rejected; only verified names are added.

**Siyam Rupali ANSI is the opposite case.** Its runs are legacy ASCII+high-byte mojibake (`Avwg`, `evsjv`, `Kw¤úDUvi`). Test-converting them through the engine produced correct Bengali (`আমি`, `বাংলা`, `কম্পিউটার`), proving the "ANSI" build uses the Bijoy/Ekushey byte layout. It was added to the allowlist as the exact string `siyam rupali ansi` — **plain `siyam rupali` (Unicode) is deliberately excluded.**

**Defence in depth:** Even on a Bijoy-listed font, `Converter.Convert` is a no-op when `IsBijoyText` finds no ASCII source glyphs, so an already-Unicode run under a Bijoy-named font is never mangled. Both the Windows (`OfficeIntegration`) and Mac (`office-interop.js`) paths convert only `FontClass.Bijoy` runs; English and Unicode-Bengali fonts are left untouched.

**How:** `Normalize` drops everything from the first comma (style/fallback decoration like `SutonnyMJ,Bold` or `Calibri, sans-serif`). Variant spellings (`sutonnymjbold`, `sutonnymj-regular`) are explicit list entries, not inferred. The standalone `tools/AuditScanner` matches the same family set so the audit reflects exactly what the add-in converts.
