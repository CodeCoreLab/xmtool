using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Server.Api;

/// <summary>
/// IRdpBackend 工厂（平台双轨收口，对齐架构规范 PLAT-5.4：平台 API 收口到单实现类）。
/// Windows = CredWriteW + mstsc（源项目原样）；Linux = L1$ 文件凭据 + xfreerdp（M3 实现）。
/// </summary>
public static class RdpBackendFactory
{
    public static IRdpBackend Create() =>
        OperatingSystem.IsWindows() ? WindowsRdpBackend.Instance : LinuxRdpBackend.Instance;
}

/// <summary>
/// IRdpLauncher 工厂（同上双轨）：Windows 生成 .rdp + mstsc 拉起；
/// Linux 生成 xfreerdp 参数。
/// </summary>
public static class RdpLauncherFactory
{
    public static IRdpLauncher Create() =>
        OperatingSystem.IsWindows() ? new WindowsRdpLauncher() : new LinuxRdpLauncher();
}
