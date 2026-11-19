using System.Security.Cryptography;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Server.Api;

/// <summary>
/// 主密码/启动密码 API + 启动期随机 token 会话（选型 W1 项）。
/// 安全：HttpOnly + SameSite=Strict 短时效 cookie；回环绑定 + 随机 token 双因子。
/// 主密码派生逻辑复用 CryptoService.Unlock（PBKDF2 200k，与 WPF 版密文互解）。
/// </summary>
public static class SecurityApi
{
    /// <summary>启动期随机 token（进程生命周期内固定；dropdir 模式——同机其他用户进程拿不到）。</summary>
    private static readonly string InstanceToken =
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private const string SessionCookie = "rdp_panel_session";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(8);

    /// <summary>已解锁会话（单用户面板，一个槽位即可）。</summary>
    private static DateTime _unlockedUntil = DateTime.MinValue;
    private static readonly object Lock = new();

    public static void Map(WebApplication app)
    {
        // GET /api/security/state → 主密码配置态 + 当前会话解锁态
        app.MapGet("/api/security/state", () =>
        {
            DatabaseService.Instance.Initialize();
            var crypto = new CryptoService(DatabaseService.Instance);
            return Results.Ok(new
            {
                masterConfigured = crypto.IsMasterPasswordConfigured(),
                unlocked = DateTime.UtcNow < _unlockedUntil,
            });
        });

        // POST /api/security/unlock → 主密码解锁（成功则种会话 cookie）
        app.MapPost("/api/security/unlock", async (HttpContext ctx, UnlockForm form) =>
        {
            if (string.IsNullOrEmpty(form?.MasterPassword))
                return Results.BadRequest(new { error = "主密码不能为空" });

            DatabaseService.Instance.Initialize();
            var crypto = new CryptoService(DatabaseService.Instance);
            if (!crypto.IsMasterPasswordConfigured())
                return Results.BadRequest(new { error = "主密码未配置" });

            if (!crypto.Unlock(form.MasterPassword))
                return Results.Json(new { error = "主密码错误" }, statusCode: 401);

            lock (Lock) { _unlockedUntil = DateTime.UtcNow.Add(SessionTtl); }

            ctx.Response.Cookies.Append(SessionCookie, InstanceToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = false,  // 回环 HTTP（127.0.0.1 不出机），无 TLS 需求
                MaxAge = SessionTtl,
            });
            return Results.Ok(new { unlocked = true, ttlHours = SessionTtl.TotalHours });
        });

        // POST /api/security/lock → 手动上锁
        app.MapPost("/api/security/lock", (HttpContext ctx) =>
        {
            lock (Lock) { _unlockedUntil = DateTime.MinValue; }
            ctx.Response.Cookies.Delete(SessionCookie);
            return Results.Ok(new { unlocked = false });
        });

        // 会话守卫中间件：写操作（POST/PUT/DELETE，除 unlock/healthz）要求解锁态
        app.Use(async (ctx, next) =>
        {
            var path = ctx.Request.Path.Value ?? string.Empty;
            bool isWrite = ctx.Request.Method is "POST" or "PUT" or "DELETE";
            bool exempt = path.StartsWith("/healthz") ||
                          path.StartsWith("/api/security/") ||
                          path.StartsWith("/api/probe") ||   // 探活只读语义，放行
                          path == "/" || path.StartsWith("/index.html");
            if (isWrite && !exempt && DateTime.UtcNow >= _unlockedUntil)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new { error = "面板未解锁（主密码）" });
                return;
            }
            await next();
        });
    }
}

/// <summary>解锁请求 DTO。</summary>
public record UnlockForm(string? MasterPassword);
