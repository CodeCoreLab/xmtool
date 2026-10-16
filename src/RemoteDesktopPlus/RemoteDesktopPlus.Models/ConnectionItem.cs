using CommunityToolkit.Mvvm.ComponentModel;

namespace RemoteDesktopPlus.Models;

/// <summary>
/// 服务器列表中的一条连接记录。
/// <para>
/// 属性由 CommunityToolkit.Mvvm 的 <c>[ObservableProperty]</c> 源生成器生成
/// （见 analyzers\CommunityToolkit.Mvvm.SourceGenerators.dll）。
/// 反编译版本里那些带 <c>[GeneratedCode]</c> 特性的手写属性是生成器产物，
/// 若保留它们会与生成器冲突（CS0102 重复定义），故一律删除，只留字段声明。
/// </para>
/// </summary>
public partial class ConnectionItem : ObservableObject
{
	[ObservableProperty]
	private int index;

	[ObservableProperty]
	private string host = string.Empty;

	[ObservableProperty]
	private int port = 3389;

	[ObservableProperty]
	private string userName = string.Empty;

	[ObservableProperty]
	private string remark = string.Empty;

	[ObservableProperty]
	private OnlineStatus status;

	public long Id { get; set; }

	public string EncryptedPassword { get; set; } = string.Empty;

	public int SortOrder { get; set; }
}
