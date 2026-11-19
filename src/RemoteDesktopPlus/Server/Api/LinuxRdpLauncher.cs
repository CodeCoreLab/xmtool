using RemoteDesktopPlus.Server.Api;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Services;

/// <summary>
/// Linux（信创）启动器：xfreerdp 参数化拉起（不经 .rdp 文件——xfreerdp 原生参数驱动）。
/// 密码经 stdin 传入（/from:stdin），参数面零密码——安全性质与 Windows 版对齐。
/// </summary>
public sealed class LinuxRdpLauncher : IRdpLauncher
{
    public void Launch(RdpLauncher.RdpOptions opt)
    {
        var backend = LinuxRdpBackend.Instance;
        var target = WindowsRdpBackend.BuildTarget(opt.Host);

        // 凭据：优先 L1$ 文件（StoreCredential 已写入），其次调用方传入的 opt.Password（无凭据场景）
        var cred = LinuxRdpBackend.ReadCredential(target);
        string? password = cred?.Password ?? (string.IsNullOrEmpty(opt.Password) ? null : opt.Password);
        string userName = cred?.User ?? opt.UserName;

        LinuxRdpBackend.LaunchXfreerdp(opt.Host, opt.Port, userName, password,
            opt.Width, opt.Height, fullScreen: opt.Width is null && opt.Height is null);
    }

    public void ClearCredential(string host)
    {
        LinuxRdpBackend.Instance.DeleteCredential(WindowsRdpBackend.BuildTarget(host));
    }
}
