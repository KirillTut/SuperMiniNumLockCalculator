# NumLock Calculator

NumLock Calculator is a small Windows tray calculator. Press `NumLock` to show the calculator, type an expression immediately, and press `NumLock` again or the close button to hide it back to the tray.

## Features

- Show/hide the calculator with the `NumLock` key.
- Optional `Keep NumLock always on` mode so the numeric keypad remains usable.
- Single-line expression input with result history.
- Continue calculations from the latest result or from a selected history result.
- Tray menu with settings and exit actions.
- Optional always-on-top, opacity, angle mode, number format, language, and start-with-Windows settings.
- Portable settings stored in `nlcalc.ini` next to the executable.

## Download

Download the latest `NumLockCalculator-*-win-x64.zip` package from GitHub Releases.

To run:

1. Extract the zip file.
2. Run `NLCalc2.exe`.
3. Use the tray icon to open settings if needed.

Windows may show a SmartScreen warning for unsigned community builds.

## Build From Source

Requirements:

- Windows
- .NET SDK 10.0 or newer

Build:

```powershell
dotnet build NLCalc2Source\NLCalc2.csproj
```

Publish a self-contained Windows x64 package:

```powershell
dotnet publish NLCalc2Source\NLCalc2.csproj -c Release -r win-x64 --self-contained true -o NLCalc2Runtime
```

## Notes

This repository does not include the original legacy NumLock Calculator binary. The legacy executable is used only as local behavioral reference.

## License

MIT License. See [LICENSE](LICENSE).

