# Skipower의 CLI와 GUI

2026년 9월 30일 기준 Skipower는 Windows 10과 Windows 11에서 일반 종료와 재시작을 요청하는 CLI와 MewUI GUI를 제공합니다. 두 실행 파일은 .NET 10과 C# 14를 사용하며 RID를 지정한 게시에서 Native AOT를 기본으로 적용합니다.

CLI는 자동화용 옵션과 종료 코드를 제공하고 GUI는 종료, 다시 시작, 동작 범위 안내에 집중합니다. 두 프로그램은 같은 전원 제어 계층을 호출합니다. Windows UI 언어가 한국어이면 한국어를 표시하고 그 외에는 영어를 표시합니다. 아래에서 요청의 범위, 사용법, 권한 재시도, 빌드, 검증 결과, 배포 조건을 순서대로 정리하겠습니다.

## 전원 요청에 포함되지 않는 업데이트 설치

Skipower는 `InitiateShutdownW`에 `SHUTDOWN_RESTART` 또는 `SHUTDOWN_POWEROFF`를 전달합니다. 명시적으로 요청하지 않은 한 현재 세션 강제 종료나 하이브리드 종료 플래그도 전달하지 않습니다. 모든 실행 경로에서 `SHUTDOWN_INSTALL_UPDATES`를 제외합니다. [Microsoft의 InitiateShutdownW 문서](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw)는 이 플래그가 종료 전 업데이트 설치를 요청한다고 설명합니다.

Windows가 이미 업데이트 설치를 마치고 재부팅을 기다리거나 조직 정책의 기한이 적용된 경우에는 재시작 과정에서 업데이트를 완료할 수 있습니다. Skipower는 Windows Update 서비스, 업데이트 파일, 정책을 변경하지 않습니다. 요청 접수 결과는 종료 완료를 뜻하지 않습니다.

## 두 실행 파일의 사용법

CLI 사용법부터 살펴보겠습니다. `skipower.exe`는 `shutdown`, `restart`, `--force`, 종료 전용 `--hybrid`, `--json`, `--verbose`, `--elevate`, `--help`, `--version`을 제공합니다. `--force`는 호출한 사용자 세션에만 적용하며 다른 사용자의 세션은 강제로 종료하지 않습니다. [Microsoft의 종료 플래그 설명](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw)에서 세션별 강제 종료 범위를 확인할 수 있습니다.

다음 명령은 업데이트 설치 플래그 없이 재시작을 요청합니다.

```powershell
.\skipower.exe restart --json
```

CLI 종료 코드는 요청 접수 `0`, 인수 오류 `2`, 권한 없음 `10`, 다른 사용자 로그인 `11`, 종료 진행 중 `12`, 종료 예약됨 `13`, 시스템 준비 안 됨 `14`, 기타 오류 `30`입니다. GUI 파일명은 `Skipower.Gui.exe`입니다. [MewUI](https://github.com/aprillz/MewUI)의 Win32 호스트, GDI 렌더러, 공통 전원 제어 계층을 사용합니다. 두 파일을 대소문자만으로 구분하면 Windows의 일반적인 파일 시스템에서 이름이 충돌하므로 [초기 설계서](docs/plan.md)의 GUI 파일명을 조정했습니다.

## 권한 오류 이후의 관리자 재시도

현재 사용자 토큰에 종료 권한이 없을 때 CLI의 `--elevate` 옵션은 UAC 승인을 요청한 뒤 같은 전원 작업을 한 번 더 시도합니다. GUI는 권한 오류가 발생했을 때 ‘관리자 권한으로 다시 시도’ 버튼을 표시합니다. 기본 실행 매니페스트는 `asInvoker`를 유지합니다. [Microsoft의 관리자 권한 실행 문서](https://learn.microsoft.com/en-us/windows/win32/secbp/running-with-administrator-privileges)는 `runas`를 통한 명시적 승격을 설명합니다.

다음 명령은 먼저 현재 권한으로 재시작을 요청합니다. 권한 오류가 발생한 경우에만 UAC 승인을 요청합니다.

```powershell
.\skipower.exe restart --elevate
```

사용자가 UAC 승인을 취소하면 CLI는 종료 코드 `10`을 반환하고 GUI는 취소 결과를 표시합니다. 관리자 토큰에도 종료 권한이 없으면 재시도 역시 실패합니다. JSON 속성명과 상태 값은 자동화 계약을 위해 영어로 고정하며 사람이 읽는 CLI 메시지와 GUI 문안은 [Windows의 현재 UI 문화권](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentuiculture)에 따라 한국어 또는 영어로 표시합니다.

## Native AOT 빌드와 ZIP

RID를 지정한 게시에서는 CLI와 GUI가 Native AOT를 적용합니다. [MewUI의 설치 문서](https://github.com/aprillz/MewUI/blob/main/docs/Installation.md)에 따라 GUI는 MewUI 0.18.1의 Core, Win32, GDI 패키지만 참조합니다. 패키지 버전을 고정해 API 변경의 영향을 통제합니다.

다음 명령은 로컬 컴퓨터에 해당 RID의 C++ 도구 모음이 있을 때 두 실행 파일과 라이선스 고지를 ZIP으로 묶습니다.

```powershell
.\scripts\publish.ps1 -Rid win-x64
# Arm64 도구 모음 또는 Arm64 Windows에서 실행할 때:
.\scripts\publish.ps1 -Rid win-arm64
```

산출물은 `artifacts/skipower-win-x64.zip` 또는 `artifacts/skipower-win-arm64.zip`입니다. [Microsoft의 Native AOT 교차 컴파일 문서](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/cross-compile)는 Windows x64에서 Arm64로 게시할 때 Arm64 C++ 빌드 도구가 필요하다고 설명합니다. [GitHub Actions 빌드 작업](.github/workflows/build.yml)은 `main` 브랜치 푸시와 수동 실행 시 `windows-latest`와 `windows-11-arm`에서 테스트와 Native AOT 게시를 수행합니다. 각 작업의 Actions 실행 페이지에서 `skipower-win-x64` 또는 `skipower-win-arm64` 아티팩트를 내려받으면 실행 파일과 라이선스 고지가 담긴 ZIP을 얻습니다. [GitHub의 러너 문서](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)는 두 Windows 러너를 공개 저장소에서 사용할 수 있다고 설명합니다.

## 시스템 종료 없는 자동 검증

단위 테스트는 가짜 Win32 호출, 가짜 권한 범위, 가짜 권한 상승 실행기를 사용합니다. 종료 플래그에 업데이트 설치와 다른 세션 강제 종료가 들어가지 않는지 확인하고 오류 매핑, UAC 취소, 언어 선택, CLI 종료 코드를 검사합니다. [테스트 코드](tests/Skipower.Windows.Tests/WindowsPowerControllerTests.cs)와 [CLI 테스트](tests/Skipower.Core.Tests/CliRunnerTests.cs)에 실행 범위가 드러납니다.

다음 명령으로 실제 종료를 요청하지 않고 테스트를 실행할 수 있습니다.

```powershell
dotnet test Skipower.slnx
```

2026년 9월 30일 로컬 Windows x64에서 31개 테스트와 CLI, GUI의 x64 Native AOT 게시가 통과했습니다. GUI 실행 파일이 시작되고 CLI가 시스템 UI 언어인 한국어를 표시하는 것도 확인했습니다. 이 컴퓨터에는 Arm64 C++ 링커가 없어 로컬 Arm64 AOT 게시를 완료하지 못했습니다. 실제 UAC 승인과 전원 작업은 VM 통합 테스트 대상으로 남습니다.

## 실기기 검증과 배포 조건

실제 Windows Update 상태별 결과는 [설계서의 통합 테스트 시나리오](docs/plan.md)에 따라 VM에서 확인할 대상으로 남습니다. 특히 재부팅 대기 상태, 관리 정책, 다른 사용자 세션, 저장하지 않은 문서가 있는 경우를 구분해 기록할 수 있습니다. [Microsoft 문서](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-initiateshutdownw)도 API 성공 반환이 실제 종료 완료를 보장하지 않는다고 설명합니다.

공개 배포 전에는 x64와 Arm64의 실제 종료 동작을 확인하고 Authenticode 서명을 적용할 수 있습니다. 현재 ZIP 제작 스크립트에는 서명 단계가 없습니다. MewUI의 [MIT 라이선스](licenses/MewUI-LICENSE.txt)와 [제3자 고지](licenses/THIRD_PARTY_NOTICES.md)는 ZIP에 포함합니다.

## 마무리

여기까지 정리하면 Skipower는 Windows에 일반 전원 작업을 요청하며 업데이트 설치 플래그를 전달하지 않습니다. 이미 진행된 업데이트와 관리 정책이 실제 종료 동작에 미치는 영향은 별도로 확인할 대상으로 남습니다.

자동화에서는 CLI 종료 코드와 JSON을 사용할 수 있습니다. 사람이 직접 전원 작업을 선택하는 경우에는 MewUI GUI에서 작업 범위를 확인할 수 있습니다.
