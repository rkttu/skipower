# Skipower

[English](README.md) | [한국어](README.ko.md)

[![Build portable binaries](https://github.com/rkttu/skipower/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/rkttu/skipower/actions/workflows/build.yml)

Skipower provides a command-line tool and a [MewUI](https://github.com/aprillz/MewUI) desktop app for ordinary Windows shutdown and restart requests. Both target Windows 10 and 11, use .NET 10, and publish as self-contained Native AOT executables for x64 and Arm64. The CLI and GUI share the same power-control implementation.

Skipower calls [`InitiateShutdownW`](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw) without `SHUTDOWN_INSTALL_UPDATES`. This asks Windows to shut down or restart without requesting update installation. Windows may still finish an update that already requires a restart or enforce an organization policy. A successful API return means Windows accepted the request; it does not guarantee that shutdown completed.

## Download and run

The [build workflow](.github/workflows/build.yml) runs on pushes to `main` and can also be started manually. Open its latest successful run and download the `skipower-win-x64` or `skipower-win-arm64` artifact. Each artifact contains a portable ZIP with:

- `skipower.exe`: command-line app
- `Skipower.Gui.exe`: MewUI desktop app
- MewUI license and third-party notices

The two executables have distinct names because Windows normally treats file names case-insensitively. The workflow uses GitHub's [Windows x64 and Arm64 hosted runners](https://docs.github.com/en/actions/reference/runners/github-hosted-runners).

## Display language

Both apps use the current Windows UI culture. A Korean UI culture, including `ko-KR`, selects Korean. Every other UI culture selects English. This follows the Windows user interface language reported through [.NET `CurrentUICulture`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentuiculture). An elevated GUI retry retains the language selected before the UAC prompt. CLI JSON property names and status values remain in English for scripts.

## Command-line use

These commands request ordinary power operations:

```powershell
./skipower.exe shutdown
./skipower.exe restart --json
./skipower.exe shutdown --force
./skipower.exe shutdown --hybrid
```

`--force` applies only to the current user's session; Skipower never requests `SHUTDOWN_FORCE_OTHERS`. `--hybrid` is available only for shutdown. `--verbose` writes diagnostics to stderr, while `--json` writes a stable machine-readable result. `--help` and `--version` are also available. See [Microsoft's shutdown flag reference](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw) for the Windows semantics.

| Exit code | Meaning |
| --- | --- |
| 0 | Windows accepted the request |
| 2 | Invalid command line |
| 10 | Shutdown privilege unavailable or UAC retry canceled |
| 11 | Another user is signed in |
| 12 | Shutdown already in progress |
| 13 | Shutdown already scheduled |
| 14 | Windows is not ready |
| 30 | Other failure |

## GUI and administrator retry

`Skipower.Gui.exe` presents Shut down and Restart buttons, an explanation of the update limitation, and the result of the request. The GUI does not offer forced shutdown. It uses MewUI's Win32 platform and GDI renderer.

Both executables start with an `asInvoker` manifest. They try the requested operation with the current token. If Windows denies shutdown privilege, the GUI shows **Retry as administrator**. The CLI offers the same conditional retry when `--elevate` is supplied:

```powershell
./skipower.exe restart --elevate
```

The retry uses the Windows `runas` verb and asks for UAC approval only after access is denied. Canceling UAC leaves the power request unaccepted. An administrator token can still lack the required privilege under system policy. See [Microsoft's administrator privilege guidance](https://learn.microsoft.com/en-us/windows/win32/secbp/running-with-administrator-privileges).

## Build from source

Install the .NET SDK version selected by [`global.json`](global.json) and the appropriate Windows C++ build tools for Native AOT. The script publishes both apps and creates a portable ZIP:

```powershell
dotnet test Skipower.slnx -c Release
./scripts/publish.ps1 -Rid win-x64
# Run on Windows Arm64, or with the Arm64 C++ toolchain:
./scripts/publish.ps1 -Rid win-arm64
```

The output is `artifacts/skipower-win-x64.zip` or `artifacts/skipower-win-arm64.zip`. The project files enable Native AOT when a runtime identifier is supplied. Microsoft's [Native AOT documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) describes its C++ toolchain requirements.

## Validation and release scope

Unit tests use fake Win32, privilege, and elevation components, so they never shut down the test machine. They cover shutdown flags, status and exit-code mapping, UAC cancellation, and Korean/English selection. The [Windows tests](tests/Skipower.Windows.Tests/WindowsPowerControllerTests.cs) and [CLI tests](tests/Skipower.Core.Tests/CliRunnerTests.cs) show the cases.

The first [x64 and Arm64 CI run](https://github.com/rkttu/skipower/actions/runs/36718297912) passed and uploaded both ZIP artifacts on September 30, 2026. A local Windows x64 run passed 34 tests on October 1, 2026. An earlier run built both Native AOT executables and started the GUI. Actual UAC approval, Windows Update state transitions, and real shutdown or restart still require isolated Windows VM testing. The ZIPs are not Authenticode-signed.

The [design plan](docs/plan.md) records the API boundary, deferred update diagnostics, and VM test scenarios. Distribution includes the [MewUI MIT license](licenses/MewUI-LICENSE.txt) and [third-party notices](licenses/THIRD_PARTY_NOTICES.md).
