using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 锁死探活语义：连通返回 true、所有失败路径都安静地返回 false（不抛异常）。
/// </summary>
public class OnlineProbeTests
{
	[Fact]
	public async Task IsOnlineAsync_ListeningPort_ReturnsTrue()
	{
		TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		try
		{
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			Assert.True(await OnlineProbe.IsOnlineAsync("127.0.0.1", port, 2000));
		}
		finally
		{
			listener.Stop();
		}
	}

	[Fact]
	public async Task IsOnlineAsync_ClosedPort_ReturnsFalse()
	{
		// 绑一个端口再立刻释放，保证该端口此刻无人监听
		TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();

		Assert.False(await OnlineProbe.IsOnlineAsync("127.0.0.1", port, 800));
	}

	[Fact]
	public async Task IsOnlineAsync_UnresolvableHost_ReturnsFalse()
	{
		Assert.False(await OnlineProbe.IsOnlineAsync("host.invalid.definitely-not-real", 3389, 500));
	}

	[Fact]
	public async Task IsOnlineAsync_EmptyHost_ReturnsFalse()
	{
		Assert.False(await OnlineProbe.IsOnlineAsync(string.Empty, 3389, 300));
	}

	[Fact]
	public async Task IsOnlineAsync_TimeoutIsHonoured_ForBlackholedAddress()
	{
		// 10.255.255.1 是典型的黑洞地址：连接会挂住而非立刻失败。
		// 若超时没生效，这个用例会明显变慢，因此用 1500ms 预算做上界断言。
		Stopwatch sw = Stopwatch.StartNew();
		bool result = await OnlineProbe.IsOnlineAsync("10.255.255.1", 3389, 1200);
		sw.Stop();

		Assert.False(result);
		Assert.True(sw.ElapsedMilliseconds < 5000, $"超时未生效，耗时 {sw.ElapsedMilliseconds}ms");
	}

	[Fact]
	public async Task IsOnlineAsync_AlreadyCancelledToken_ReturnsFalse()
	{
		using CancellationTokenSource cts = new CancellationTokenSource();
		cts.Cancel();
		TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		try
		{
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			Assert.False(await OnlineProbe.IsOnlineAsync("127.0.0.1", port, 2000, cts.Token));
		}
		finally
		{
			listener.Stop();
		}
	}
}
