# Security Policy

## Reporting a vulnerability

Please **do not** open a public issue for a security or privacy problem. Instead,
report it privately via GitHub's **"Report a vulnerability"** button on the
repository's **Security** tab (Security advisories), or by contacting the
maintainer (GRU-953) privately. You'll get an acknowledgement and, where valid, a
fix and credit.

## Mukti's security & privacy stance

- **Your document content never leaves your device** — no text, filenames, or
  metadata are transmitted, and there is no telemetry. This is enforced, not just
  promised: a strict Content-Security-Policy (`connect-src 'none'`), a
  self-hosted font, and a "no analytics dependency" CI check. Details and the
  network-monitor acceptance test are in
  [`docs/phase2/SECURITY.md`](docs/phase2/SECURITY.md).
- **No secrets in the repository.** Keys, certificates and `.env` files are
  git-ignored and blocked by push protection; development certificates are
  generated locally; the release pipeline holds no secrets (it uses short-lived
  GitHub OIDC).
- **Supply chain.** Dependencies are pinned (`npm ci`), watched by Dependabot,
  and a CVE gate blocks high/critical issues in shipped code. Released artefacts
  ship a `SHA256SUMS` checksum.
- **Online-first.** Like every Office add-in, Mukti loads its program code from
  Microsoft's CDN and the project's hosting when it starts.

## Supported versions

Mukti is in **public beta** (0.x). Security fixes target the latest release.
