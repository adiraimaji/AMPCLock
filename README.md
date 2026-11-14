# AM PC Lock

A lightweight, open-source pattern-lock application for Windows. It runs in the
system tray and shows a full-screen pattern lock overlay that can block keyboard
input, task switching, the taskbar, and Task Manager while the PC is locked.

AM PC Lock is free software released under the [MIT License](LICENSE). You can
use it, modify it, redistribute it, and even sell it.

## Features

- **Pattern lock** — draw a pattern on a 3×3, 4×4, or 5×5 grid.
- **Keyboard entry** — type the letters shown on the dots instead of drawing.
- **Recovery options** — reset access with a recovery code or a security question.
- **Input blocking** — optionally block the keyboard, Alt+Tab / Win keys, the
  taskbar, Task Manager, logoff, and shutdown while locked.
- **Automatic locking** — lock after a configurable idle period or at startup.
- **Global hotkey** — lock instantly (default `Ctrl+Alt+L`).
- **Start with Windows** — optionally run at sign-in.
- **Monitor control** — turn the display off after a configurable timeout.
- **Customizable lock screen** — background color or image, transparency, and
  alert text/sound after repeated failed attempts.
- **Safe recovery** — desktop changes are journaled and restored on unlock or on
  the next launch if the app was interrupted.

## Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building)

## Build

```powershell
dotnet build AMPCLock.csproj -c Release
```

Output: `bin\Release\net8.0-windows\AMPCLock.exe`

## Run

```powershell
dotnet run --project AMPCLock.csproj
```

The app starts in the system tray. Open **Settings** from the tray menu or the
global hotkey to create an unlock pattern.

## Publish a portable single-file executable

```powershell
dotnet publish AMPCLock.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None -p:DebugSymbols=false
```

The self-contained executable is written to
`bin\Release\net8.0-windows\win-x64\publish\AMPCLock.exe`.

## First run and settings access

Opening **Settings** from the tray menu, the hotkey, or `--settings` asks for the
current unlock pattern first. If no pattern is configured yet (first run),
Settings opens directly so a pattern can be created.

The identity prompt supports mouse drawing and keyboard entry. Each dot shows a
random letter; type the letters in pattern order and press **Enter** to confirm.
**Backspace** removes the last dot and **Delete** clears the typed pattern.

Inside **Settings → Unlock pattern**, the **Reset password data…** button removes
the stored unlock pattern, recovery code, and security question/answer, then
saves the config immediately. Lock-on-startup is switched off and autolock stays
paused until a new pattern exists, so the PC cannot lock itself without a way to
unlock. The app returns to first-run setup and will not ask for the old pattern
again.

## Command-line options

| Option | Purpose |
| --- | --- |
| `--selftest` | Runs the built-in self-test and exits (0 = pass). Does not lock the PC. |
| `--locktest` | Locks for 5 seconds, then unlocks. Override with `AMPCLOCK_LOCKTEST_SECONDS` (2–60). |
| `--preview` | Opens the settings preview window. |
| `--previewlock` | Shows the lock overlay for 8 seconds, then closes it. |
| `--verifytest` | Opens the identity verification window. |
| `--recoverytest` | Opens the recovery window. |
| `--settings` | Opens settings on startup. |
| `--unlock` | Restores desktop state from the journal and exits. |
| `/lock`, `--lock` | Starts locked. |
| `/startuplock` | Starts locked (used by the Windows startup entry). |

## Data and logs

Settings, lock state, and logs are stored in:

```
%APPDATA%\AM PC Lock\
```

- `config.json` — settings, pattern/recovery hashes and salts
- `lockstate.json` — desktop state journal used to restore Explorer/taskbar/policies
- `app.log` — diagnostic log

All credentials are stored as salted PBKDF2 (SHA-256) hashes, never as plain text.

## Project layout

| File | Responsibility |
| --- | --- |
| `Program.cs` | Entry point, single-instance mutex, command-line modes |
| `TrayApp.cs` | Tray icon, menu, hotkey, auto-lock timer |
| `LockController.cs` | Orchestrates lock/unlock and guard lifetimes |
| `LockScreen.cs` | Full-screen lock overlay |
| `PatternPad.cs` | Pattern drawing control |
| `PatternHasher.cs` | PBKDF2 hashing and `AppConfig` verification helpers |
| `AppConfig.cs` | Persisted settings model |
| `ConfigStore.cs` | JSON config/log storage |
| `SettingsForm.cs` | Settings UI |
| `PatternEnrollForm.cs` / `PreviewForm.cs` | Pattern enrollment and preview |
| `VerifyIdentityForm.cs` / `RecoveryForm.cs` / `RecoveryCodeForm.cs` | Unlock and recovery UI |
| `InputGuard.cs` | Keyboard/task-switching blocking |
| `SystemGuard.cs` | Taskbar/policy changes and desktop-state journal |
| `MediaGuard.cs` | Monitor power / media handling |
| `HotkeyWindow.cs` | Global hotkey registration |
| `Native.cs` | Win32 P/Invoke declarations |
| `SelfTest.cs` | Non-destructive self-test |
| `AppIcon.cs` | Runtime icon generation |

## How the lock works

While locked, AM PC Lock shows a topmost overlay and, depending on your settings,
hides the taskbar, disables Task Manager, blocks the Windows keys and Alt+Tab,
and prevents logoff and shutdown. These changes are recorded in
`lockstate.json` and are restored when you unlock. If the app is interrupted or
crashes, the journal is restored automatically on the next launch, or manually
with `AMPCLock.exe --unlock`.

### Important security note

AM PC Lock is a **deterrent**, not a security boundary. It is designed to stop
casual access to your computer, not to protect data from a determined attacker.
The source is open, and no desktop-level lock can stop someone with physical
access, an administrator account, another OS boot, or the ability to kill the
process. `AMPCLock.exe --unlock` and the journal restore path are intentionally
unauthenticated. Do not rely on it to protect sensitive information, and use
full-disk encryption and your Windows account password for real security.

## Safety

The lock is intentionally intrusive. Test changes with `--preview`,
`--previewlock`, or a short `--locktest` before using `--lock`. If something goes
wrong, run `AMPCLock.exe --unlock` to restore the desktop state, or delete
`%APPDATA%\AM PC Lock\lockstate.json` and re-run the app.

This software is provided "as is", without warranty of any kind. See the
[LICENSE](LICENSE) for details.

## Contributing

Issues and pull requests are welcome. Please build the project and run
`AMPCLock.exe --selftest` before submitting a change. For anything security
related, open an issue instead of publishing exploit details.

## License

Released under the [MIT License](LICENSE). Copyright (c) 2026 AM.

You are free to use, copy, modify, merge, publish, distribute, sublicense, and
sell copies of this software, provided the license notice is included.
