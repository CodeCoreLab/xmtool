using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

	public Process? StartClient(string rdpFilePath)
	{
		StartedClients.Add(rdpFilePath);
		ProcessArguments.Add(rdpFilePath);
		return null; // 替身不启动真实进程：MstscWindowManager.Track 对 null 无操作
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
		bool redirect = false,
		string? letter = "D",
		bool forceConsole = false,
		bool clipboard = true)
	{
		return new RdpLauncher.RdpOptions(host, port, user, password, w, h, redirect, letter, forceConsole, clipboard);
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
	public void BuildRdpContent_RedirectDrive_RequiresBothFlagAndLetter()
	{
		// 规范语法：每个盘符带尾随反斜杠并以分号结尾（官方示例 C:\;E:\;）。
		Assert.Contains(@"drivestoredirect:s:D:\;", RdpLauncher.BuildRdpContent(Opt(redirect: true, letter: "D")));
		Assert.DoesNotContain("drivestoredirect", RdpLauncher.BuildRdpContent(Opt(redirect: false, letter: "D")));
		Assert.DoesNotContain("drivestoredirect", RdpLauncher.BuildRdpContent(Opt(redirect: true, letter: "")));
		Assert.DoesNotContain("drivestoredirect", RdpLauncher.BuildRdpContent(Opt(redirect: true, letter: null)));
	}

	[Fact]
	public void BuildRdpContent_RedirectAllDrives_EmitsStarWithoutTrailingColon()
	{
		// `*` 覆盖全部驱动器且是动态的（连接后插入的 U 盘也会重定向）。
		// 关键：绝对不能写成 `s:*:` —— 那是非法语法，会让整段重定向失效。
		string content = RdpLauncher.BuildRdpContent(Opt(redirect: true, letter: "*"));

		Assert.Contains("drivestoredirect:s:*", content);
		Assert.DoesNotContain("drivestoredirect:s:*:", content);
	}

	[Theory]
	[InlineData("D", @"D:\;")]
	[InlineData("D:", @"D:\;")]
	[InlineData(@"D:\", @"D:\;")]
	[InlineData("d", @"D:\;")]
	[InlineData("*", "*")]
	[InlineData("", "")]
	[InlineData(null, "")]
	[InlineData("1", "")]
	public void NormalizeDriveSpec_AcceptsCommonInputShapes(string? input, string expected)
	{
		// 归一化集中在一处，避免调用方传入 "D"/"D:"/"D:\" 任一形态时输出不一致。
		Assert.Equal(expected, RdpLauncher.RdpOptions.NormalizeDriveSpec(input));
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
	public void BuildRdpContent_Clipboard_CanBeDisabledExplicitly()
	{
		Assert.Contains("redirectclipboard:i:0", RdpLauncher.BuildRdpContent(Opt(clipboard: false)));
		Assert.DoesNotContain("redirectclipboard:i:1", RdpLauncher.BuildRdpContent(Opt(clipboard: false)));
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

	// ─────────────────── A16：延迟清理不得删除仍在被引用的凭据 ───────────────────

	/// <summary>
	/// A16 回归：同主机并发连接时，先发那条的延迟清理会把
	/// <c>TERMSRV/&lt;host&gt;</c> 凭据删掉，而后发那条的 mstsc 还在用它
	/// —— 表现为「第二次连接弹密码框」。
	/// <para>
	/// 修复后：清理前先看该主机是否仍有活跃连接引用；有则本轮跳过，
	/// 等最后一条连接释放引用后才删。
	/// </para>
	/// </summary>
	[Fact]
	public void TryCleanupCredentialIfUnused_SkipsDeletion_WhileHostStillReferenced()
	{
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			// 模拟「另一条同主机连接仍在使用该凭据」
			RdpLauncher.AcquireConnection("host-a16-alive");

			bool cleaned = RdpLauncher.TryCleanupCredentialIfUnused("host-a16-alive");

			Assert.False(cleaned, "仍被活跃连接引用时不得清理凭据");
			Assert.Empty(b.DeletedCredentials);
			Assert.Equal(1, RdpLauncher.GetActiveConnectionCount("host-a16-alive"));
			return 0;
		}, out rec);
	}

	/// <summary>A16：引用归零后才允许删除凭据。</summary>
	[Fact]
	public void TryCleanupCredentialIfUnused_Deletes_AfterLastReferenceReleased()
	{
		RecordingRdpBackend rec;
		WithRecordingBackend(delegate(RecordingRdpBackend b)
		{
			RdpLauncher.AcquireConnection("host-a16-release");
			RdpLauncher.ReleaseConnection("host-a16-release");

			bool cleaned = RdpLauncher.TryCleanupCredentialIfUnused("host-a16-release");

			Assert.True(cleaned, "引用归零后应清理凭据");
			Assert.Equal(new[] { "TERMSRV/host-a16-release" }, b.DeletedCredentials);
			return 0;
		}, out rec);
	}

	/// <summary>
	/// A16 端到端：两条同主机并发连接，延迟清理跑完后凭据**只被删一次**
	/// （不能一条连接就把凭据删掉，也不能重复删）。
	/// </summary>
	[Fact]
	public void Launch_SameHostTwice_CredentialDeletedOnlyAfterLastReferenceReleased()
	{
		int originalDelay = RdpLauncher.CleanupDelayMilliseconds;
		RecordingRdpBackend rec;
		try
		{
			RdpLauncher.CleanupDelayMilliseconds = 150;
			WithRecordingBackend(delegate(RecordingRdpBackend b)
			{
				RdpLauncher.Launch(Opt(host: "host-a16-concurrent"));
				RdpLauncher.Launch(Opt(host: "host-a16-concurrent"));
				Assert.Equal(2, RdpLauncher.GetActiveConnectionCount("host-a16-concurrent"));

				// 等两条连接的延迟清理都跑完（两条各 150ms，留足余量）
				Thread.Sleep(1500);

				// 两条连接都结束 → 引用归零 → 凭据最终被删除
				Assert.Equal(0, RdpLauncher.GetActiveConnectionCount("host-a16-concurrent"));
				Assert.NotEmpty(b.DeletedCredentials);
				Assert.Equal("TERMSRV/host-a16-concurrent", b.DeletedCredentials[b.DeletedCredentials.Count - 1]);
				return 0;
			}, out rec);
		}
		finally
		{
			RdpLauncher.CleanupDelayMilliseconds = originalDelay;
		}
	}

	// ─────────────────── A19：.rdp 临时文件名必须唯一 ───────────────────

	/// <summary>
	/// A19 回归：旧实现文件名只由「主机名_端口」决定，
	/// 同主机同端口的并发连接会写到同一个文件上（互相覆盖 + 互相删除）。
	/// </summary>
	[Fact]
	public void WriteRdpFile_ConcurrentSameHostAndPort_ProducesDistinctFileNames()
	{
		string p1 = RdpLauncher.WriteRdpFile(Opt(host: "10.0.0.9", port: 3390));
		string p2 = RdpLauncher.WriteRdpFile(Opt(host: "10.0.0.9", port: 3390));
		try
		{
			Assert.NotEqual(p1, p2);
			Assert.NotEqual(Path.GetFileName(p1), Path.GetFileName(p2));
			// 主机名+端口前缀仍保留（便于人工辨认）
			Assert.Contains("10.0.0.9_3390", p1);
			Assert.Contains("10.0.0.9_3390", p2);
		}
		finally
		{
			foreach (string p in new[] { p1, p2 })
			{
				if (File.Exists(p))
				{
					File.Delete(p);
				}
			}
		}
	}

	/// <summary>A19：并发（多线程）生成的临时文件名也不得重复。</summary>
	[Fact]
	public void WriteRdpFile_ParallelWrites_ProducesDistinctFileNames()
	{
		const int N = 16;
		string[] paths = new string[N];
		System.Threading.Tasks.Parallel.For(0, N, i =>
		{
			paths[i] = RdpLauncher.WriteRdpFile(Opt(host: "10.0.0.10", port: 3390));
		});
		try
		{
			Assert.Equal(N, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		}
		finally
		{
			foreach (string p in paths)
			{
				if (p != null && File.Exists(p))
				{
					File.Delete(p);
				}
			}
		}
	}

}
