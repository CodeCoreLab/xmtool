using System;
using System.IO;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 每个用例一个一次性临时目录，用于承载 data.db。
/// 之所以需要它：<see cref="RemoteDesktopPlus.Services.DatabaseService"/> 的生产单例把库钉在
/// AppContext.BaseDirectory，测试必须改走可注入构造，否则会污染真实用户数据。
/// </summary>
public sealed class TempDb : IDisposable
{
	private readonly string _dir;

	public string DbPath { get; }

	public TempDb()
	{
		_dir = Path.Combine(Path.GetTempPath(), "rdpp_tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_dir);
		DbPath = Path.Combine(_dir, "data.db");
	}

	public RemoteDesktopPlus.Services.DatabaseService NewService()
	{
		RemoteDesktopPlus.Services.DatabaseService service = new RemoteDesktopPlus.Services.DatabaseService(DbPath);
		service.Initialize();
		return service;
	}

	public void Dispose()
	{
		try
		{
			Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
			if (Directory.Exists(_dir))
			{
				Directory.Delete(_dir, recursive: true);
			}
		}
		catch
		{
		}
	}
}
