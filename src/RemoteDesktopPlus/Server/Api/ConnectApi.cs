using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;

namespace RemoteDesktopPlus.Server.Api;

/// <summary>
/// 连接动作 API：探活（OnlineProbe）与 RDP 拉起（WindowsBackend / M3 LinuxBackend 双轨）。
/// 拉起的安全性质保持 IRdpBackend 契约：密码只进凭据存储，进程参数面不含密码。
/// </summary>
public static class ConnectApi
{
    public static void Map(WebApplication app)
    {
        // POST /api/probe/{id} → 单连接探活（OnlineProbe 纯 TcpClient，双平台通用）
        app.MapPost("/api/probe/{id}", (long id) =>
        {
            var db = DatabaseService.Instance;
            db.Initialize();
            var item = db.LoadAll().FirstOrDefault(c => c.Id == id);
            if (item == null) return Results.NotFound(new { error = "连接不存在" });

            bool online = OnlineProbe.IsOnlineAsync(item.Host, item.Port).GetAwaiter().GetResult();
            return Results.Ok(new { id, online });
        });

        // POST /api/scan → 全量探活（等价 VM ScanOnlineAsync：串行逐个，与 WPF 版一致）
        app.MapPost("/api/scan", () =>
        {
            var db = DatabaseService.Instance;
            db.Initialize();
            var results = new List<object>();
            foreach (var item in db.LoadAll())
            {
                bool online = OnlineProbe.IsOnlineAsync(item.Host, item.Port).GetAwaiter().GetResult();
                results.Add(new { id = item.Id, host = item.Host, online });
            }
            return Results.Ok(new { scannedAt = DateTime.UtcNow, results });
        });

        // POST /api/connect/{id} → 拉起 RDP 会话
        app.MapPost("/api/connect/{id}", (long id, ConnectForm form) =>
        {
            var db = DatabaseService.Instance;
            db.Initialize();
            var item = db.LoadAll().FirstOrDefault(c => c.Id == id);
            if (item == null) return Results.NotFound(new { error = "连接不存在" });

            // 密码来源：请求体（前端经主密码解锁后的会话明文）。
            // 库内有密文但请求未带密码 → 401 要求解锁（不静默退化弹窗）。
            if (!string.IsNullOrEmpty(item.EncryptedPassword) && string.IsNullOrEmpty(form?.Password))
            {
                return Results.Json(new { error = "需要主密码解锁", needUnlock = true },
                    statusCode: 401);
            }

            var backend = RdpBackendFactory.Create();
            var target = WindowsRdpBackend.BuildTarget(item.Host);
            if (!string.IsNullOrEmpty(form?.Password))
            {
                if (!backend.StoreCredential(target, item.UserName, form.Password))
                    return Results.Json(new { error = "凭据写入失败（客户端将退化为交互式询问密码）" },
                        statusCode: 500);
            }

            var opt = new RdpLauncher.RdpOptions(
                Host: item.Host,
                Port: item.Port,
                UserName: item.UserName,
                Password: string.Empty,  // 契约：密码不进 .rdp/参数面，只走凭据存储
                Width: form?.Width,
                Height: form?.Height,
                RedirectLocalDrive: false,
                LocalDriveLetter: string.Empty,
                ForceConsole: false);

            var launcher = RdpLauncherFactory.Create();
            launcher.Launch(opt);
            db.UpdateLastConnectedAt(id, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            return Results.Ok(new { id, launched = true, backend = backend.GetType().Name });
        });
    }
}

/// <summary>连接请求 DTO（密码=主密码解锁后的会话明文，短时效内存传递）。</summary>
public record ConnectForm(string? Password, int? Width, int? Height, bool? FullScreen);
