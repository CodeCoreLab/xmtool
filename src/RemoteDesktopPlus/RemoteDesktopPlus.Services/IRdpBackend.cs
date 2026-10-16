namespace RemoteDesktopPlus.Services;

/// <summary>
/// 与 RDP 客户端进程及其凭据存储交互的抽象层。
/// 存在的意义有二：一是把 mstsc.exe / 凭据写入隔离开，便于换后端；
/// 二是让测试能断言「密码只进安全通道，绝不进进程命令行」——
/// 见 <see cref="WindowsRdpBackend.StartClient"/> 只接受 .rdp 文件路径，参数面天然不含密码。
/// </summary>
public interface IRdpBackend
{
	/// <summary>把凭据写入系统凭据存储（等价于 cmdkey /generic，但不经命令行）。</summary>
	void StoreCredential(string target, string userName, string password);

	/// <summary>从系统凭据存储删除指定 target 的凭据。</summary>
	void DeleteCredential(string target);

	/// <summary>以给定的 .rdp 文件启动远程桌面客户端。</summary>
	void StartClient(string rdpFilePath);
}
