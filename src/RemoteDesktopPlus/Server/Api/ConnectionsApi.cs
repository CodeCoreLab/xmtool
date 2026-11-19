using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;

namespace RemoteDesktopPlus.Server.Api;

/// <summary>
/// 连接 CRUD API：映射 MainViewModel 连接库操作（S20 Connections partial）。
/// 设计原则（ub-mode 入口校验一次）：Controller 层做参数校验，进服务层即信任。
/// 对象新建走 VM 同款路径（Dialog 表单→CryptoService 加密→DatabaseService 落库），
/// 保证与 WPF 版行为一致（同样的密文格式/分组/排序语义）。
/// </summary>
public static class ConnectionsApi
{
    /// <summary>每请求建独立 VM+DB（TempDb 模式复刻），避免跨请求共享 Observable 状态。
    /// 单机面板低频操作，构建成本（SQLite 打开+全量加载）实测毫秒级可接受。</summary>
    private static (MainViewModel Vm, DatabaseService Db, CryptoService Crypto) CreateSession()
    {
        // 沿用 DatabaseService.Instance 单例（AppContext.BaseDirectory\data.db，与 WPF 版同库）
        var db = DatabaseService.Instance;
        db.Initialize();  // 幂等：WAL PRAGMA + CREATE TABLE IF NOT EXISTS + 迁移管线（WPF 版 App 启动时调，服务版每会话保证）
        var crypto = new CryptoService(db);
        var dialogs = new ApiDialogSink();
        var vm = new MainViewModel(db, crypto, dialogs);
        vm.ReloadConnections();
        return (vm, db, crypto);
    }

    public static void Map(WebApplication app)
    {
        // GET /api/connections → 全量列表（含在线状态字段，探活另走 /api/probe）
        app.MapGet("/api/connections", () =>
        {
            var (vm, db, _) = CreateSession();
            var items = vm.Connections.Select(c => new
            {
                c.Id, c.Host, c.Port, c.UserName, c.GroupName, c.Remark,
                c.SortOrder, c.LastConnectedAt, c.LastConnectedText,
                HasPassword = !string.IsNullOrEmpty(c.EncryptedPassword),
            }).ToList();
            return Results.Ok(new { groups = vm.GroupFilterOptions, items });
        });

        // POST /api/connections → 新建（表单字段驱动，等价 VM NewItem+SaveItem）
        app.MapPost("/api/connections", (ConnectionForm form) =>
        {
            var (vm, db, crypto) = CreateSession();
            {
                var err = Validate(form);
                if (err != null) return Results.BadRequest(new { error = err });

                var item = new ConnectionItem
                {
                    Host = form.Host.Trim(),
                    Port = form.Port,
                    UserName = form.UserName?.Trim() ?? string.Empty,
                    GroupName = form.GroupName?.Trim() ?? string.Empty,
                    Remark = form.Remark?.Trim() ?? string.Empty,
                };
                if (!string.IsNullOrEmpty(form.Password))
                {
                    item.EncryptedPassword = crypto.Encrypt(form.Password);
                }
                vm.Connections.Add(item);
                db.Update(item);
                return Results.Created($"/api/connections/{item.Id}", new { id = item.Id });
            }
        });

        // PUT /api/connections/{id} → 更新（等价 VM 选中→编辑→SaveItem）
        app.MapPut("/api/connections/{id}", (long id, ConnectionForm form) =>
        {
            var (vm, db, crypto) = CreateSession();
            {
                var err = Validate(form);
                if (err != null) return Results.BadRequest(new { error = err });

                var item = db.LoadAll().FirstOrDefault(c => c.Id == id);
                if (item == null) return Results.NotFound(new { error = "连接不存在" });

                item.Host = form.Host.Trim();
                item.Port = form.Port;
                item.UserName = form.UserName?.Trim() ?? string.Empty;
                item.GroupName = form.GroupName?.Trim() ?? string.Empty;
                item.Remark = form.Remark?.Trim() ?? string.Empty;
                // 密码字段语义对齐 WPF SaveItem（All.cs 4760 段）：空=保留原密文，非空=重加密
                if (!string.IsNullOrEmpty(form.Password))
                {
                    item.EncryptedPassword = crypto.Encrypt(form.Password);
                }
                db.Update(item);
                return Results.Ok(new { id });
            }
        });

        // DELETE /api/connections/{id}
        app.MapDelete("/api/connections/{id}", (long id) =>
        {
            var (_, db, _) = CreateSession();
            {
                var item = db.LoadAll().FirstOrDefault(c => c.Id == id);
                if (item == null) return Results.NotFound(new { error = "连接不存在" });
                db.Delete(id);
                return Results.Ok(new { id });
            }
        });

        // POST /api/connections/move → 拖拽重排（服务版 Drop 签名）
        app.MapPost("/api/connections/move", (MoveForm form) =>
        {
            var (vm, db, _) = CreateSession();
            {
                if (vm.SortByRecent) return Results.BadRequest(new { error = "按最近连接排序时禁止手工排序" });
                int count = vm.Connections.Count;
                if (form.FromIndex < 0 || form.FromIndex >= count ||
                    form.InsertIndex < 0 || form.InsertIndex > count)
                    return Results.BadRequest(new { error = "索引越界" });
                vm.Drop(form.FromIndex, form.InsertIndex);
                return Results.Ok(new { ok = true });
            }
        });
    }

    private static string? Validate(ConnectionForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Host)) return "主机地址不能为空";
        if (f.Port is < 1 or > 65535) return "端口须在 1-65535";
        return null;
    }
}

/// <summary>连接表单 DTO（API 入口校验对象；密码仅内存传递不落日志）。</summary>
public record ConnectionForm(string Host, int Port, string? UserName, string? Password,
    string? GroupName, string? Remark);

/// <summary>拖拽重排 DTO。</summary>
public record MoveForm(int FromIndex, int InsertIndex);
