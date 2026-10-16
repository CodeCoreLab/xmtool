using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace RemoteDesktopPlus.Services;

public static class BackupService
{
	public static void Export(string targetPath)
	{
		using FileStream fileStream = new FileStream(DatabaseService.Instance.DbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using FileStream destination = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
		fileStream.CopyTo(destination);
	}

	public static void Import(string sourcePath)
	{
		if (!File.Exists(sourcePath))
		{
			throw new FileNotFoundException("导入的数据库文件不存在", sourcePath);
		}
		string dbPath = DatabaseService.Instance.DbPath;
		string text = dbPath + ".bak";
		if (File.Exists(dbPath))
		{
			File.Copy(dbPath, text, overwrite: true);
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		SqliteConnection.ClearAllPools();
		try
		{
			File.Copy(sourcePath, dbPath, overwrite: true);
			DatabaseService.Instance.ReinitializeAfterImport();
		}
		catch
		{
			try
			{
				SqliteConnection.ClearAllPools();
				if (File.Exists(text))
				{
					File.Copy(text, dbPath, overwrite: true);
					DatabaseService.Instance.ReinitializeAfterImport();
				}
			}
			catch
			{
			}
			throw;
		}
	}
}
