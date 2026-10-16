using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDesktopPlus.Services;

/// <summary>
/// 生成 .rdp 文件并拉起远程桌面客户端。
/// <para>
/// 安全要点（原实现缺陷已修）：密码**不再**经 <c>cmdkey.exe /pass:&lt;明文&gt;</c> 传入，
/// 而是通过 <see cref="IRdpBackend.StoreCredential"/> 写入系统凭据存储。
/// 原因是命令行参数对任何进程可见（Win32_Process.CommandLine / 任务管理器），
/// 而 CredWriteW 的 CredentialBlob 不进进程命令行。
/// </para>
/// </summary>
public static class RdpLauncher
{
	public sealed record RdpOptions(string Host, int Port, string UserName, string Password, int? Width, int? Height, bool RedirectLocalDrive, string LocalDriveLetter, bool ForceConsole);

	private static IRdpBackend _backend = WindowsRdpBackend.Instance;

	/// <summary>可替换后端（测试注入用）。生产默认走 Windows 实现。</summary>
	public static IRdpBackend Backend
	{
		get
		{
			return _backend;
		}
		set
		{
			_backend = value ?? throw new ArgumentNullException(nameof(value));
		}
	}

	public static void Launch(RdpOptions opt)
	{
		string target = WindowsRdpBackend.BuildTarget(opt.Host);
		bool hasCredential = !string.IsNullOrEmpty(opt.UserName);
		if (hasCredential)
		{
			_backend.StoreCredential(target, opt.UserName, opt.Password);
		}

		string rdpPath = WriteRdpFile(opt);
		_backend.StartClient(rdpPath);

		// mstsc 读取 .rdp 与凭据是异步的，故延迟清理临时文件与一次性凭据。
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(5000).ConfigureAwait(continueOnCapturedContext: false);
				TryDelete(rdpPath);
				if (hasCredential)
				{
					TryDeleteCredential(opt.Host);
				}
			}
			catch
			{
			}
		});
	}

	public static void ClearCredential(string host)
	{
		TryDeleteCredential(host);
	}

	public static void CleanupTempFiles()
	{
		try
		{
			string dir = GetTempDirectory();
			if (!Directory.Exists(dir))
			{
				return;
			}
			foreach (string file in Directory.EnumerateFiles(dir, "*.rdp"))
			{
				TryDelete(file);
			}
		}
		catch
		{
		}
	}

	/// <summary>临时 .rdp 所在目录：%TEMP%\RemoteDesktopPlus。</summary>
	public static string GetTempDirectory()
	{
		return Path.Combine(Path.GetTempPath(), "RemoteDesktopPlus");
	}

	/// <summary>
	/// 生成 .rdp 文件内容。公开以便单元测试直接断言各分支渲染结果，无需落盘。
	/// </summary>
	public static string BuildRdpContent(RdpOptions opt)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"full address:s:{opt.Host}:{opt.Port}");
		sb.AppendLine($"username:s:{opt.UserName}");
		sb.AppendLine("prompt for credentials:i:0");
		sb.AppendLine("authentication level:i:0");
		sb.AppendLine("enablecredsspsupport:i:1");

		bool hasSize = opt.Width.HasValue && opt.Width.GetValueOrDefault() > 0
			&& opt.Height.HasValue && opt.Height.GetValueOrDefault() > 0;
		if (hasSize)
		{
			sb.AppendLine("screen mode id:i:1");
			sb.AppendLine($"desktopwidth:i:{opt.Width.GetValueOrDefault()}");
			sb.AppendLine($"desktopheight:i:{opt.Height.GetValueOrDefault()}");
		}
		else
		{
			sb.AppendLine("screen mode id:i:2");
		}

		if (opt.RedirectLocalDrive && !string.IsNullOrEmpty(opt.LocalDriveLetter))
		{
			sb.AppendLine($"drivestoredirect:s:{opt.LocalDriveLetter}:");
		}
		if (opt.ForceConsole)
		{
			sb.AppendLine("connect to console:i:1");
		}
		return sb.ToString();
	}

	/// <summary>生成并落盘 .rdp 文件（UTF-16LE），返回绝对路径。</summary>
	public static string WriteRdpFile(RdpOptions opt)
	{
		string dir = GetTempDirectory();
		Directory.CreateDirectory(dir);
		string safeHost = string.Join("_", opt.Host.Split(Path.GetInvalidFileNameChars()));
		string path = Path.Combine(dir, $"{safeHost}_{opt.Port}.rdp");
		File.WriteAllText(path, BuildRdpContent(opt), Encoding.Unicode);
		return path;
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static void TryDeleteCredential(string host)
	{
		try
		{
			_backend.DeleteCredential(WindowsRdpBackend.BuildTarget(host));
		}
		catch
		{
		}
	}
}
