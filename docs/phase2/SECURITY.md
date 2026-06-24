# Security & privacy controls

How the privacy promise ("your document content never leaves your device; no
telemetry") is *enforced*, not just asserted (Phase 3 security review).

## Content-Security-Policy (D-0012)

The taskpane ships a real CSP `<meta>` (and CI asserts it exists):

```
default-src 'none';
script-src  'self' https://appsforoffice.microsoft.com;   /* Office.js CDN only */
style-src   'self' 'unsafe-inline';
font-src    'self';                                        /* bundled Noto only  */
img-src     'self' data:;
connect-src 'none';                                        /* no network calls   */
```

- **`connect-src 'none'`** — the add-in makes no `fetch`/XHR/WebSocket calls at
  all, so document content has no channel to leave the device.
- **`font-src 'self'`** — the Noto Sans Bengali font is **bundled (self-hosted
  woff2)**, never requested from `fonts.googleapis.com` by name (which would
  beacon to Google on every open).
- **Office.js CDN caveat:** `script-src` must allow Microsoft's CDN
  (`appsforoffice.microsoft.com`). Microsoft mutates those bytes behind a static
  URL, so Subresource-Integrity pinning is not possible; we allowlist exactly
  that one host and disclose this residual trust in the privacy doc.

## No telemetry / no analytics (CI-enforced)

- No analytics/error-reporting/beacon dependencies are permitted. A CI denylist
  check fails the build if a known analytics package or a raw `navigator.sendBeacon`
  / `fetch(` to an external host appears in the bundle.
- The word "telemetry" appears in code only in comments stating its absence.

## The revert snapshot lives inside the .docx (disclosure)

The "Revert Mukti changes" snapshot is stored as a CustomXML part **inside the
document**. It contains the pre-conversion text of changed runs, so it travels if
the user shares the file. This is **disclosed** in the UI and the privacy doc, and
the user can clear it ("Forget Mukti's undo data"). It never leaves the device.

## Acceptance test (release-blocking)

Per the spec: with a network monitor running (e.g. Fiddler / browser DevTools
Network tab / `mitmproxy`), perform a full install + scan + convert + revert and
confirm **zero document-content egress**. Expected/allowed traffic: the Office.js
CDN code-load and the project's own hosting code-load at launch — and nothing
else. Record the capture in the release checklist.

## Secrets & supply chain (recap; see BUILD-CI.md)

- `.gitignore` blocks `*.key/*.p12/*.pem/certs/signing/.env`; secret-scanning +
  push-protection on; dev TLS certs generated at build time.
- CVE gate (osv-scanner) blocks high/critical in shipped + reachable deps;
  Dependabot on; CI actions pinned by SHA; the published artifact ships a
  `SHA256SUMS` checksum.
