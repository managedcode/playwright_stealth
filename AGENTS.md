# ManagedCode.Playwright.Stealth

Owner: ManagedCode maintainers

## Self-Learning Rules
- When the user states a clear directive ("always", "never", "the process is", "remember this"), add it to "Rules to follow" with the task scope it applies to.
- If the directive is path- or module-specific, scope it to the nearest folder; otherwise apply it repo-wide.
- If a new rule conflicts with an existing one, prefer the newest explicit user instruction and update the old rule.
- Record stable patterns, corrections, and strong preferences here; chat is not memory.

## Development Flow
1. Read `AGENTS.md` and `README.md` before making changes.
2. Inspect current stealth scripts and tests to avoid breaking bot-detection coverage.
3. Implement changes with tests in the same PR.
4. Run build/analyze, then unit tests, then integration tests.
5. Update documentation and examples to match behaviour.
6. Propose `AGENTS.md` updates when stable patterns appear (owner approval required).

## Testing Discipline
- Any change to stealth scripts or config requires integration coverage on multiple bot-detection sites.
- Run build and analyzers before tests.
- Integration tests require Playwright browsers installed and network access.
- Failures block completion.
- Definition of Done: build + analyzers + unit tests + integration tests are green.
- Test order: new/modified tests, then related suites, then full required suites.

## Coding Rules
- C# 14, .NET 10, nullable enabled, warnings treated as errors.
- Avoid breaking public API without explicit request.
- Apply stealth to the browser context before creating pages.
- JS scripts live in `src/Playwright.Stealth/Resources/js`; keep file names in sync with `StealthScriptProvider`.
- Use `ConfigureAwait(false)` in library code.
- Centralize test URLs and constants; avoid scattering literal duplicates.

## Commands
- build: `dotnet build -c Release ManagedCode.Playwright.Stealth.slnx`
- analyze: `dotnet build -c Release /p:RunAnalyzers=true ManagedCode.Playwright.Stealth.slnx`
- test: `dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release`
- test:google: `RUN_GOOGLE_SEARCH_TESTS=1 dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release`
- format: `dotnet format --verify-no-changes`

## Maintainer Preferences
- Keep diffs small and focused.
- Prefer file-scoped namespaces and explicit types when the type is not obvious.
- Test names follow `Method_Scenario_ExpectedResult`.
- Avoid "Arrange/Act/Assert" comments in tests.

## Rules to follow
- Always run build and tests before finishing a task.
- Root `AGENTS.md` changes require owner review and approval.
