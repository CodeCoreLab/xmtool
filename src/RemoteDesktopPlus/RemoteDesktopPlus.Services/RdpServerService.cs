using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace RemoteDesktopPlus.Services;

public static class RdpServerService
{
	private const string RdpTcpKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp";

	private const string TerminalServerKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server";

	public const int DefaultPort = 3389;

	private const string FirewallRulePrefix = "RemoteDesktopPlus-RDP-";

	public static int GetCurrentPort()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp");
			object obj = registryKey?.GetValue("PortNumber");
			if (obj is int)
			{
				return (int)obj;
			}
		}
		catch
		{
		}
		return 3389;
	}

	public static bool IsRemoteEnabled()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server");
			if (registryKey?.GetValue("fDenyTSConnections") is int num)
			{
				return num == 0;
			}
		}
		catch
		{
		}
		return false;
	}

	public static void SetRemoteEnabled(bool enabled)
	{
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server", writable: true) ?? throw new InvalidOperationException("无法打开终端服务注册表项，请确认以管理员身份运行。");
		registryKey.SetValue("fDenyTSConnections", (!enabled) ? 1 : 0, RegistryValueKind.DWord);
	}

	public static void SetPort(int port)
	{
		if ((port < 1 || port > 65535) ? true : false)
		{
			throw new ArgumentOutOfRangeException("port", "端口范围必须在 1 到 65535 之间。");
		}
		int currentPort = GetCurrentPort();
		using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp", writable: true) ?? throw new InvalidOperationException("无法打开 RDP-Tcp 注册表项，请确认以管理员身份运行。"))
		{
			registryKey.SetValue("PortNumber", port, RegistryValueKind.DWord);
		}
		RemoveOurFirewallRules(port);
		if (currentPort != port)
		{
			RemoveFirewallRuleForPort(currentPort);
		}
		AddFirewallRule(port);
	}

	public static bool IsPortListening(int port)
	{
		try
		{
			IPEndPoint[] activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
			for (int i = 0; i < activeTcpListeners.Length; i++)
			{
				if (activeTcpListeners[i].Port == port)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void AddFirewallRule(int port)
	{
		string text = $"{"RemoteDesktopPlus-RDP-"}{port}";
		RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text);
		RunNetsh("advfirewall", "firewall", "add", "rule", "name=" + text, "dir=in", "action=allow", "protocol=TCP", $"localport={port}");
	}

	private static void RemoveFirewallRuleForPort(int port)
	{
		string text = $"{"RemoteDesktopPlus-RDP-"}{port}";
		RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text);
	}

	private static void RemoveOurFirewallRules(int exceptPort)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				ArgumentList = 
				{
					"-NoProfile",
					"-Command",
					$"Get-NetFirewallRule -DisplayName '{"RemoteDesktopPlus-RDP-"}*' -ErrorAction SilentlyContinue | Where-Object {{ $_.DisplayName -ne '{"RemoteDesktopPlus-RDP-"}{exceptPort}' }} | " + "ForEach-Object { $_.DisplayName }"
				}
			});
			if (process == null)
			{
				return;
			}
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit(3000);
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (!string.IsNullOrEmpty(text2) && text2.StartsWith("RemoteDesktopPlus-RDP-", StringComparison.OrdinalIgnoreCase))
				{
					RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text2);
				}
			}
		}
		catch
		{
		}
	}

	private static void RunNetsh(params string[] args)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = "netsh.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		foreach (string item in args)
		{
			processStartInfo.ArgumentList.Add(item);
		}
		try
		{
			using Process process = Process.Start(processStartInfo);
			process?.WaitForExit(5000);
		}
		catch
		{
		}
	}
}
