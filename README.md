# MyBooks

A self-hosted ebook catalog that reads your books from wherever they already live,
and lets you share them with family and friends without complicated loan limits or per-title/per-user
fees.

**Status:** on hold. The feature set is largely complete and the app has run in
production, but the cloud storage integrations it was designed around depend on
OAuth scopes that both Google and Microsoft have changed without notice, repeatedly.
Reading a user's existing library now requires a restricted scope, which requires a
paid annual third-party security assessment, and the unrestricted workarounds have
already been rebuilt twice. Development is paused until the appropriate scopes are
achievable rather than continuing to re-engineer around moving requirements. The
reasoning is written up in
[The third-party scope problem](#the-third-party-scope-problem) below, since it drove
most of the interesting design decisions here.

---

## The problem it solves

Ebooks are locked to whatever platform you bought them from. Moving a library
between platforms means exporting everything and re-importing it, assuming the
platform lets you export at all. Lending is worse: most platforms cap how often a
title can be loaned, for how long, and to whom.

MyBooks inverts that. Your files stay in your own storage. The app indexes them,
handles metadata, and manages who can read what.

## Architecture

Six ASP.NET Core services behind a reverse proxy, with an Angular front end.

| Service | Responsibility |
| --- | --- |
| AuthService | Authentication, users, roles, invitations, support impersonation |
| CatalogService | Books, genres, series, tags, age ratings, metadata lookup |
| FileService | Uploads, downloads, Google Drive integration, bulk import, reading progress |
| TenantService | Tenant signup and provisioning, billing plans |
| EmailService | Transactional mail |
| SupportService | Impersonation and abuse-report audit logs |

Services authenticate to each other with short-lived system tokens issued by
AuthService and validated against a shared per-service secret, so no service trusts
a caller purely on network position.

**Stack:** .NET 9, Entity Framework Core, SQL Server, Angular 19, Angular Material,
Docker Compose.

**Notable pieces:** JWT auth with BCrypt password hashing; multi-tenant data
isolation enforced through EF query filters; ClamAV malware scanning on every
upload; PDF metadata extraction with PdfPig; Open Library API lookups to fill in
missing book metadata; FluentValidation on request models; HtmlSanitizer on
user-supplied content.

Roles are SuperAdmin, Owner, Admin, Editor, User, plus Support and GlobalReviewer
for internal tooling. Support staff can impersonate a tenant user to reproduce
issues, and every impersonation is written to an audit log.

## The third-party scope problem

The core promise of this app is that your files stay where they already are. That
makes cloud storage permissions load-bearing rather than incidental, and it turned
out to be the wrong thing to build on.

The original design assumed the app could read a user's Drive folder on their
behalf. That assumption failed in stages:

**Google reclassified `drive.readonly` as a restricted scope.** Restricted scopes
require an annual CASA security assessment by an approved third-party assessor if
you store the data server-side, which this app does. Google negotiated a discounted
rate, but it is a recurring cost per app.

**Folder selection stopped cascading.** Under the non-sensitive `drive.file` scope,
selecting a folder in the Google Picker grants access to the folder object but not
to the files inside it. Any design built on "point at a folder, index its contents"
stops working.

**The Drive UI "Open with" path became impractical** as a way to hand files to a
third-party app at any useful volume.

**Then the Picker and `drive.file` behaviour changed again**, requiring a second
rework of an ingest path that had already been rebuilt once.

**OneDrive, the planned second provider, had its own separate but comparable
permission and scope problems**, which ended that work before it started.

None of these changes were announced ahead of time. Each one broke or degraded
functionality that was already built, tested, and working.

A workaround does exist, and it is what the code does today: `drive.file` combined
with the Google Picker, where the user explicitly multi-selects files and the
resulting grant is per-file and persistent. It requires no restricted scope and asks
far less of the user's privacy. Its cost is that newly added books need another trip
through the picker instead of appearing automatically.

I stopped there rather than continuing to re-engineer around it. Two providers had
now changed permission behaviour without notice, twice in Google's case, and every
change landed on the same part of the system. Rebuilding the ingest path a third
time would not have made the fourth change any less likely. The right move is to
wait until the appropriate scope is genuinely achievable — `drive.readonly` and its
Microsoft equivalent — rather than keep shipping workarounds on a foundation that
can move again at any time.

That is a dependency risk decision rather than a technical one. The workaround
functions. It just is not something worth building a product on.

## Other things worth calling out

**PDF to EPUB conversion was attempted and abandoned.** Reflowing a PDF for small
screens works well when the source PDF has a real structural hierarchy. Scanned or
flattened PDFs produced output that was not good enough to put in front of users, so
the feature was cut rather than shipped at partial quality.

**Storage is pluggable but incomplete.** Local disk and Google Drive work; the S3
path is scaffolded but not finished. OneDrive was planned, but its permission model
presented the same class of problem as Drive, so implementation never began.

## Known limitations

- No automated test coverage. This is the largest gap.
- Configuration keys drifted over time. Several sections in `appsettings.json` are
  no longer read by the code and are superseded by environment variables in
  deployment.
- Bulk import retries failures once and then gives up; there is no dead-letter path
  or partial-resume for very large imports.

## Running it locally

Requires Docker Desktop, the .NET 9 SDK, and Node 20+.

```
docker compose -f docker-compose.dev.yml up -d
powershell -ExecutionPolicy Bypass -File .\start-all.ps1
```

SQL Server and ClamAV run in containers; the services and the Angular dev server run
on the host. Each service creates its own schema on first start. The UI is served at
http://localhost:8080 and proxies API calls to the individual services.

## Configuration and secrets

Deployment configuration is supplied through environment variables. The values left
in `appsettings.json` are development placeholders and are not valid credentials for
any live system — there is currently no deployed instance, and anything that was
ever live has been rotated.

Early commits in this repository contain real development credentials (now rotated and no longer functional), which is a
mistake I would not repeat. Secrets belong in environment variables or a secret
store from the first commit, not added later.

## Usage and rights

Copyright (c) 2026 Kathleen Malone. All rights reserved.

This repository is public so that the code can be read as a work sample. It is not
open source. No permission is granted to use, copy, modify, distribute, or sell this
software or any part of it.
