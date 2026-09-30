namespace Skipower.Core;

internal static class KoreanStrings
{
    internal static string Get(MessageId id) => id switch
    {
        MessageId.Usage => "사용법: skipower <shutdown|restart> [--force] [--hybrid] [--json] [--verbose] [--elevate]\n        skipower --help\n        skipower --version",
        MessageId.InvalidCommand => "명령행 인수가 올바르지 않습니다.",
        MessageId.ShutdownAccepted => "종료 요청을 Windows가 접수했습니다.",
        MessageId.RestartAccepted => "재시작 요청을 Windows가 접수했습니다.",
        MessageId.InstallUpdatesNotRequested => "Windows Update 설치는 요청하지 않았습니다.",
        MessageId.UpdateMayStillComplete => "재부팅을 기다리는 업데이트나 조직 정책에 따라 업데이트를 완료할 수 있습니다.",
        MessageId.ShutdownNotRequested => "종료를 요청하지 못했습니다.",
        MessageId.RestartNotRequested => "재시작을 요청하지 못했습니다.",
        MessageId.AccessDenied => "현재 보안 정책에서 시스템 종료 권한을 사용할 수 없습니다.",
        MessageId.OtherUsersLoggedOn => "다른 사용자가 로그인해 있습니다.",
        MessageId.ShutdownInProgress => "종료 작업이 이미 진행 중입니다.",
        MessageId.ShutdownScheduled => "종료 작업이 이미 예약되어 있습니다.",
        MessageId.SystemNotReady => "Windows가 현재 요청을 받을 준비가 되지 않았습니다.",
        MessageId.WindowsError => "Windows 오류 코드: {0}.",
        MessageId.Unknown => "알 수 없음",
        MessageId.ElevationCancelled => "관리자 권한 승인을 취소했습니다. 요청을 다시 시도하지 않았습니다.",
        MessageId.ElevationLaunchFailed => "관리자 권한으로 다시 시도할 수 없습니다. Windows 오류 코드: {0}.",
        MessageId.ElevationUnexpectedExit => "관리자 권한 재시도 결과를 받지 못했습니다. 프로세스 종료 코드: {0}.",
        MessageId.DiagnosticRequest => "동작: {0}; 네이티브 플래그: 0x{1:X8}",
        MessageId.DiagnosticPrivilege => "종료 권한 활성화 Windows 코드: {0}",
        MessageId.DiagnosticShutdown => "InitiateShutdownW Windows 코드: {0}",
        MessageId.DiagnosticElevatedExit => "관리자 권한 재시도 프로세스 종료 코드: {0}",
        MessageId.GuiLead => "Windows Update 설치를 요청하지 않고 전원 작업을 실행합니다.",
        MessageId.GuiScope => "Windows가 이미 재부팅을 요구하거나 조직 정책이 적용된 경우 업데이트를 완료할 수 있습니다.",
        MessageId.GuiShutdown => "종료",
        MessageId.GuiShutdownDescription => "업데이트 설치를 요청하지 않고 PC를 종료합니다.",
        MessageId.GuiRestart => "다시 시작",
        MessageId.GuiRestartDescription => "업데이트 설치를 요청하지 않고 PC를 다시 시작합니다.",
        MessageId.GuiCancel => "취소",
        MessageId.GuiClose => "닫기",
        MessageId.GuiElevateRetry => "관리자 권한으로 다시 시도",
        MessageId.GuiAccepted => "Windows가 요청을 접수했습니다. 업데이트 설치는 요청하지 않았습니다.",
        MessageId.GuiAccessDenied => "종료 권한이 없습니다. 관리자 승인으로 다시 시도할 수 있습니다.",
        MessageId.GuiAccessDeniedElevated => "관리자 권한으로 실행했지만 현재 보안 정책에서 종료 권한을 사용할 수 없습니다.",
        MessageId.GuiOtherUsers => "다른 사용자가 로그인해 있어 요청을 접수하지 않았습니다.",
        MessageId.GuiInProgress => "종료 또는 재시작이 이미 진행 중입니다.",
        MessageId.GuiScheduled => "다른 종료 작업이 이미 예약되어 있습니다.",
        MessageId.GuiNotReady => "Windows가 현재 요청을 받을 준비가 되지 않았습니다.",
        MessageId.GuiFailed => "요청에 실패했습니다. Windows 오류 코드: {0}.",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };
}
