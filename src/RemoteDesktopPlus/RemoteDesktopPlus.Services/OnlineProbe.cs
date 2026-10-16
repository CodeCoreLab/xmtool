using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDesktopPlus.Services;

public static class OnlineProbe
{
	public static async Task<bool> IsOnlineAsync(string host, int port, int timeoutMs = 1500, CancellationToken token = default(CancellationToken))
	{
		try
		{
			using TcpClient client = new TcpClient();
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
			cts.CancelAfter(timeoutMs);
			await client.ConnectAsync(host, port, cts.Token);
			return client.Connected;
		}
		catch
		{
			return false;
		}
	}
}
