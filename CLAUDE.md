# MouseJiggler — guide for AI/code agents

Console app that posts a mouse-moved event at the pointer's current position every 10 s, so the OS never goes idle (Teams stays "Available", screen doesn't lock). The cursor does not visibly move. Stops by itself at 18:00 local time by default. macOS only today; ports are ready for other OS adapters.

## Commands (run from this folder)

Shortcuts in `Makefile` (`make` lists them): `make run` / `make run-dev` (alias; extra options via `ARGS="--interval 30"`), `make demo` (2 s × 7 s), `make build`, `make test`, `make clean`. Keep the Makefile in sync when commands change.

```bash
dotnet build                                   # TreatWarningsAsErrors: must be 0 warnings
dotnet test                                    # 95 tests: 2 real-mouse tests skip without Accessibility permission
dotnet run --project src/MouseJiggler.Console                              # defaults from appsettings.json, Ctrl+C to stop
dotnet run --project src/MouseJiggler.Console -- --interval 2 --duration 7 # bounded demo run
dotnet run --project src/MouseJiggler.Console -- --help
```

CLI options (override `appsettings.json` → section `Jiggler`; env vars `Jiggler__IntervalSeconds` etc. also work):

| Option | Config key | Default | Meaning |
|---|---|---|---|
| `--interval <seconds>` | `Jiggler:IntervalSeconds` | 10 | time between jiggles (first jiggle is immediate) |
| `--duration <seconds>` | `Jiggler:DurationSeconds` | none | auto-stop after this long |
| `--stop-at <HH:mm>` | `Jiggler:StopAt` | `18:00` | auto-stop at this local time; empty config value = none. With `--duration`, whichever comes first. Already past at start → prints a message, exits 0 without running (never rolls over to tomorrow) |
| `-h`, `--help` | — | — | usage |

Exit codes (`src/MouseJiggler.Console/ExitCodes.cs`): 0 ok / Ctrl+C, 1 pointer unavailable (no Accessibility permission or unsupported OS), 2 invalid arguments.

Stack: .NET 10 (net10.0), xUnit 2, Shouldly (not FluentAssertions), NSubstitute, FakeTimeProvider, FakeLogger, NetArchTest. Central Package Management: versions only in `Directory.Packages.props`; shared settings and test packages/usings in `Directory.Build.props` (any project ending in `Tests` gets xUnit/Shouldly/NSubstitute automatically).

## Solution map (dependencies point inwards)

`Console → Application → Domain` and `Console → Infrastructure → Application → Domain`. Enforced by `tests/MouseJiggler.Console.Tests/ArchitectureTests.cs`.

| Project | Responsibility |
|---|---|
| `src/MouseJiggler.Domain` | Pure value types and rules (`ScreenPoint`, `StopTime`). No packages. |
| `src/MouseJiggler.Application` | Use cases + ports (interfaces). Only `*.Abstractions` packages (DI, Logging). |
| `src/MouseJiggler.Infrastructure` | OS adapters (CoreGraphics P/Invoke), chosen per OS in `AddInfrastructure`. |
| `src/MouseJiggler.Console` | Composition root, CLI parsing, options, terminal output. Root namespace `MouseJiggler.ConsoleApp` (avoids clash with `System.Console`). Types are `internal`; tests see them via `InternalsVisibleTo`. |

## Key file index

Domain (`src/MouseJiggler.Domain/`)
- `ScreenPoint.cs` — global coordinates, Y grows downwards (CoreGraphics convention); invariant `ToString`.
- `StopTime.cs` — local time of day; `RemainingFrom(now, zone)` = real time left today (DST-aware), ≤0 once passed.

Application (`src/MouseJiggler.Application/`)
- `Ports/IMouseController.cs` — `GetPosition`, `MoveTo` (must emit real input events).
- `Ports/IPointerReadiness.cs` — `Check()` → `PointerReadiness(IsReady, Problem)`.
- `JiggleMouse.cs` — use case: one jiggle = a single `MoveTo(GetPosition())` (in place; returns the position).
- `KeepAwakeSession.cs` / `IKeepAwakeSession.cs` — use case: readiness gate + timed loop with `TimeProvider`; ends on cancellation or `TimeLimit` (no jiggle if ≤0); one `ILogger` line per jiggle (`[LoggerMessage]`).
- `JiggleSchedule.cs` — interval, optional duration and `StopAt` (validated); `TimeLimit(TimeProvider)` = min(duration, time until stop) or null. `KeepAwakeOutcome.cs` — result (ran + count, or problem).
- `DependencyInjection.cs` — `AddApplication()` (also `TryAdd TimeProvider.System`).

Infrastructure (`src/MouseJiggler.Infrastructure/`)
- `MacOS/NativeMethods.cs` — all P/Invoke (`LibraryImport`): CGEvent*, CFRelease (via `CFTypeHandle` SafeHandle), AXIsProcessTrusted.
- `MacOS/MacMouseController.cs` — posts `kCGEventMouseMoved` at `kCGHIDEventTap`.
- `MacOS/MacPointerReadiness.cs` — Accessibility check + user guidance text.
- `UnsupportedPlatformPointer.cs` — non-macOS fallback: readiness reports "Unsupported platform"; pointer members throw.
- `DependencyInjection.cs` — `AddInfrastructure()`: `OperatingSystem.IsMacOS()` switch.

Console (`src/MouseJiggler.Console/`)
- `Program.cs` — thin: builder (content root = binary dir), `AddMouseJiggler()`, `RunMouseJigglerAsync(args)`.
- `HostingExtensions.cs` — DI/config/logging wiring; runs app with `ApplicationStopping` token (Ctrl+C).
- `JigglerApp.cs` — parse args → help/error → readiness → stop-time-passed check (`TimeProvider`) → banner → session → summary; returns exit code.
- `CommandLine/CommandLineParser.cs` — table-driven parser (`ValueOptions`: name → expected-value text + `TryApply`) returning `CommandLineParseResult` (no exceptions).
- `TimeOfDayFormat.cs` — the `H:mm` format shared by `--stop-at`, `JigglerOptions` and the validator.
- `Configuration/JigglerOptions.cs` — options + defaults constants + `ToSchedule(overrides)`; `JigglerOptionsValidator.cs`.
- `IConsole.cs` / `SystemConsole.cs` — output port; `Usage.cs` — help text; `appsettings.json` — defaults + log levels.

## Conventions

- Strict TDD: write one failing test (confirm it fails for the right reason), minimal code, refactor, `dotnet build && dotnet test` green.
- Test names `Method_State_Expected`; Arrange/Act/Assert; Shouldly assertions; `[Theory]` for variants.
- Where things go: rules → Domain + `tests/MouseJiggler.Domain.UnitTests`; orchestration/ports → Application + `tests/MouseJiggler.Application.UnitTests` (hand-written fakes in `TestDoubles/`: `RecordingMouse`, `ObservableTimeProvider`); OS code → Infrastructure + `tests/MouseJiggler.Infrastructure.IntegrationTests` (`[MacOSFact]`, `[AccessibilityFact]` in `PlatformFacts.cs`; `SystemIdleTime` reads the HID idle time); CLI/options/wiring → Console + `tests/MouseJiggler.Console.Tests` (`RecordingConsole`, NSubstitute `IKeepAwakeSession`).
- Mock only ports (`IMouseController`, `IPointerReadiness`, `IKeepAwakeSession`, `IConsole`), never Domain types.
- Expected failures use result types (`CommandLineParseResult`, `KeepAwakeOutcome`, `PointerReadiness`); exceptions only for programmer errors.
- Classes `sealed`, file-scoped namespaces, user-visible numbers formatted with `CultureInfo.InvariantCulture`.
- DI: each layer exposes `AddXxx()`; Console composes them in `HostingExtensions.AddMouseJiggler`.

## Gotchas

- **Accessibility permission**: macOS silently drops posted events unless the *terminal/IDE app* running `dotnet` is allowed in System Settings → Privacy & Security → Accessibility (then restart that app). The app checks `AXIsProcessTrusted` first and exits 1 with guidance. Never grant it programmatically.
- **CGEvent vs warp**: `CGWarpMouseCursorPosition` moves the cursor but does not reset the HID idle timer. Use `CGEventCreateMouseEvent` + `CGEventPost(kCGHIDEventTap)`.
- **CGEventPost is asynchronous**: reading the position right after posting can return the old value. The real-mouse test polls (`MacMouseControllerTests.WaitForPointerAt`).
- **Why no visible movement**: measured on macOS (HID idle time via `ioreg` / `CGEventSourceSecondsSinceLastEventType`): power assertions (`caffeinate -d -i`, and `-u` = `IOPMAssertionDeclareUserActivity`) do **not** reset idle time, so Teams would still go "Away". A `kCGEventMouseMoved` posted at the *current* position does reset it, and the cursor stays put. Guarded by `MacMouseControllerTests.MoveTo_TheCurrentPosition_ResetsTheSystemIdleTimerWithoutMovingThePointer` (keep hands off the mouse while it runs).
- **FakeTimeProvider + Task.Delay**: `Task.Delay(.., TimeProvider, ..)` resumes asynchronously, so a test can advance the clock before the loop schedules its next delay. Always `await _time.WaitForTimersCreatedAsync(n)` (`ObservableTimeProvider`) before each `Advance`. Timer #1 is the time limit when `Duration` or `StopAt` is set. `ObservableTimeProvider(start)` / `FakeTimeProvider` use UTC as the local zone, so pin "now" in tests that involve `StopAt`; tests on the real clock (e.g. `CompositionRootTests`) must set `Jiggler:StopAt` to "" or they'd depend on the time of day.
- **Ctrl+C** works because `RunMouseJigglerAsync` calls `host.StartAsync()` (ConsoleLifetime hooks SIGINT/SIGTERM) and passes `ApplicationStopping` to the session. To test SIGINT from a script, restore the default handler: non-interactive shells start background jobs with SIGINT ignored.
- `appsettings.json` is copied to `bin/`; the host loads it from `AppContext.BaseDirectory`, not the working directory.
- The `LibraryImport` source generator requires `AllowUnsafeBlocks` (set in the Infrastructure csproj). Mac types are `[SupportedOSPlatform("macos")]`, so callers need an `OperatingSystem.IsMacOS()` guard or the same attribute (CA1416 is an error here).

## How to make common changes

**Change the interval or stop-time default**: edit `src/MouseJiggler.Console/appsettings.json` and the `Default*` constants in `Configuration/JigglerOptions.cs` (help text uses them). Tests: update `JigglerOptionsTests.Defaults_*`, `CompositionRootTests.AddMouseJiggler_ReadsTheShippedAppSettings`, and the banner strings in `JigglerAppTests`.

**Add a CLI option** (e.g. `--quiet`): (1) test in `CommandLineParserTests`; (2) add a property to `CommandLine/CommandLineArguments.cs` and an entry in the `ValueOptions` table (or a flag set like `HelpFlags`) in `CommandLineParser.cs`; (3) if it maps to config, add it to `JigglerOptions` + `ToSchedule` + `JigglerOptionsValidator` (tests in `JigglerOptionsTests`); (4) document it in `Usage.cs` (`JigglerAppTests.Usage_DocumentsEveryOptionAndItsDefault`), this file and `README.md`.

**Bring back visible movement** (only if some app turns out to need real displacement): put the geometry in Domain (a planner returning waypoints from the origin), with display metrics as a new Application port. Add a `Pattern` to `JiggleSchedule` and let `JiggleMouse.Execute` pick the strategy. `JiggleMouseTests` must still assert that `RecordingMouse.Moves` ends at the origin.

**Add an OS adapter** (e.g. Windows): create `src/MouseJiggler.Infrastructure/Windows/` with `WindowsMouseController` (`SendInput` with `MOUSEEVENTF_MOVE`, a real input event that resets idle; not `SetCursorPos`; check that an in-place move resets `GetLastInputInfo`), `WindowsPointerReadiness` (always `Ready`), and P/Invoke in `Windows/NativeMethods.cs`, all `[SupportedOSPlatform("windows")]`. Add an `OperatingSystem.IsWindows()` branch in `Infrastructure/DependencyInjection.cs`, and remove "macOS only" from `UnsupportedPlatformPointer`'s message. Tests: a `[WindowsFact]` in `PlatformFacts.cs` plus integration tests mirroring `MacMouseControllerTests`, and a DI test like `DependencyInjectionTests`. Domain/Application/Console need no changes.
