namespace RemoteDesktopPlus.Models;

/// <summary>
/// 主密码对话框的用途。
/// <para>
/// 该枚举原先定义在 <c>RemoteDesktopPlus.Views</c> 命名空间，导致 ViewModel 必须引用 Views
/// 才能指定用途。这里移到 Models（无任何依赖的领域层），使 ViewModel / Services 只需依赖 Models。
/// </para>
/// </summary>
public enum MasterPasswordPurpose
{
	/// <summary>设置或修改主密码。</summary>
	Set,

	/// <summary>输入已有主密码以解锁。</summary>
	Unlock
}
