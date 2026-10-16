using System.Diagnostics;

namespace RemoteDesktopPlus.Services;

/// <summary>
/// Windows 平台的 RDP 后端：凭据走 <see cref="CredentialStore"/>（CredWriteW），
/// 客户端走 mstsc.exe。
/// 关键安全性质：本类所有进程启动的参数面都**不含密码**——
/// 密码只经 CredentialStore 写入系统凭据存储。
/// </summary>
public sealed class WindowsRdpBackend : IRdpBackend
{
	public static WindowsRdpBackend Instance { get; } = new WindowsRdpBackend();

	/// <summary>cmdkey 的 target 命名约定：TERMSRV/&lt;host&gt;，mstsc 只认这个前缀。</summary>
	public static string BuildTarget(string host)
	{
		return "TERMSRV/" + host;
	}

	public void StoreCredential(string target, string userName, string password)
	{
		CredentialStore.WriteGeneric(target, userName, password);
	}

	public void DeleteCredential(string target)
	{
		CredentialStore.DeleteGeneric(target);
	}

	public void StartClient(string rdpFilePath)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = "mstsc.exe",
			UseShellExecute = true,
			ArgumentList = { rdpFilePath }
		});
	}
}
