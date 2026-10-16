using RemoteDesktopPlus.Models;

namespace RemoteDesktopPlus.Services;

/// <summary>弹窗的语义分类，避免把 WPF 的 MessageBoxImage 类型泄漏进 ViewModel。</summary>
public enum DialogKind
{
	/// <summary>普通提示。</summary>
	Information,

	/// <summary>警告 / 需要用户特别留意的确认。</summary>
	Warning,

	/// <summary>操作失败。</summary>
	Error
}

/// <summary>
/// ViewModel 面向用户的交互出口。
/// <para>
/// 引入它的唯一目的：切断 <c>MainViewModel → RemoteDesktopPlus.Views</c> 的直接依赖。
/// 视图模型只表达「需要弹一个错误」「需要用户填一份连接表单」「需要用户选一个文件」，
/// 至于用哪个窗口、哪个控件实现，由 <c>WpfDialogService</c> 在视图层决定。
/// </para>
/// <para>
/// 该接口只出现 Models 里的 DTO 与自身枚举，因此 ViewModel 与测试替身都不需要引用 WPF 控件。
/// </para>
/// </summary>
public interface IDialogService
{
	/// <summary>提示 / 警告 / 错误提示框（仅一个确定按钮）。</summary>
	void ShowMessage(string message, string title, DialogKind kind = DialogKind.Information);

	/// <summary>是 / 否确认框，返回用户是否选择「是」。</summary>
	bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning);

	/// <summary>
	/// 打开「添加 / 编辑服务器」对话框。
	/// </summary>
	/// <param name="source">要编辑的既有连接；为 null 表示新建。</param>
	/// <param name="result">用户确认后的表单内容；取消时无意义。</param>
	/// <returns>用户是否点了确定。</returns>
	bool EditConnection(ConnectionItem? source, out ConnectionFormResult result);

	/// <summary>打开「设置启动密码」对话框。</summary>
	/// <returns>用户是否点了确定。</returns>
	bool SetLaunchPassword();

	/// <summary>打开主密码对话框（设置或解锁，由 <paramref name="purpose"/> 决定）。</summary>
	/// <returns>用户是否点了确定。</returns>
	bool ShowMasterPassword(MasterPasswordPurpose purpose);

	/// <summary>选择导出目标文件；用户取消返回 null。</summary>
	string? PickExportTarget(string suggestedFileName);

	/// <summary>选择导入来源文件；用户取消返回 null。</summary>
	string? PickImportSource();
}
