# Skipower technical plan

[English](plan.md) | [Korean](plan.ko.md)

Document baseline: September 30, 2026. Target systems: Windows 10 and 11. Implementation: .NET 10 and C# 14. This document describes the power API contract, current implementation, and work that remains before a signed public release.

## Purpose and limits

Skipower requests an ordinary local shutdown or restart without asking Windows to install updates during that operation. It does not disable Windows Update, alter update policy, delete update files, or roll back installed updates. A restart can still complete an update that Windows has already installed, and an organization policy can still enforce a deadline.

The power operation must not depend on a successful update-state query. This keeps shutdown available even when Windows Update Agent cannot be queried. The [`InitiateShutdownW` reference](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw) defines the flags used for the request; it does not offer a flag that guarantees all update servicing will be skipped.

| Windows state | Skipower request | Remaining uncertainty |
| --- | --- | --- |
| Update available or downloaded, installation not committed | Ordinary shutdown or restart without `SHUTDOWN_INSTALL_UPDATES` | Windows servicing behavior can vary by build |
| Restart already required | Same ordinary request | Restart may complete the installed update |
| Managed update deadline | Same ordinary request | Policy may enforce installation or restart |

The original scope also excludes service manipulation, `SoftwareDistribution` changes, private `UsoClient` commands, registry overrides, remote shutdown, and scheduled shutdown.

## Power API and privileges

`Skipower.Windows` owns the Win32 calls and privilege scope. `Skipower.Core` contains the request and result models. Both front ends use the same `IPowerController`. The implementation uses source-generated `LibraryImport` declarations for the small fixed set of Win32 APIs.

The flag mapping is:

| Request | Required flag | Optional flags | Excluded flags |
| --- | --- | --- | --- |
| Restart | `SHUTDOWN_RESTART` (`0x00000004`) | `SHUTDOWN_FORCE_SELF` (`0x00000002`) | `SHUTDOWN_INSTALL_UPDATES`, `SHUTDOWN_FORCE_OTHERS`, `SHUTDOWN_HYBRID` |
| Shutdown | `SHUTDOWN_POWEROFF` (`0x00000008`) | `SHUTDOWN_FORCE_SELF`, `SHUTDOWN_HYBRID` (`0x00000200`) | `SHUTDOWN_INSTALL_UPDATES`, `SHUTDOWN_FORCE_OTHERS` |

`SHUTDOWN_FORCE_SELF` applies only to the originating session. It is requested only by the CLI's explicit `--force` option. The GUI does not expose forced shutdown. `SHUTDOWN_HYBRID` is an explicit shutdown-only option; the default is a full shutdown. The program does not override an existing scheduled shutdown. These meanings and error conditions follow the [Microsoft flag and return-code reference](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw).

Before `InitiateShutdownW`, the program enables `SeShutdownPrivilege` in its existing process token, then restores the previous state. `AdjustTokenPrivileges` cannot grant a privilege absent from the token. The executables use `asInvoker` manifests, so ordinary launches do not request UAC. Both results and CLI exit codes describe request acceptance, not completed shutdown.

Skipower records a planned "Other" shutdown reason. It does not claim a Windows Update or maintenance reason. The request targets the local computer and uses a zero-second grace period.

## Architecture and binaries

The solution contains four product projects and two test projects:

- `Skipower.Core`: request, result, diagnostics, and English/Korean UI strings
- `Skipower.Windows`: Win32 shutdown call, privilege management, and optional elevated-process launcher
- `Skipower.Cli`: console app, options, JSON contract, exit codes
- `Skipower.Gui`: MewUI Win32/GDI desktop app
- `Skipower.Core.Tests` and `Skipower.Windows.Tests`: fake-based tests that never request real shutdown

The binaries are `skipower.exe` and `Skipower.Gui.exe`. Case-only names would collide on a typical Windows file system, so the GUI uses a distinct suffix. The GUI uses [MewUI Core, Win32 platform, and GDI backend](https://github.com/aprillz/MewUI); its package references are pinned in the project file. Both application projects enable self-contained Native AOT when published with a runtime identifier. The current build retains globalization data for Korean text. Microsoft's [Native AOT guide](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) describes the platform toolchain prerequisites.

## CLI, GUI, and localization

The CLI supports `shutdown`, `restart`, `--force`, shutdown-only `--hybrid`, `--json`, `--verbose`, `--elevate`, `--help`, and `--version`. `--verbose` writes diagnostics to stderr. JSON keys and status values stay in English for automation. Exit codes are 0 for accepted, 2 for invalid arguments, 10 for privilege denial or canceled elevation, 11 for another signed-in user, 12 for an operation in progress, 13 for a scheduled operation, 14 for system-not-ready, and 30 for another failure.

The GUI presents shutdown, restart, the update limitation, and the result. It does not offer `--force`. Both apps select Korean if the current UI culture's two-letter language is `ko`; they select English for all other cultures. On Windows, [.NET initializes `CurrentUICulture` from the user's default Windows UI language](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentuiculture). A GUI administrator retry carries the already-selected language into the new process.

On `AccessDenied`, the CLI can retry once when the user included `--elevate`. The GUI reveals a retry button only after that status. The launcher invokes the same executable with the Windows `runas` verb; UAC cancellation produces a visible failure result. Neither front end enters an elevation loop. Administrator approval may still fail under a restrictive security policy. [Microsoft's privilege guidance](https://learn.microsoft.com/en-us/windows/win32/secbp/running-with-administrator-privileges) describes the explicit elevation mechanism.

## Error and log policy

`Skipower.Windows` maps the documented Win32 responses to `Accepted`, `AccessDenied`, `OtherUsersLoggedOn`, `ShutdownAlreadyInProgress`, `ShutdownAlreadyScheduled`, `SystemNotReady`, or `Failed`. It never applies `SHUTDOWN_GRACE_OVERRIDE` to silently replace an existing schedule. A future version could offer a bounded retry for `ERROR_NOT_READY`; the current version reports the status.

Normal operation writes no file log and sends no telemetry. The CLI emits optional diagnostics only when `--verbose` is selected. It does not collect update titles, user names, or computer names.

## Build and validation

The [build workflow](../.github/workflows/build.yml) runs tests and `scripts/publish.ps1` on native x64 and Arm64 Windows runners. It uploads separate `skipower-win-x64` and `skipower-win-arm64` artifacts, each containing a portable ZIP with both executables and the required notices. The [README](../README.md) shows local commands and the workflow badge.

Unit tests use fake native API calls, privilege scopes, and elevated launchers. They check flags, return-code mapping, canceled UAC, CLI output, and language fallback without shutting down the runner. The first [CI run](https://github.com/rkttu/skipower/actions/runs/36718297912) passed both architectures on September 30, 2026.

Before a signed public release, isolated Windows VMs should verify actual power behavior across the following conditions:

1. Ordinary shutdown and restart with no pending update.
2. Update discovered but not downloaded, and update fully downloaded.
3. Update installed with `RebootRequired` set.
4. Managed update deadline or grace period.
5. Another signed-in user and an existing shutdown schedule.
6. An application with unsaved work, with and without the CLI's `--force` option.
7. Full and hybrid shutdown with Fast Startup enabled and disabled.
8. UAC approval, cancellation, and denial under a restrictive policy.
9. Windows 10 and Windows 11 x64 and Arm64 builds.

These tests must record whether Windows accepted the request and what actually happened after restart. The portable ZIPs currently have no Authenticode signature. Artifact creation in CI is verified; power behavior under these VM conditions is not.

## Deferred update diagnostics

A later, optional diagnostics component could query Windows Update Agent's [`ISystemInformation::RebootRequired`](https://learn.microsoft.com/en-us/windows/win32/api/wuapi/nf-wuapi-isysteminformation-get_rebootrequired) and search for available or downloaded updates. It could report `NoPendingUpdate`, `Available`, `Downloaded`, `RebootRequired`, or `Unknown`. This query must never block the power request.

The current Native AOT binaries omit that component. WUA uses COM, and [.NET's built-in COM interop is unavailable under Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/warnings/il3052). A separate CoreCLR probe or source-generated COM wrapper would need its own design and tests. Tray integration, shell integration, scheduled shutdown, remote shutdown, and organization-policy diagnostics also remain outside the current release.
