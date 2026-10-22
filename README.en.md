# CP2077-Performance - English

**[Read this in Portuguese](README.md#pt-br)**

Performance profile for **Cyberpunk 2077** on low-end hardware. The goal:
**playable 30 FPS** on integrated graphics, without removing any mod.

**Version: `0.3.0-alpha`** - see the [CHANGELOG](CHANGELOG.md). Validated on
**AMD Ryzen 5 5500U + AMD Radeon Vega 8 integrated + 20 GB RAM**. The project
**removes no mod** and **keeps the hair** (`Hair = True`).

---
## 1. What this is

Cyberpunk 2077 has **three configuration layers**, read by the game at startup:
**menu options** (`UserSettings.json`), **engine cvars** (`user.ini`) and
**engine patches** (`config.json`). The full table is in the Portuguese
section above.

The problem: **the settings menu hides the options that matter.** The RED
engine features, which drive actual GPU and CPU cost, do not appear in the UI -
the engine reads them from an INI file. And the game **silently reverts
invalid values**: every list-typed option has an exact set of accepted values,
and a value outside it simply has no effect.

This project solves both with a single JSON profile, applied by a desktop
application that **validates every value** against the game's schema and the
executable before writing. It cuts fragment cost, removes 11 async compute
passes, cuts CPU cost and texture bandwidth, caps the game at 30 FPS and cuts
background I/O. What it **does not** do: touch `Hair`, `DistantShadows`,
`GlobalIllumination` or Ray Tracing; remove any mod; or **touch the game UI (HUD)**.

### Scope boundary and display mode

The application focuses exclusively on **low-spec performance setup** and on
**forcing fullscreen (`WindowMode = Fullscreen`, `index = 2`) while preserving
whatever `Resolution` the user configured in the game** (e.g. `1366x768`,
`1280x720`, `1600x900`, `1920x1080`), without enumerating monitors or overriding
resolution - working across any monitor setup while preserving internal Dynamic
Resolution Scaling.

All HUD manipulation and in-game HUD toggling have been moved to a **separate
project** (seeded in `handoff/hud-toggle/`). The variant mechanism remains
ready for future performance variants, without any HUD options in the curated
profile.

## 2. Usage

**1.** Open `CP2077-Performance.exe`.

**2.** Enter the **game folder** and click **Validate**.

> The save always lives in `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077`,
> whichever store you use - no disk scan, no per-store logic, one build for
> Steam, Epic and GOG.

> The game must be **closed** to apply: it loads the menu options at startup
> and flushes its in-memory state to the file on exit, which would undo the
> application.

**3.** The **Profile Status** card shows what will happen in plain language, how
many settings will change, and the live state read from disk (whether the
profile is applied, since when, and how many settings diverge).

**4.** Click **Apply Profile**. It takes an automatic backup, writes the three
layers (including `WindowMode = Fullscreen` while preserving your `Resolution`),
**reads the files back** and compares against the expected result, and records
every change with **the origin of every value**.

**5.** Start the game. The complete technical reference tables for all menu
options, engine cvars, and CET patches live in
[`docs/ARQUITETURA.md` §10](docs/ARQUITETURA.md#10-referência-técnica-completa-das-três-camadas).

## 3. Undoing

Every application that changes something creates a backup under
`%LOCALAPPDATA%\CP2077-Performance\backups\`. **The three most recent are
kept**: creating a fourth discards the oldest; the fifth discards the second.

Click **Restore Backup** on the main screen to roll back to the most recent
version.

## 4. Building, documentation and license

`build.cmd` publishes **self-contained** to `dist\CP2077-Performance.exe` (a
single file, no runtime to install); `build.cmd framework-dependent` publishes
the smaller build, which requires the .NET 10 Desktop Runtime. Development
requires the **.NET 10 SDK**, and the verification runner is
`dotnet run --project app\CP2077Performance.Tests`.

See the [Portuguese section](README.md#5-documenta%C3%A7%C3%A3o) for the documentation links.
**MIT** - see [`LICENSE`](LICENSE). This project targets **offline /
single-player** use.
