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

## D-0005 — No code signing in release workflow (temporary)

**Decision:** The release workflow ships unsigned installers directly. SignPath integration is deferred.

**Why:** SignPath Foundation's free open-source certificate requires a connector URL configuration that was not ready when v2.0.4 shipped. The workflow was simplified to remove the signing step rather than block the release. Users on Windows will see a SmartScreen warning on first run.

**Path forward:** Apply at https://about.signpath.io/product/open-source. See `docs/SIGNPATH-APPLICATION.md` for the full application details. Once approved, restore the `signpath/github-action-submit-signing-request@v1` step in `release.yml`.
