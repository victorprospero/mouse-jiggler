# MouseJiggler

Keeps your Mac from going idle. Every 10 seconds it posts a mouse event at the pointer's current position, so Microsoft Teams doesn't switch you to "Away" and the screen doesn't lock. It stops by itself at 18:00 (configurable).

- The cursor never visibly moves, so it doesn't get in the way of your work.
- It uses a real input event, which resets the system idle timer. Teams decides whether you're "Away" from that timer. Power assertions like `caffeinate` only keep the screen awake; they don't reset the timer.

## Prerequisites

- macOS. Other operating systems get a clear "Unsupported platform" message.
- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` should print 10.x).
- **Accessibility permission** for the app you run it from (Terminal, iTerm2, Visual Studio Code, ...). Without it macOS silently ignores the mouse events. If permission is missing, MouseJiggler tells you so and exits. To grant it:
  1. Open **System Settings → Privacy & Security → Accessibility**.
  2. Turn on your terminal app (use **+** to add it if it isn't listed).
  3. Quit and reopen the terminal app, then run MouseJiggler again.

## Run

From the `MouseJiggler` folder:

```bash
make run-dev                          # or: make run
make run ARGS="--interval 30"         # pass options
make demo                             # quick check: every 2 s for 7 s
make                                  # list all shortcuts (build, test, clean…)
```

Without make:

```bash
dotnet run --project src/MouseJiggler.Console
```

Sample output:

```text
Keeping the system awake: posting an in-place mouse event every 10 s until 18:00. Press Ctrl+C to stop.
10:59:33 info: MouseJiggler.Application.KeepAwakeSession[1] Jiggle #1: input event posted in place at (512, 723)
...
```

### Options

| Option | Default | Description |
|---|---|---|
| `--interval <seconds>` | 10 | Time between jiggles. The first one happens immediately. |
| `--duration <seconds>` | none | Stop automatically after this long. |
| `--stop-at <HH:mm>` | 18:00 | Stop automatically at this local time (24-hour clock). With `--duration`, whichever comes first. If it's already past that time, MouseJiggler says so and exits. |
| `-h`, `--help` | | Show help. |

Example of a short test run (jiggles every 2 s and stops after 7 s):

```bash
dotnet run --project src/MouseJiggler.Console -- --interval 2 --duration 7
```

## Configuration

Defaults live in `src/MouseJiggler.Console/appsettings.json`:

```json
{ "Jiggler": { "IntervalSeconds": 10, "StopAt": "18:00" } }
```

Set `StopAt` to `""` to run until Ctrl+C. You can also set `DurationSeconds` there. Precedence, highest first: command-line options, then environment variables (for example `Jiggler__IntervalSeconds=30`), then `appsettings.json`. Values must be positive numbers, and decimals use a dot (`0.5`).

## Stop

Press **Ctrl+C**. MouseJiggler finishes cleanly and prints how many jiggles it made. It also stops on its own at the stop time (18:00 by default) or after `--duration`.

Exit codes: `0` success, `1` mouse can't be controlled (missing Accessibility permission or unsupported OS), `2` invalid options.

## Development

```bash
dotnet build
dotnet test
```

See [CLAUDE.md](CLAUDE.md) for the architecture, file index and how to make common changes.
