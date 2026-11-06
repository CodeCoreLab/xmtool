using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// A42 回归：工具脚本不得硬编码本机绝对路径。
/// <para>
/// 缺陷现象：tools/ 下的脚本写死了 <c>E:\xmtool\xmtool\...</c> 与
/// <c>C:/Program Files/Google/Chrome/Application/chrome.exe</c>，
/// 换一台机器 / 换一个 checkout 目录就直接 FileNotFound。
/// </para>
/// <para>
/// 修复方式：仓库内资源改为**相对脚本自身位置**解析
/// （Python 用 <c>__file__</c>、JS 用 <c>__dirname</c>）；
/// 浏览器可执行文件改为「环境变量优先 + 常见安装位置探测」。
/// </para>
/// </summary>
public class ToolScriptPathTests
{
	/// <summary>tools/ 目录（相对本测试工程向上两级）。</summary>
	private static string ToolsDir()
	{
		string testDir = AppContext.BaseDirectory;
		// .../src/RemoteDesktopPlus/tests/RemoteDesktopPlus.Tests/bin/<cfg>/net10.0-windows
		DirectoryInfo? dir = new DirectoryInfo(testDir);
		for (int i = 0; i < 8 && dir != null; i++)
		{
			string candidate = Path.Combine(dir.FullName, "tools");
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
			dir = dir.Parent;
		}
		throw new DirectoryNotFoundException("找不到 tools/ 目录（从 " + testDir + " 向上查找失败）");
	}

	/// <summary>仓库根（tools/ 的上一级）。</summary>
	private static string RepoRoot()
	{
		return Directory.GetParent(ToolsDir())!.FullName;
	}

	private static string[] ScriptFiles(string extension)
	{
		return Directory.GetFiles(ToolsDir(), "*" + extension, SearchOption.TopDirectoryOnly);
	}

	/// <summary>
	/// 静态验证：脚本里不得再出现写死的**本机仓库绝对路径**。
	/// <para>
	/// 覆盖两类：旧仓库根 <c>E:\xmtool</c>（含 <c>E:/xmtool</c> 写法）。
	/// </para>
	/// <para>
	/// 只检查**生效代码**：注释行（Python <c>#</c>、JS <c>//</c>）里为了说明
	/// 历史缺陷而出现的旧路径字面量不算硬编码，故先剔除注释再匹配。
	/// </para>
	/// </summary>
	[Fact]
	public void ToolScripts_ContainNoHardcodedLocalAbsolutePaths()
	{
		string[] patterns =
		{
			@"E:\xmtool",          // 旧仓库根
			@"E:/xmtool",
		};
		var offenders = new System.Collections.Generic.List<string>();
		foreach (string file in ScriptFiles("*.py").Concat(ScriptFiles("*.js")).Concat(ScriptFiles("*.sh")))
		{
			string text = StripComments(File.ReadAllText(file));
			foreach (string pattern in patterns)
			{
				if (text.Contains(pattern))
				{
					offenders.Add(Path.GetFileName(file) + " ← " + pattern);
				}
			}
		}
		Assert.True(offenders.Count == 0, "工具脚本仍含硬编码本机绝对路径: " + string.Join("; ", offenders));
	}

	/// <summary>
	/// 静态验证：JS 脚本不得把浏览器可执行文件写成硬编码字面量。
	/// <para>
	/// 旧实现是 <c>executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe'</c>，
	/// 换机器就 FileNotFound。修复后统一走 <c>resolveChromePath()</c>（环境变量优先 + 常见位置探测），
	/// 因此生效代码里不允许再出现 <c>executablePath</c> 后面跟绝对路径字面量。
	/// </para>
	/// </summary>
	[Fact]
	public void JsScripts_DoNotHardcodeBrowserExecutablePath()
	{
		var offenders = new System.Collections.Generic.List<string>();
		foreach (string file in ScriptFiles("*.js"))
		{
			string text = StripComments(File.ReadAllText(file));
			foreach (string line in text.Split('\n'))
			{
				string trimmed = line.Trim();
				if (!trimmed.StartsWith("executablePath", StringComparison.Ordinal))
				{
					continue;
				}
				// 允许 resolveChromePath() / process.env[...] 之类；禁止再写死绝对路径字面量
				if (trimmed.Contains("'") || trimmed.Contains("\""))
				{
					string quoted = trimmed.Substring(trimmed.IndexOf('=') + 1).Trim();
					if (quoted.Contains("resolveChromePath") || quoted.Contains("process.env"))
					{
						continue;
					}
					offenders.Add(Path.GetFileName(file) + " ← " + trimmed);
				}
			}
		}
		Assert.True(offenders.Count == 0, "JS 脚本仍写死了浏览器可执行文件路径: " + string.Join("; ", offenders));
	}

	/// <summary>剔除注释行，只保留可能生效的代码。</summary>
	private static string StripComments(string text)
	{
		var kept = new System.Collections.Generic.List<string>();
		foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
		{
			string trimmed = line.TrimStart();
			if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
			{
				continue;
			}
			kept.Add(line);
		}
		return string.Join("\n", kept);
	}

	/// <summary>
	/// 静态验证：定位仓库资源的脚本必须用**脚本自身位置**解析路径。
	/// </summary>
	[Fact]
	public void RepoResourceScripts_ResolvePathsRelativeToScriptLocation()
	{
		// 这些脚本需要定位仓库内资源（docs/、src/），必须基于 __file__ 解析
		string[] needScriptRelative = { "check_html.py", "merge_to_single_file.py", "audit_personal_info.py", "enqueue_product_page.py", "capture_app.py" };
		foreach (string name in needScriptRelative)
		{
			string path = Path.Combine(ToolsDir(), name);
			Assert.True(File.Exists(path), "缺少脚本: " + name);
			string text = File.ReadAllText(path);
			Assert.True(
				text.Contains("__file__"),
				name + " 必须用 __file__ 相对定位仓库资源（不得依赖当前工作目录）");
		}
	}

	/// <summary>
	/// 动态验证：从**不同工作目录**调用脚本，仍能定位到仓库资源。
	/// <para>
	/// 用 check_html.py 做探针：它读 docs/ 下的产品页 HTML 并打印统计，
	/// 路径解析失败会直接抛 FileNotFoundError（非零退出码）。
	/// </para>
	/// </summary>
	[Fact]
	public void CheckHtmlScript_RunsSuccessfully_FromArbitraryWorkingDirectory()
	{
		string python = FindPython();
		Assert.True(python != null, "未找到 python 解释器，无法执行动态验证");

		string script = Path.Combine(ToolsDir(), "check_html.py");
		Assert.True(File.Exists(script), "缺少脚本: check_html.py");

		// 从两个与仓库无关的工作目录调用，结果必须一致且都成功
		string cwd1 = Path.Combine(Path.GetTempPath(), "xmtool-a42-cwd-1");
		string cwd2 = Path.Combine(Path.GetTempPath(), "xmtool-a42-cwd-2");
		Directory.CreateDirectory(cwd1);
		Directory.CreateDirectory(cwd2);
		try
		{
			string out1 = RunPython(python, script, cwd1);
			string out2 = RunPython(python, script, cwd2);

			Assert.Contains("正确的 --i 值数量", out1);
			Assert.Contains("正确的 --i 值数量", out2);
			// 两个工作目录的输出必须一致 → 证明路径解析与 cwd 无关
			Assert.Equal(out1, out2);
		}
		finally
		{
			TryDeleteDir(cwd1);
			TryDeleteDir(cwd2);
		}
	}

	/// <summary>
	/// 动态验证：merge_to_single_file.py 的仓库根解析不依赖 cwd。
	/// <para>
	/// 不真的跑合并（会改源码），而是让脚本自己把解析出的路径打印出来 ——
	/// 通过 env 开关切换到「只解析不写」模式。
	/// </para>
	/// </summary>
	[Fact]
	public void MergeScript_ResolvesRepoRoot_IndependentOfWorkingDirectory()
	{
		string python = FindPython();
		Assert.True(python != null, "未找到 python 解释器，无法执行动态验证");

		string script = Path.Combine(ToolsDir(), "merge_to_single_file.py");
		string cwd = Path.Combine(Path.GetTempPath(), "xmtool-a42-cwd-merge");
		Directory.CreateDirectory(cwd);
		try
		{
			// 只解析路径不写文件（脚本新增的 dry-run 开关）
			Environment.SetEnvironmentVariable("XMTOOL_MERGE_DRYRUN", "1");
			try
			{
				string output = RunPython(python, script, cwd);
				Assert.Contains("REPO_ROOT=", output);
				Assert.Contains("RemoteDesktopPlus", output);
			}
			finally
			{
				Environment.SetEnvironmentVariable("XMTOOL_MERGE_DRYRUN", null);
			}
		}
		finally
		{
			TryDeleteDir(cwd);
		}
	}

	private static string RunPython(string python, string script, string cwd)
	{
		ProcessStartInfo psi = new ProcessStartInfo
		{
			FileName = python,
			ArgumentList = { script },
			WorkingDirectory = cwd,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardOutputEncoding = System.Text.Encoding.UTF8,
			StandardErrorEncoding = System.Text.Encoding.UTF8,
		};
		// Windows 下 Python 默认按 cp936 输出中文，这里强制 UTF-8，避免断言看到乱码
		psi.Environment["PYTHONIOENCODING"] = "utf-8";
		psi.Environment["PYTHONUTF8"] = "1";
		using Process proc = Process.Start(psi)!;
		string stdout = proc.StandardOutput.ReadToEnd();
		string stderr = proc.StandardError.ReadToEnd();
		proc.WaitForExit(120000);
		Assert.True(proc.ExitCode == 0, "脚本退出码 " + proc.ExitCode + "（cwd=" + cwd + "）\nstdout:\n" + stdout + "\nstderr:\n" + stderr);
		return stdout;
	}

	private static string? FindPython()
	{
		foreach (string name in new[] { "python", "python3", "py" })
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = name,
					ArgumentList = { "--version" },
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using Process proc = Process.Start(psi);
				if (proc == null)
				{
					continue;
				}
				proc.StandardOutput.ReadToEnd();
				proc.StandardError.ReadToEnd();
				proc.WaitForExit(30000);
				if (proc.ExitCode == 0)
				{
					return name;
				}
			}
			catch
			{
				// 该名字不可用 → 试下一个
			}
		}
		return null;
	}

	private static void TryDeleteDir(string dir)
	{
		try
		{
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, recursive: true);
			}
		}
		catch
		{
			// 清理失败不影响测试结论
		}
	}
}
