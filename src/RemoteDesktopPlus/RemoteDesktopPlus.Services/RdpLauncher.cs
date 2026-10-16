using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDesktopPlus.Services;

public static class RdpLauncher
{
	public sealed record RdpOptions(string Host, int Port, string UserName, string Password, int? Width, int? Height, bool RedirectLocalDrive, string LocalDriveLetter, bool ForceConsole);

	public static void Launch(RdpOptions opt)
	{
		RdpOptions opt2 = opt;
		string target = "TERMSRV/" + opt2.Host;
		bool hasCredential = !string.IsNullOrEmpty(opt2.UserName);
		if (hasCredential)
		{
			RunCmdkeyAdd(target, opt2.UserName, opt2.Password);
		}
		string rdpPath = WriteRdpFile(opt2);
		Process.Start(new ProcessStartInfo
		{
			FileName = "mstsc.exe",
			UseShellExecute = true,
			ArgumentList = { rdpPath }
		});
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(5000).ConfigureAwait(continueOnCapturedContext: false);
				try
				{
					if (File.Exists(rdpPath))
					{
						File.Delete(rdpPath);
					}
				}
				catch
				{
				}
				if (hasCredential)
				{
					try
					{
						ClearCredential(opt2.Host);
						return;
					}
					catch
					{
						return;
					}
				}
			}
			catch
			{
			}
		});
	}

	public static void ClearCredential(string host)
	{
		string text = "TERMSRV/" + host;
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = "cmdkey.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		processStartInfo.ArgumentList.Add("/delete:" + text);
		try
		{
			using Process process = Process.Start(processStartInfo);
			process?.WaitForExit(3000);
		}
		catch
		{
		}
	}

	public static void CleanupTempFiles()
	{
		try
		{
			string path = Path.Combine(Path.GetTempPath(), "RemoteDesktopPlus");
			if (!Directory.Exists(path))
			{
				return;
			}
			foreach (string item in Directory.EnumerateFiles(path, "*.rdp"))
			{
				try
				{
					File.Delete(item);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static void RunCmdkeyAdd(string target, string user, string password)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = "cmdkey.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		processStartInfo.ArgumentList.Add("/generic:" + target);
		processStartInfo.ArgumentList.Add("/user:" + user);
		processStartInfo.ArgumentList.Add("/pass:" + password);
		try
		{
			using Process process = Process.Start(processStartInfo);
			process?.WaitForExit(3000);
		}
		catch
		{
		}
	}

	private static string WriteRdpFile(RdpOptions opt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 2, stringBuilder2);
		handler.AppendLiteral("full address:s:");
		handler.AppendFormatted(opt.Host);
		handler.AppendLiteral(":");
		handler.AppendFormatted(opt.Port);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
		handler.AppendLiteral("username:s:");
		handler.AppendFormatted(opt.UserName);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder.AppendLine("prompt for credentials:i:0");
		stringBuilder.AppendLine("authentication level:i:0");
		stringBuilder.AppendLine("enablecredsspsupport:i:1");
		int? width = opt.Width;
		if (width.HasValue && width.GetValueOrDefault() > 0)
		{
			width = opt.Height;
			if (width.HasValue && width.GetValueOrDefault() > 0)
			{
				stringBuilder.AppendLine("screen mode id:i:1");
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
				handler.AppendLiteral("desktopwidth:i:");
				handler.AppendFormatted(opt.Width);
				stringBuilder5.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
				handler.AppendLiteral("desktopheight:i:");
				handler.AppendFormatted(opt.Height);
				stringBuilder6.AppendLine(ref handler);
				goto IL_0155;
			}
		}
		stringBuilder.AppendLine("screen mode id:i:2");
		goto IL_0155;
		IL_0155:
		if (opt.RedirectLocalDrive && !string.IsNullOrEmpty(opt.LocalDriveLetter))
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
			handler.AppendLiteral("drivestoredirect:s:");
			handler.AppendFormatted(opt.LocalDriveLetter);
			handler.AppendLiteral(":");
			stringBuilder7.AppendLine(ref handler);
		}
		if (opt.ForceConsole)
		{
			stringBuilder.AppendLine("connect to console:i:1");
		}
		string text = Path.Combine(Path.GetTempPath(), "RemoteDesktopPlus");
		Directory.CreateDirectory(text);
		string value = string.Join("_", opt.Host.Split(Path.GetInvalidFileNameChars()));
		string text2 = Path.Combine(text, $"{value}_{opt.Port}.rdp");
		File.WriteAllText(text2, stringBuilder.ToString(), Encoding.Unicode);
		return text2;
	}
}
