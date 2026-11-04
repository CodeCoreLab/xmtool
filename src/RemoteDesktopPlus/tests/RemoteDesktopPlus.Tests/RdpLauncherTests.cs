using System;
using System.Collections.Generic;
using System.IO;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 记录一次 Launch 中后端被调用的全过程，用于断言：
/// 密码只经 StoreCredential 进入安全通道，绝不出现于任何进程命令行参数。
/// </summary>
internal sealed class RecordingRdpBackend : IRdpBackend
{
	public List<string> StoredCredentials { get; } = new List<string>();

	public List<string> DeletedCredentials { get; } = new List<string>();

	public List<string> StartedClients { get; } = new List<string>();

	/// <summary>真正传给进程的每个参数（等价于 ProcessStartInfo.ArgumentList 的可见内容）。</summary>
	public List<string> ProcessArguments { get; } = new List<string>();

	/// <summary>模拟凭据写入失败（用于断言调用方不会静默吞掉失败）。</summary>
	public bool StoreCredentialResult { get; set; } = true;

	public bool StoreCredential(string target, string userName, string password)
	{
		// 只记录 target，密码本身留在参数里即可被测试读取（测试需要它来断言不泄漏）
		StoredCredentials.Add(target + "|" + userName + "|" + password);
		return StoreCredentialResult;
	}

	public void DeleteCredential(string target)
	{
		DeletedCredentials.Add(target);
	}

	public void StartClient(string rdpFilePath)
	{
		StartedClients.Add(rdpFilePath);
		ProcessArguments.Add(rdpFilePath);
	}
}

/// <summary>
/// 锁死 RDP 启动契约与「密码不进命令行」这条安全性质。
/// </summary>
public class RdpLauncherTests
{
	private static RdpLauncher.RdpOptions Opt(
		string host = "10.0.0.1",
		int port = 3389,
		string user = "admin",
		string password = "S3cr3t!",
		int? w = null,
		int? h = null,
		bool forceConsole = false)
	{
		return new RdpLauncher.RdpOptions(host, port, user, password, w, h, forceConsole);
	}

	private static T WithRecordingBackend<T>(Func<RecordingRdpBackend, T> body, out RecordingRdpBackend backend)
	{
		IRdpBackend original = RdpLauncher.Backend;
		RecordingRdpBackend rec = new RecordingRdpBackend();
		backend = rec;
		RdpLauncher.Backend = rec;
		try
		{
			return body(rec);
		}
		finally
		{
			RdpLauncher.Backend = original;
		}
	}

	[Fact]
	public void BuildRdpContent_FullScreen_HasNoDesktopSize_AndMode2()
	{
		string content = RdpLauncher.BuildRdpContent(Opt());
		Assert.Contains("full address:s:10.0.0.1:3389", content);
		Assert.Contains("username:s:admin", content);
		Assert.Contains("prompt for credentials:i:0", content);
		Assert.Contains("authentication level:i:0", content);
		Assert.Contains("enablecredsspsupport:i:1", content);
		Assert.Contains("screen mode id:i:2", content);
		Assert.DoesNotContain("desktopwidth:i:", content);
		Assert.DoesNotContain("desktopheight:i:", content);
	}

	[Fact]
	public void BuildRdpContent_WithResolution_HasMode1AndDimensions()
	{
		string content = RdpLauncher.BuildRdpContent(Opt(w: 1920, h: 1080));
		Assert.Contains("screen mode id:i:1", content);
		Assert.Contains("desktopwidth:i:1920", content);
		Assert.Contains("desktopheight:i:1080", content);
		Assert.DoesNotContain("screen mode id:i:2", content);
	}

	[Theory]
	[InlineData(null, 1080)]   // 只有高 → 全屏
	[InlineData(1920, null)]   // 只有宽 → 全屏
	[InlineData(0, 1080)]      // 零宽 → 全屏
	[InlineData(1920, 0)]      // 零高 → 全屏
	[InlineData(-1, 1080)]     // 负宽 → 全屏
	[InlineData(null, null)]
	public void BuildRdpContent_PartialOrNonPositiveSize_FallsBackToFullScreen(int? w, int? h)
	{
		string content = RdpLauncher.BuildRdpContent(Opt(w: w, h: h));
		Assert.Contains("screen mode id:i:2", content);
		Assert.DoesNotContain("desktopwidth:i:", content);
	}

	[Fact]
	public void BuildRdpContent_AlwaysRedirectsAllDrives()
	{
		// v1.2.0 起磁盘共享内置恒开：恒写 `*`（动态覆盖全部驱动器，含后插 U 盘）。
		// 关键：绝对不能写成 `s:*:` —— 那是非法语法，会让整段重定向失效。
		string content = RdpLauncher.BuildRdpContent(Opt());

		Assert.Contains("drivestoredirect:s:*", content);
		Assert.DoesNotContain("drivestoredirect:s:*:", content);
	}

	[Fact]
	public void BuildRdpContent_ForceConsole_AddsConsoleLine()
	{
		Assert.Contains("connect to console:i:1", RdpLauncher.BuildRdpContent(Opt(forceConsole: true)));
		Assert.DoesNotContain("connect to console", RdpLauncher.BuildRdpContent(Opt(forceConsole: false)));
	}

	[Fact]
	public void BuildRdpContent_Clipboard_OnByDefault()
	{
		// mstsc 对 redirectclipboard 是「键缺失即按禁用」，故默认必须显式写出 i:1。
		// 这条断言防的是「以后有人把默认值改回 false 或漏写该行」导致的复制粘贴失效。
		Assert.Contains("redirectclipboard:i:1", RdpLauncher.BuildRdpContent(Opt()));
	}

	[Fact]
	public void BuildRdpContent_Clipboard_AlwaysExplicitlyEnabled()
	{
		// v1.2.0 起剪贴板共享内置恒开，且必须显式 i:1（键缺失 mstsc 按禁用处理）。
		string content = RdpLauncher.BuildRdpContent(Opt());
		Assert.DoesNotContain("redirectclipboard:i:0", content);
		Assert.Contains("redirectclipboard:i:1", content);
	}

	[Fact]
	public void BuildRdpContent_NeverContainsThePassword()
	{
		// .rdp 文件本身绝不能承载密码（历史实现也没有，这里把它固化为不变量）。
		string content = RdpLauncher.BuildRdpContent(Opt(password: "PLAINTEXT-MARKER"));
		Assert.DoesNotContain("PLAINTEXT-MARKER", content);
		Assert.DoesNotContain("password 51", content);
	}

	[Fact]
	public void WriteRdpFile_UsesUtf16_AndTempSubdirectory_AndSafeName()
	{
		string path = RdpLauncher.WriteRdpFile(Opt(host: "10.0.0.1", port: 3390));
		try
		{
			Assert.True(File.Exists(path));
			Assert.Contains("RemoteDesktopPlus", path);
			Assert.EndsWith(".rdp", path);
			Assert.Contains("10.0.0.1_3390", path);
			Assert.Equal(RdpLauncher.GetTempDirectory(), Path.GetDirectoryName(path));

			// UTF-16LE：前两字节应为 BOM FF FE，否则 mstsc 解析中文备注会出问题
			byte[] head = new byte[2];
			using (FileStream fs = File.OpenRead(path))
			{
				fs.Read(head, 0, 2);
			}
			Assert.Equal(0xFF, head[0]);
			Assert.Equal(0xFE, head[1]);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Launch_StoresCredential_AndStartsClientWithOnlyTheRdpPath()
	{
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			RdpLauncher.Launch(Opt(user: "admin", password: "S3cr3t!"));
			return 0;
		}, out rec);

		Assert.Single(rec.StoredCredentials);
		Assert.Equal("TERMSRV/10.0.0.1|admin|S3cr3t!", rec.StoredCredentials[0]);
		Assert.Single(rec.StartedClients);
	}

	[Fact]
	public void Launch_NoUserName_SkipsCredentialWrite_ButStillStartsClient()
	{
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			RdpLauncher.Launch(Opt(user: "", password: ""));
			return 0;
		}, out rec);

		Assert.Empty(rec.StoredCredentials);
		Assert.Single(rec.StartedClients);
	}

	[Fact]
	public void Launch_ProcessCommandLine_NeverContainsPassword()
	{
		// 这是本文件的核心回归：历史实现走 cmdkey /pass:<明文>，
		// 密码因此出现在进程命令行上（任务管理器可见）。修复后必须不再出现。
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			RdpLauncher.Launch(Opt(password: "PLAINTEXT-MARKER"));
			return 0;
		}, out rec);

		foreach (string arg in rec.ProcessArguments)
		{
			Assert.DoesNotContain("PLAINTEXT-MARKER", arg);
		}
		Assert.DoesNotContain("/pass:", string.Join(" ", rec.ProcessArguments));
	}

	[Fact]
	public void Launch_CredentialWriteFails_StillStartsClientWithoutThrowing()
	{
		// 凭据写入失败必须被记录（不能静默），但不应阻断连接：
		// mstsc 会退化为弹窗询问密码，用户仍可连上。
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			b.StoreCredentialResult = false;
			RdpLauncher.Launch(Opt(user: "admin", password: "S3cr3t!"));
			return 0;
		}, out rec);

		Assert.Single(rec.StoredCredentials);
		Assert.Single(rec.StartedClients);
	}

	[Fact]
	public void Backend_Setter_RejectsNull()
	{
		Assert.Throws<ArgumentNullException>(() => RdpLauncher.Backend = null);
	}

	[Fact]
	public void DefaultBackend_IsWindows_AndBuildTargetUsesTermsrvPrefix()
	{
		Assert.IsType<WindowsRdpBackend>(RdpLauncher.Backend);
		Assert.Equal("TERMSRV/10.0.0.1", WindowsRdpBackend.BuildTarget("10.0.0.1"));
	}

	[Fact]
	public void ClearCredential_DelegatesToBackend()
	{
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			RdpLauncher.ClearCredential("10.0.0.1");
			return 0;
		}, out rec);

		Assert.Equal(new[] { "TERMSRV/10.0.0.1" }, rec.DeletedCredentials);
	}

	[Fact]
	public void CleanupTempFiles_DoesNotThrow_AndKeepsForeignFiles()
	{
		// 不该抛异常；且只清 *.rdp，不误删同目录其他文件
		RdpLauncher.CleanupTempFiles();

		string dir = RdpLauncher.GetTempDirectory();
		Directory.CreateDirectory(dir);
		string keeper = Path.Combine(dir, "keep-me.txt");
		File.WriteAllText(keeper, "x");
		try
		{
			RdpLauncher.CleanupTempFiles();
			Assert.True(File.Exists(keeper));
		}
		finally
		{
			if (File.Exists(keeper))
			{
				File.Delete(keeper);
			}
		}
	}

	[Fact]
	public void GetTempDirectory_IsUnderSystemTemp()
	{
		Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), RdpLauncher.GetTempDirectory());
	}

	// ─────────────────── 参数回退（v1.2.1：.rdp 回到 v1.0.x 纯净形态） ───────────────────

	[Fact]
	public void BuildRdpContent_NeverEmitsAnyTuningKeys()
	{
		// 字体模糊排查收官：所有调优键（视觉档 + 无损效率档）全部不再写出，
		// .rdp 回到 v1.0.x 的纯净形态，画质完全由 mstsc 默认值决定。
		string content = RdpLauncher.BuildRdpContent(Opt(w: 1600, h: 900));

		Assert.DoesNotContain("session bpp", content);
		Assert.DoesNotContain("connection type", content);
		Assert.DoesNotContain("networkautodetect", content);
		Assert.DoesNotContain("bandwidthautodetect", content);
		Assert.DoesNotContain("allow desktop composition", content);
		Assert.DoesNotContain("disable wallpaper", content);
		Assert.DoesNotContain("compression", content);
		Assert.DoesNotContain("allow font smoothing", content);
		Assert.DoesNotContain("bitmapcachepersistenable", content);
		Assert.DoesNotContain("audiomode", content);
		Assert.DoesNotContain("redirectprinters", content);
		Assert.DoesNotContain("redirectsmartcards", content);
	}

	[Fact]
	public void BuildRdpContent_SharedChannels_AlwaysOn()
	{
		// 剪贴板与磁盘共享内置恒开（用户明确要求的功能，与画质无关，不随参数回退）。
		string content = RdpLauncher.BuildRdpContent(Opt());

		Assert.Contains("redirectclipboard:i:1", content);
		Assert.Contains("drivestoredirect:s:*", content);
	}

	[Fact]
	public void BuildRdpContent_NeverEmitsScalingOrWindowPlacementKeys()
	{
		// smart sizing（位图拉伸缩放）与 dynamic resolution 是 v1.1.0 引入的
		// 画质相关键，随参数回退一并移除；窗口摆放交回 mstsc 默认行为。
		string content = RdpLauncher.BuildRdpContent(Opt(w: 1280, h: 720));

		Assert.DoesNotContain("smart sizing", content);
		Assert.DoesNotContain("dynamic resolution", content);
		Assert.DoesNotContain("winposstr", content);
		Assert.Contains("screen mode id:i:1", content);
		Assert.Contains("desktopwidth:i:1280", content);
	}
}
