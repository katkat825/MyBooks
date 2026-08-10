# MyBooks end-to-end suite

Playwright regression coverage for the MyBooks web client, driving the real Angular app
against a running backend.

## Setup

```
npm install
npm run install:browsers
cp .env.example .env
```

Fill in `.env` with credentials for four accounts that already exist in the target
environment: an Owner, a plain User, a SuperAdmin and a GlobalReviewer. The suite signs in
through the real login form rather than minting tokens, so these must be genuine accounts.

## Running

```
npm test                 # everything
npm run test:smoke       # the @smoke subset
npm run test:ui          # interactive runner
npm run test:headed      # watch it drive the browser
npm run report           # open the last HTML report
```

By default the runner starts the Angular dev server itself and targets
`http://localhost:8080`. Point it elsewhere with `BASE_URL`:

```
BASE_URL=https://qa.mybookcatalog.com npm test
```

## Layout

| Path | Contents |
| --- | --- |
| `tests/auth.setup.ts` | Signs in once per role and saves the storage states |
| `tests/public/` | Everything reachable without a session, run with no stored credentials |
| `tests/access-control/` | Role and guard enforcement across every route |
| `tests/auth/` | Session lifecycle |
| `tests/books/` | Catalogue, book form, details, reader |
| `tests/account/` | Owner administration |
| `tests/support/` | SuperAdmin and GlobalReviewer portals |
| `pages/` | Page objects |
| `fixtures/` | Test fixtures wiring page objects into `test` |
| `utils/` | Role credentials and unique-value helpers |

## Conventions

Tests are tagged rather than split into separate suites. `@smoke` marks the subset worth
running on every push; `@mobile` marks the cases that run against a phone viewport.

Selectors prefer the ids already present in the templates. Where an id repeats inside an
`*ngFor`, or collides across components rendered at the same time, the page object scopes
it to a container instead — `pages/admin.page.ts` and `pages/nav-bar.ts` are the two
places this matters most.

Anything that depends on data existing in the target environment calls `test.skip` with a
reason rather than failing, so a fresh database produces an honest "skipped" instead of a
misleading red.

## Known gaps

The Google Drive OAuth handshake and the Google Picker are cross-origin and cannot be
driven from here. The suite asserts the outbound authorisation request instead, including
that the requested scope is still `drive.file` and has not regressed to a restricted one.

File upload through the picker is likewise uncovered. Book creation is exercised via the
skip-file path.
