# Phase 2 — Risk register

Live list of what could go wrong, how likely, how bad, and what we do about it.
Likelihood/Impact: L/M/H. Owner: who acts. Status updates as the project moves.

| # | Risk | L | I | Status | Mitigation (short) |
|---|---|---|---|---|---|
| R1 | Per-run font reading in Word insufficient → mixed-font text dropped | M | H | Open (Spike A) | Self-verifying kit ready; fallbacks below |
| R2 | Snapshot/revert doesn't round-trip formatting | L | H | Open (Spike C) | Kit ready; snapshot text+font props; honest Ctrl+Z story |
| R3 | A font caps below the 99% gate | M | M | Managed | Escape valve: document, runtime-detect, flag spans, sign-off |
| R4 | Full SutonnyMJ glyph map incomplete (rare conjuncts) | M | M | Managed | Grow corpus; loud unsupported handling; escape valve |
| R5 | Headers/footers scope unclear at 1.3 | M | L | Open (spike) | Report as "pending/not scanned"; never silent |
| R6 | Performance: too many context.sync() (prior did ~3/para) | M | M | Managed | Batch reads/writes; budget per sync; Phase-4 calibration spike |
| R7 | Custom-domain dependency (maintainer must own a domain) | M | M | Open (D-0006) | Parameterized manifest; GitHub Pages dev; pick domain pre-launch |
| R8 | Sideload "unknown developer" friction (no paid signing) | H | L | Accepted | Disclosed honestly in install guide |
| R9 | High/critical CVE in a shipped dependency | M | M | Managed | CVE gate blocks shipped+reachable; Dependabot; expiring allowlist |
| R10 | Bundled font licence non-compliance | L | H | Managed | Noto Sans Bengali is SIL OFL; retain OFL/NOTICE; asset audit |
| R11 | Manifest change forces all users to re-sideload | M | M | Managed | Custom domain minimizes; re-sideload notice process |
| R12 | Solo non-technical maintainer continuity | M | H | Managed | CONTINUITY/RUNBOOK/MIGRATION (Phase 6); plain-language guides |
| R13 | Already-Unicode text mis-detected as Bijoy → corruption | L | H | Mitigated | Engine no-op on non-Bijoy (D-0007) + font-class `unicode` skip |
| R14 | English text in a Bijoy-range font mis-converted | L | M | Mitigated | Per-run font gate; only known-Bijoy runs reach the engine |
| R15 | Apply flattens intra-run formatting (prior insertText bug) | M | M | Managed | Per-run apply preserving each run's formatting; snapshot |
| R16 | Held-out corpus leaks to engine-building agents | L | M | Mitigated | Sealed dir; engine agents not given it; gate-only use |
| R17 | Online-first: no network at launch → add-in can't load | M | L | Accepted | Honest "online-first"; clear launch-time error, no false "offline" |
| R18 | Privacy regression (accidental egress/telemetry) | L | H | Managed | No analytics deps; CSP; acceptance test = network monitor shows zero doc egress |

## Detail on the highest-impact items

### R1 — per-run font access (killer Spike A)
**Trigger:** the Spike A kit returns RED or PARTIAL.
**Fallbacks, in order:** (a) finer-grained ranges than word-split; (b) treat a
paragraph as convertible only if its dominant font is a known Bijoy font and
flag mixed paragraphs for user review (degraded but loud); (c) selection-only
conversion (already a planned fast-follow) as the safe path for mixed content.
A RED result reshapes scope **before** Phase 4 — exactly the spike's purpose.

### R2 — revert fidelity (killer Spike C)
**Trigger:** snapshot/restore kit shows any property not round-tripping.
**Fallback:** snapshot a superset of properties; if a property can't be restored
at 1.3, document the limitation and surface it (we never promise byte-identical
OOXML). The reliable revert is the snapshot command, not Ctrl+Z.

### R3 — the 99% escape valve (from the spec)
A font that can't reach 99% after reasonable effort MAY ship iff: (1) the
shortfall is documented in the known-limitations doc, (2) it is detected at
runtime, (3) affected spans are flagged to the user, and (4) it is signed off in
the decision log. This keeps "loud, not silent" even when imperfect.

### R12 — continuity (the maintainer is solo + non-technical)
The single biggest long-term risk. Mitigated by Phase 6 deliverables
(CONTINUITY.md, RUNBOOK.md, MIGRATION.md), Dependabot with plain-language merge
guides, a custom domain so hosting can move, and this whole paper trail
(decision log, do-not-repeat, reuse manifest, corpus) so a future maintainer can
pick up the context.

### R18 — privacy (the headline promise)
No analytics/telemetry dependencies are permitted. A Content-Security-Policy
restricts origins to Microsoft's Office.js CDN + project hosting. **Acceptance
test (from the spec):** run a network monitor during install/convert and confirm
**zero** document-content egress (CDN/host code-loads are expected and allowed).
This test is part of the release checklist.

## Risks explicitly OUT of scope (by MVP decision)
Reverse Unicode→Bijoy, grammar/spell-check, Excel/PowerPoint, runtime
community-pluggable profiles, auto-scan on open (no shared runtime at 1.3) — all
deferred/cut per the spec, so their risks do not apply to the MVP.
