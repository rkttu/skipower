\# Skipower 기술 설계서



문서 기준일: 2026년 9월 30일  

대상 플랫폼: Windows 11, Windows 10  

구현 기준: .NET 10, C# 14  

프로젝트명: Skipower, 가칭



\## 제품 목적



Skipower는 Windows에서 시스템을 종료하거나 다시 시작할 때 Windows Update 설치를 명시적으로 요청하지 않는 전원 작업을 제공합니다.



주요 사용 사례는 다음과 같습니다.



\- 퇴근 직전 업데이트 설치 때문에 종료가 지연되는 상황

\- 회의나 시연 직전 빠른 재시작이 필요한 상황

\- 스크립트에서 업데이트 설치와 일반 재시작을 구분하려는 상황

\- Windows 기본 전원 UI가 업데이트 동작을 명확히 구분하지 않는 환경

\- 업데이트 UX가 서로 다른 Windows 버전을 동일한 명령으로 제어하려는 상황



Windows Update 자체를 비활성화하거나 업데이트 정책을 변경하는 기능은 제품 범위에 포함하지 않습니다.



\## 조사 결과



Microsoft는 2026년 4월 Windows Update UX 개편을 발표하면서 일반 `Restart`, `Shut down`과 `Update and restart`, `Update and shut down`을 구분하는 방향을 공개했습니다. 일반 전원 작업을 선택하면 대기 중인 업데이트를 설치하지 않는 것이 새로운 UX의 목표입니다.



2026년 8월 Microsoft가 공개한 지원 문서에는 2026년 7월 28일 이후 업데이트부터 Windows 11 24H2, 25H2, 26H1에서 업데이트 재시작을 월간 보안 업데이트와 통합하는 새 동작이 설명되어 있습니다. 다만 Microsoft는 이 기능을 점진적으로 배포하며 모든 장치에서 즉시 사용할 수 있는 것은 아니라고 명시합니다. 오래 대기한 업데이트는 Windows가 결국 설치하고 다시 시작할 수도 있습니다.



따라서 Skipower는 Windows의 업데이트 정책을 무력화하는 도구가 아니라 다음 요청을 명시적으로 표현하는 도구로 정의합니다.



> 이번 전원 작업에서는 Windows Update 설치를 요청하지 않습니다.



이 정의는 구현과 사용자 인터페이스 모두에 적용합니다.



\## Win32 전원 API



핵심 API로 `InitiateShutdownW`를 사용합니다.



Microsoft 문서는 이 API에 다음 플래그를 정의합니다.



```c

SHUTDOWN\_RESTART          = 0x00000004

SHUTDOWN\_POWEROFF         = 0x00000008

SHUTDOWN\_FORCE\_SELF       = 0x00000002

SHUTDOWN\_INSTALL\_UPDATES  = 0x00000040

SHUTDOWN\_HYBRID           = 0x00000200

```



`SHUTDOWN\_INSTALL\_UPDATES`를 지정하면 Windows가 시스템을 종료하기 전에 업데이트를 설치합니다. 따라서 Skipower는 이 플래그를 지정하지 않고 `SHUTDOWN\_RESTART` 또는 `SHUTDOWN\_POWEROFF`만 전달합니다.



공개 API에는 `SHUTDOWN\_SKIP\_UPDATES`와 같은 반대 플래그가 존재하지 않습니다. 따라서 Skipower가 제공할 수 있는 보장은 다음 범위까지입니다.



```text

Skipower

&#x20;   ↓

InitiateShutdownW

&#x20;   ↓

SHUTDOWN\_RESTART 또는 SHUTDOWN\_POWEROFF

&#x20;   ↓

SHUTDOWN\_INSTALL\_UPDATES 없음

```



이 설계는 Windows에 업데이트 설치를 요청하지 않습니다. 이미 설치가 커밋되어 재부팅만 남았거나 조직 정책의 기한이 만료된 경우까지 업데이트 수행을 차단하지는 않습니다.



\## 업데이트 상태에 따른 동작 범위



Windows Update 상태를 세 가지로 구분합니다.



\### 업데이트 설치 전 상태



업데이트를 검색했거나 다운로드했지만 아직 설치하지 않은 상태입니다.



Skipower가 가장 유효하게 작동할 수 있는 상태입니다. 일반 종료 또는 재시작을 요청하고 `SHUTDOWN\_INSTALL\_UPDATES`를 지정하지 않습니다.



\### 재부팅 대기 상태



Windows Update Agent가 `RebootRequired`를 보고하는 상태입니다.



이 시점에는 업데이트 설치 작업의 일부가 이미 끝났으며 시스템 재시작을 통해 설치를 완료해야 합니다. WUA는 이를 공식적으로 별도 상태로 노출합니다.



Skipower가 이 상태를 원래 상태로 되돌리지는 않습니다.



따라서 향후 상태 진단 기능에서 다음과 같이 표시합니다.



```text

Restart required by Windows Update.



A restart may complete an update that Windows has already installed.

Skipower cannot cancel an update that has reached this state.

```



\### 관리 정책 강제 상태



Intune, Windows Update for Business, 그룹 정책 등에서 업데이트 기한을 설정할 수 있습니다.



Microsoft 문서에 따르면 유효 기한과 유예 기간이 끝나면 Windows는 활성 시간 여부와 관계없이 재시작할 수 있으며 사용자가 다시 일정을 변경하지 못할 수도 있습니다.



Skipower는 이러한 정책을 변경하거나 우회하지 않습니다.



\## 비목표



Skipower는 다음 기능을 구현하지 않습니다.



\- `wuauserv` 서비스 중지

\- Update Orchestrator 서비스 조작

\- `SoftwareDistribution` 디렉터리 변경

\- 업데이트 파일 삭제

\- Windows Update 레지스트리 상태 강제 변경

\- 업데이트 기한 정책 변경

\- Intune 또는 그룹 정책 우회

\- 비공개 `UsoClient` 명령 사용

\- 업데이트 설치 롤백

\- 원격 PC 종료

\- 드라이버 또는 서비스 설치



이 범위를 유지하면 Windows 내부 구현에 대한 의존성을 줄일 수 있으며 시스템 업데이트 상태를 손상할 가능성도 낮출 수 있습니다.



\## 전체 아키텍처



1차 버전은 다음 네 프로젝트로 구성합니다.



```text

Skipower.sln



src/

&#x20;├─ Skipower.Core/

&#x20;│   ├─ PowerAction.cs

&#x20;│   ├─ PowerRequest.cs

&#x20;│   ├─ PowerResult.cs

&#x20;│   └─ IPowerController.cs

&#x20;│

&#x20;├─ Skipower.Windows/

&#x20;│   ├─ WindowsPowerController.cs

&#x20;│   ├─ ShutdownPrivilege.cs

&#x20;│   ├─ NativeMethods.cs

&#x20;│   └─ NativeConstants.cs

&#x20;│

&#x20;├─ Skipower.Cli/

&#x20;│   └─ Program.cs

&#x20;│

&#x20;└─ Skipower.Gui/

&#x20;    ├─ Program.cs

&#x20;    └─ PowerDialog.cs



tests/

&#x20;├─ Skipower.Core.Tests/

&#x20;└─ Skipower.Windows.Tests/

```



`Skipower.Core`에는 운영 체제 API를 넣지 않습니다.



`Skipower.Windows`가 Win32 API 호출과 권한 처리를 전담합니다.



CLI와 GUI는 동일한 `IPowerController` 구현을 사용합니다.



\## 핵심 도메인 모델



전원 요청은 작은 값 객체로 표현합니다.



```csharp

public enum PowerAction

{

&#x20;   Shutdown,

&#x20;   Restart

}



public sealed record PowerRequest(

&#x20;   PowerAction Action,

&#x20;   bool Force = false,

&#x20;   bool Hybrid = false);

```



결과는 실제 종료 완료가 아닌 Windows의 요청 접수 결과를 나타냅니다.



```csharp

public sealed record PowerResult(

&#x20;   PowerRequest Request,

&#x20;   PowerRequestStatus Status,

&#x20;   int? NativeErrorCode = null);



public enum PowerRequestStatus

{

&#x20;   Accepted,

&#x20;   AccessDenied,

&#x20;   OtherUsersLoggedOn,

&#x20;   ShutdownAlreadyInProgress,

&#x20;   ShutdownAlreadyScheduled,

&#x20;   SystemNotReady,

&#x20;   Failed

}

```



`InitiateShutdownW`는 비동기 API입니다. 성공 반환값도 시스템 종료 완료를 의미하지 않고 Windows가 요청을 받아들였다는 의미입니다. 애플리케이션이나 시스템이 이후 종료를 중단할 수도 있습니다.



따라서 `Success` 또는 `Completed`보다 `Accepted`가 API 의미에 맞습니다.



\## Win32 호출



.NET 10에서는 `LibraryImport` 또는 CsWin32를 사용할 수 있습니다.



Microsoft는 현재 C#에서 Win32 API를 사용할 때 CsWin32를 기본 선택지로 안내하고 있으며 `LibraryImport` 역시 소수의 고정된 API를 직접 선언할 때 적합한 방식으로 설명합니다. 두 방식 모두 AOT 친화적으로 구성할 수 있습니다.



Skipower에서 필요한 Win32 API 수가 적으므로 1차 구현은 `LibraryImport`를 사용합니다.



핵심 선언은 다음 형태가 됩니다.



```csharp

internal static partial class NativeMethods

{

&#x20;   \[LibraryImport(

&#x20;       "advapi32.dll",

&#x20;       EntryPoint = "InitiateShutdownW",

&#x20;       StringMarshalling = StringMarshalling.Utf16)]

&#x20;   internal static partial uint InitiateShutdown(

&#x20;       string? machineName,

&#x20;       string? message,

&#x20;       uint gracePeriod,

&#x20;       uint shutdownFlags,

&#x20;       uint reason);

}

```



전원 플래그는 별도로 관리합니다.



```csharp

internal static class ShutdownFlags

{

&#x20;   public const uint ForceSelf      = 0x00000002;

&#x20;   public const uint Restart        = 0x00000004;

&#x20;   public const uint PowerOff       = 0x00000008;

&#x20;   public const uint InstallUpdates = 0x00000040;

&#x20;   public const uint Hybrid         = 0x00000200;

}

```



중요한 부분은 `InstallUpdates` 상수를 정의하더라도 Skipower의 일반 실행 경로에서는 사용하지 않는다는 점입니다.



실제 플래그 구성은 다음과 같습니다.



```csharp

uint flags = request.Action switch

{

&#x20;   PowerAction.Restart  => ShutdownFlags.Restart,

&#x20;   PowerAction.Shutdown => ShutdownFlags.PowerOff,

&#x20;   \_ => throw new ArgumentOutOfRangeException()

};



if (request.Force)

&#x20;   flags |= ShutdownFlags.ForceSelf;



if (request.Hybrid \&\& request.Action is PowerAction.Shutdown)

&#x20;   flags |= ShutdownFlags.Hybrid;

```



호출은 다음 구조로 구성합니다.



```csharp

using var privilege = ShutdownPrivilege.Enable();



uint error = NativeMethods.InitiateShutdown(

&#x20;   machineName: null,

&#x20;   message: null,

&#x20;   gracePeriod: 0,

&#x20;   shutdownFlags: flags,

&#x20;   reason: ShutdownReasons.PlannedOther);

```



\## 시스템 종료 권한



`InitiateShutdownW`를 로컬 시스템에서 호출하려면 프로세스 토큰에 `SeShutdownPrivilege`가 있어야 합니다. Microsoft 문서도 종료 전에 해당 권한을 활성화하도록 안내합니다.



권한 처리에는 다음 Win32 API를 사용합니다.



```text

GetCurrentProcess

&#x20;   ↓

OpenProcessToken

&#x20;   ↓

LookupPrivilegeValueW("SeShutdownPrivilege")

&#x20;   ↓

AdjustTokenPrivileges

&#x20;   ↓

InitiateShutdownW

```



`AdjustTokenPrivileges`는 프로세스 토큰에 존재하지 않는 권한을 추가할 수 없습니다. 기존 권한을 활성화하거나 비활성화할 수 있을 뿐입니다.



따라서 회사 정책에서 사용자의 `Shut down the system` 권한을 제거한 경우 Skipower가 이를 우회하지 못합니다.



애플리케이션 매니페스트도 다음 정책을 사용합니다.



```xml

<requestedExecutionLevel

&#x20;   level="asInvoker"

&#x20;   uiAccess="false" />

```



기본 실행부터 관리자 권한을 요구하지 않습니다.



`ERROR\_ACCESS\_DENIED`가 발생해도 자동으로 UAC 승격을 시도하지 않습니다. 해당 장치의 보안 정책을 그대로 따릅니다.



\## 종료 사유



`dwReason`에 0을 전달하지 않습니다.



Microsoft는 0을 사용하면 계획되지 않은 종료로 기록되며 시스템 설정에 따라 추가 상태 정보를 수집하느라 종료가 늦어질 수도 있다고 설명합니다.



Skipower는 사용자 요청에 따른 정상적인 종료이므로 다음 값을 사용합니다.



```csharp

SHTDN\_REASON\_MAJOR\_OTHER |

SHTDN\_REASON\_MINOR\_OTHER |

SHTDN\_REASON\_FLAG\_PLANNED

```



Windows 이벤트 로그에는 계획된 일반 종료로 남습니다.



업데이트나 운영 체제 유지보수 사유 코드는 사용하지 않습니다. Skipower가 Windows Update를 실행한 것처럼 잘못 기록할 수 있기 때문입니다.



\## 강제 종료 정책



기본 실행에서는 `SHUTDOWN\_FORCE\_SELF`를 지정하지 않습니다.



따라서 저장되지 않은 문서나 종료를 차단하는 프로그램이 있다면 Windows가 정상적인 종료 상호 작용을 수행할 수 있습니다.



CLI에서는 명시적인 `--force` 옵션만 제공합니다.



```text

skipower shutdown --force

skipower restart --force

```



`--force`는 현재 세션에만 `SHUTDOWN\_FORCE\_SELF`를 적용합니다.



다른 로그인 세션을 종료하는 `SHUTDOWN\_FORCE\_OTHERS`는 1차 버전에서 노출하지 않습니다. 다른 사용자의 저장되지 않은 작업까지 손실시킬 수 있기 때문입니다.



다른 사용자가 로그인한 상태라면 기본 호출은 `ERROR\_SHUTDOWN\_USERS\_LOGGED\_ON`을 반환할 수 있습니다. `InitiateShutdownW`가 이 동작을 공식적으로 정의합니다.



\## 일반 종료와 하이브리드 종료



Windows 8 이후 `InitiateShutdownW`에서 `SHUTDOWN\_HYBRID`를 지정하지 않으면 전체 시스템 종료를 수행합니다. 이 플래그를 지정하면 Fast Startup에 사용하는 하이브리드 종료를 요청할 수 있습니다.



초기 버전은 전체 종료를 기본값으로 둡니다.



```text

skipower shutdown

```



필요하면 다음 옵션을 별도로 제공합니다.



```text

skipower shutdown --hybrid

```



재시작에는 하이브리드 옵션을 적용하지 않습니다.



\## CLI 인터페이스



초기 명령은 작게 유지합니다.



```text

skipower shutdown

skipower restart



skipower shutdown --force

skipower restart --force



skipower shutdown --hybrid



skipower --help

skipower --version

```



예상 출력은 다음과 같습니다.



```text

> skipower restart



Restart request accepted.

Windows Update installation was not requested.

```



실패 시 Windows 오류를 사용자 의미로 변환합니다.



```text

> skipower restart



Restart was not requested.

Another user is currently signed in.

```



자동화 용도를 위해 `--json`도 추가할 수 있습니다.



```json

{

&#x20; "action": "restart",

&#x20; "status": "accepted",

&#x20; "installUpdatesRequested": false

}

```



종료 코드는 다음 범위로 고정합니다.



```text

0   요청 접수

2   잘못된 명령행 인수

10  종료 권한 없음

11  다른 사용자 로그인

12  이미 종료 진행 중

13  이미 종료 예약됨

14  시스템 준비 안 됨

30  기타 Win32 오류

```



\## GUI 구조



GUI는 기능 범위가 매우 작으므로 WPF 전체 애플리케이션보다 Win32 기반의 작은 대화 상자가 제품 성격에 잘 맞습니다.



.NET 10 Native AOT와 `LibraryImport` 또는 CsWin32를 이용하여 표준 Windows Task Dialog를 호출하는 방식을 우선 검토합니다.



화면 구성은 다음 정도면 충분합니다.



```text

Skipower



Windows Update 설치를 요청하지 않고

전원 작업을 실행합니다.



\[ 종료 ]

&#x20; 업데이트 설치를 요청하지 않고 PC를 종료합니다.



\[ 다시 시작 ]

&#x20; 업데이트 설치를 요청하지 않고 PC를 다시 시작합니다.



Windows가 이미 재부팅을 요구하거나

조직 정책이 적용된 경우 업데이트를 완료할 수 있습니다.



\[ 취소 ]

```



GUI에는 강제 종료 기능을 넣지 않습니다.



GUI 사용자가 저장되지 않은 데이터를 잃을 가능성을 낮추고, 강제 종료이 필요할 때만 CLI의 `--force`를 사용하도록 기능을 분리합니다.



\## GUI와 CLI 바이너리 분리



하나의 실행 파일에서 GUI와 CLI를 동시에 제공하는 구조는 채택하지 않습니다.



Windows GUI subsystem 실행 파일은 명령 프롬프트의 일반적인 CLI 프로그램과 프로세스 대기 및 콘솔 연결 동작이 다릅니다. 반대로 Console subsystem 실행 파일을 더블클릭하면 불필요한 콘솔 창이 표시될 수 있습니다.



따라서 두 실행 파일을 제공합니다.



```text

skipower.exe

Skipower.exe

```



`skipower.exe`는 CLI입니다.



`Skipower.exe`는 GUI입니다.



두 프로그램은 동일한 `Skipower.Core`와 `Skipower.Windows`를 사용합니다.



\## 업데이트 상태 진단 계층



초기 버전의 전원 작업은 Windows Update 상태 조회에 의존하지 않습니다.



상태 조회가 실패한다고 종료 자체가 실패해서는 안 되기 때문입니다.



향후 다음 인터페이스를 별도 계층으로 추가할 수 있습니다.



```csharp

public interface IUpdateStatusProvider

{

&#x20;   Task<UpdateStatus> GetStatusAsync(

&#x20;       CancellationToken cancellationToken);

}

```



WUA의 `ISystemInformation.RebootRequired`는 업데이트 설치 또는 제거를 마치기 위해 시스템 재시작이 필요한지를 공식적으로 알려줍니다.



`IUpdateSearcher`는 다음과 같은 조건으로 미설치 업데이트를 검색할 수 있습니다.



```text

IsInstalled = 0 and IsHidden = 0

```



검색 결과의 `IUpdate.IsDownloaded`를 조회하면 업데이트 콘텐츠가 로컬 캐시에 모두 다운로드됐는지도 구분할 수 있습니다.



이를 이용하면 향후 다음 상태를 제공할 수 있습니다.



```text

NoPendingUpdate

Available

Downloaded

RebootRequired

Unknown

```



다만 이 기능은 전원 작업의 선행 조건으로 사용하지 않습니다.



\## WUA와 Native AOT의 경계



Windows Update Agent는 COM 기반 API입니다.



WUA의 주요 인터페이스는 `IDispatch` 계열이며 일반적인 .NET COM interop을 사용할 수 있습니다. 그러나 .NET Native AOT는 기본 제공 COM interop을 지원하지 않습니다. Microsoft도 Native AOT에서 기본 COM을 사용할 수 없다고 명시합니다.



따라서 1차 버전에서는 WUA를 핵심 바이너리에 연결하지 않습니다.



이 결정으로 CLI와 경량 GUI를 Native AOT로 게시할 수 있습니다.



향후 상태 진단을 추가할 때는 다음 두 방식 중 하나를 선택할 수 있습니다.



1\. 별도의 CoreCLR 기반 `Skipower.UpdateProbe` 프로세스를 구성합니다.

2\. `IDispatch`를 직접 호출하는 네이티브 COM 브리지를 구현합니다.



2번은 코드 크기에 비해 구현 비용이 큽니다. 상태 표시가 제품의 핵심 기능으로 자리 잡은 뒤 검토하는 편이 적합합니다.



\## 빌드와 배포



CLI와 GUI 모두 .NET 10 Native AOT 게시를 목표로 합니다.



대상 RID는 우선 다음 두 개입니다.



```text

win-x64

win-arm64

```



.NET 10은 Windows x64와 Arm64에서 Native AOT를 지원합니다.



CLI 게시 예시는 다음과 같습니다.



```xml

<PropertyGroup>

&#x20; <TargetFramework>net10.0-windows</TargetFramework>

&#x20; <PublishAot>true</PublishAot>

&#x20; <SelfContained>true</SelfContained>

&#x20; <InvariantGlobalization>true</InvariantGlobalization>

</PropertyGroup>

```



GUI는 표준 Win32 대화 상자와 Win32 API만 사용해 동일한 배포 구조를 유지합니다.



결과물은 다음과 같이 구성합니다.



```text

skipower-win-x64.zip

&#x20;├─ skipower.exe

&#x20;└─ Skipower.exe



skipower-win-arm64.zip

&#x20;├─ skipower.exe

&#x20;└─ Skipower.exe

```



설치 프로그램이나 Windows 서비스는 필요하지 않습니다.



초기에는 Portable ZIP과 GitHub Release를 제공하고 이후 WinGet 패키지를 추가할 수 있습니다.



전원 제어 프로그램이라는 성격상 배포 바이너리에는 Authenticode 서명을 적용합니다.



\## 오류 처리



`InitiateShutdownW`의 주요 반환 코드를 명시적으로 매핑합니다.



```text

ERROR\_SUCCESS

&#x20;   요청 접수



ERROR\_ACCESS\_DENIED

&#x20;   SeShutdownPrivilege 사용 불가



ERROR\_SHUTDOWN\_IN\_PROGRESS

&#x20;   이미 종료 또는 재시작 진행 중



ERROR\_SHUTDOWN\_IS\_SCHEDULED

&#x20;   다른 종료 작업 예약됨



ERROR\_SHUTDOWN\_USERS\_LOGGED\_ON

&#x20;   다른 사용자 세션 존재



ERROR\_NOT\_READY

&#x20;   Windows가 현재 종료 요청을 받을 수 없음

```



`ERROR\_NOT\_READY`에 한해서 짧은 간격으로 한 번 재시도할 수 있습니다. Microsoft 문서도 시스템이 일시적으로 준비되지 않은 경우 잠시 뒤 재시도할 수 있다고 설명합니다.



`ERROR\_SHUTDOWN\_IS\_SCHEDULED`가 발생했을 때 `SHUTDOWN\_GRACE\_OVERRIDE`를 자동으로 적용하지 않습니다.



기존 종료 예약을 Skipower가 임의로 덮어쓰지 않도록 하기 위함입니다.



\## 로그 정책



기본 실행에서는 파일 로그를 남기지 않습니다.



CLI의 `--verbose`가 지정된 경우에만 다음 정보를 stderr로 출력합니다.



```text

Action

Native flags

Privilege activation result

InitiateShutdownW result

Windows error code

```



업데이트 제목, 사용자 이름, 컴퓨터 이름과 같은 정보는 수집하지 않습니다.



원격 텔레메트리도 기본 기능에 포함하지 않습니다.



\## 테스트 전략



단위 테스트에서는 실제 시스템을 종료하지 않습니다.



`IPowerNativeApi`를 만들어 Win32 호출을 추상화합니다.



```csharp

internal interface IPowerNativeApi

{

&#x20;   uint InitiateShutdown(

&#x20;       uint flags,

&#x20;       uint reason);

}

```



다음 항목을 자동화 테스트합니다.



\- 종료 플래그 생성

\- 재시작 플래그 생성

\- `SHUTDOWN\_INSTALL\_UPDATES` 미포함

\- `--force` 처리

\- 하이브리드 종료 처리

\- Win32 오류 매핑

\- 권한 활성화 실패 처리

\- CLI 종료 코드



실제 Windows 통합 테스트는 VM으로 수행합니다.



주요 검증 환경은 다음과 같습니다.



```text

Windows 11 24H2 x64

Windows 11 25H2 x64

Windows 11 26H1 x64

Windows 11 26H1 Arm64

Windows 11 Enterprise 관리 장치

Windows 10 22H2 검증 환경

```



업데이트 상태별 테스트 시나리오는 다음과 같습니다.



1\. 업데이트가 없는 상태에서 일반 종료를 실행합니다.

2\. 업데이트가 없는 상태에서 일반 재시작을 실행합니다.

3\. 업데이트 다운로드 전 상태에서 재시작을 실행합니다.

4\. 업데이트 다운로드 완료 상태에서 재시작을 실행합니다.

5\. `RebootRequired` 전 상태에서 재시작을 실행합니다.

6\. `RebootRequired` 상태에서 실제 Windows 동작을 기록합니다.

7\. 업데이트 기한 정책이 적용된 장치에서 동작을 기록합니다.

8\. 다른 사용자가 로그인한 상태에서 오류 반환을 확인합니다.

9\. 저장되지 않은 애플리케이션이 종료를 차단하는지 확인합니다.

10\. `--force`가 현재 세션을 강제로 종료하는지 확인합니다.

11\. 기존 종료가 예약된 상태에서 Skipower가 이를 덮어쓰지 않는지 확인합니다.

12\. Fast Startup 활성화 및 비활성화 상태를 각각 확인합니다.



특히 3번부터 7번까지는 공개 API 문서만으로 실제 servicing 동작을 완전히 보장할 수 없으므로 Windows 빌드별 회귀 테스트 대상으로 유지합니다.



\## 1차 릴리스 범위



첫 릴리스는 다음 기능에 집중합니다.



```text

GUI

&#x20;├─ 종료

&#x20;├─ 다시 시작

&#x20;└─ 동작 범위 안내



CLI

&#x20;├─ shutdown

&#x20;├─ restart

&#x20;├─ --force

&#x20;├─ --hybrid

&#x20;├─ --json

&#x20;├─ --help

&#x20;└─ --version

```



다음 기능은 이후 버전으로 분리합니다.



```text

Update status

Windows Update 상세 진단

Tray integration

Shell integration

예약 종료

업데이트 일시 중지

원격 종료

조직 정책 진단

```



\## 완료 조건



1차 버전은 다음 조건을 충족하면 완료로 판단합니다.



\- `SHUTDOWN\_INSTALL\_UPDATES`를 사용하지 않는 Win32 종료 및 재시작 구현

\- `SeShutdownPrivilege`의 범위 제한 활성화

\- 기본 관리자 권한 요구 없음

\- Windows Update 서비스와 설정의 무수정

\- 다른 로그인 세션의 강제 종료 방지

\- CLI와 GUI의 동일한 코어 사용

\- x64 및 Arm64 Native AOT 빌드

\- 업데이트 유무와 관계없이 동일한 명령 인터페이스 제공

\- 실제 Windows Update 상태를 이용한 통합 테스트

\- “업데이트를 반드시 우회한다”는 보장 대신 API가 제공하는 정확한 동작 범위 표시



\## 최종 구현 방향



Skipower의 핵심은 Windows Update를 공격적으로 억제하는 로직이 아닙니다.



다음 한 줄이 프로젝트의 기술적 중심입니다.



```csharp

flags \&= \~SHUTDOWN\_INSTALL\_UPDATES;

```



실제 구현에서는 애초에 해당 플래그를 추가하지 않는 방식이 더 적합합니다.



```text

Restart

&#x20;   = SHUTDOWN\_RESTART



Shutdown

&#x20;   = SHUTDOWN\_POWEROFF

```



여기에 `SeShutdownPrivilege` 처리와 오류 매핑만 추가하면 핵심 기능을 완성할 수 있습니다.



이 구조라면 Windows Update 내부 구현이나 Update Orchestrator의 비공개 인터페이스에 의존하지 않습니다. Microsoft가 2026년부터 기본 전원 UI에 같은 사용자 선택권을 확대하더라도 CLI, 이전 Windows 환경, 점진 배포 대상 장치, 자동화 환경이라는 용도가 남습니다.

