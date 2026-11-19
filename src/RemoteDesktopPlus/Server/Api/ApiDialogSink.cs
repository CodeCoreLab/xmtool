using System.Collections.Concurrent;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Server.Api;

/// <summary>
/// IDialogService 的服务端实现（清单 §1 ApiDialogSink）：弹窗意图 → 消息流。
/// WPF 版弹窗在服务版语义 = 面板消息流（M3 Web UI 轮询呈现）；表单/文件选择类
/// 交互在服务版由显式 API 参数承载，此处降级返回「取消」语义（null/false），
/// 与 Confirm 拒绝路径一致——绝不阻塞等待用户。
/// </summary>
public sealed class ApiDialogSink : IDialogService
{
    /// <summary>待呈现消息（面板拉取后清空）。</summary>
    private readonly ConcurrentQueue<string> _pending = new();

    /// <summary>导出当前待办并清空（M3 Web UI 轮询入口用）。</summary>
    public IReadOnlyList<string> DrainPending()
    {
        var list = new List<string>();
        while (_pending.TryDequeue(out var m)) list.Add(m);
        return list;
    }

    public void ShowMessage(string message, string title, DialogKind kind = DialogKind.Information)
        => _pending.Enqueue($"[{kind}] {title}: {message}");

    public bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning)
    {
        // 服务端无法阻塞等待用户确认——降级为「拒绝 + 消息流提示」，由前端二次调用完成
        _pending.Enqueue($"[CONFIRM-DENIED] {title}: {message}");
        return false;
    }

    public bool EditConnection(ConnectionItem? source, out ConnectionFormResult result)
    {
        // 服务版：新建/编辑走 POST/PUT /api/connections 显式表单，弹窗语义不适用
        result = new ConnectionFormResult();
        return false;
    }

    public bool SetLaunchPassword()
    {
        _pending.Enqueue("[INFO] 启动密码设置请走 /api/security 接口（服务版无本地启动密码对话框）");
        return false;
    }

    public void ShowSettings()
        => _pending.Enqueue("[INFO] 服务版设置由面板设置页承载（无本地窗口）");

    public bool ShowMasterPassword(MasterPasswordPurpose purpose)
    {
        _pending.Enqueue($"[INFO] 主密码{purpose}请走 /api/security/unlock 接口");
        return false;
    }

    public string? PickExportTarget(string suggestedFileName) => null;
    public string? PickRdpExportTarget(string suggestedFileName) => null;
    public string? PickImportSource() => null;

    public bool EditGroupOptions(string groupName, GroupOptionStore store,
        IReadOnlyList<string> resolutionOptions, IReadOnlyList<string> driveLetterOptions)
    {
        _pending.Enqueue($"[INFO] 分组 {groupName} 配置请走 /api/groups 接口（服务版）");
        return false;
    }
}
