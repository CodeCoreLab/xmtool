using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using RemoteDesktopPlus.Models;

namespace RemoteDesktopPlus.Services;

public sealed class DatabaseService
{
	private readonly struct ConnectionScope : IDisposable
	{
		private readonly bool _owned;

		public SqliteConnection Connection { get; }

		public SqliteTransaction? Transaction { get; }

		public ConnectionScope(SqliteConnection conn, SqliteTransaction? tx, bool owned)
		{
			Connection = conn;
			Transaction = tx;
			_owned = owned;
		}

		public SqliteCommand CreateCommand(string sql)
		{
			SqliteCommand sqliteCommand = new SqliteCommand(sql, Connection);
			if (Transaction != null)
			{
				sqliteCommand.Transaction = Transaction;
			}
			return sqliteCommand;
		}

		public void Dispose()
		{
			if (_owned)
			{
				Connection.Dispose();
			}
		}
	}

	private static readonly Lazy<DatabaseService> _instance = new Lazy<DatabaseService>(() => new DatabaseService());

	private readonly string _connectionString;

	[ThreadStatic]
	private static SqliteConnection? _ambientConnection;

	[ThreadStatic]
	private static SqliteTransaction? _ambientTransaction;

	public static DatabaseService Instance => _instance.Value;

	public string DbPath { get; }

	private DatabaseService()
	{
		string baseDirectory = AppContext.BaseDirectory;
		DbPath = Path.Combine(baseDirectory, "data.db");
		_connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = DbPath,
			Mode = SqliteOpenMode.ReadWriteCreate
		}.ToString();
	}

	public void Initialize()
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("CREATE TABLE IF NOT EXISTS Connections (\r\n    Id              INTEGER PRIMARY KEY AUTOINCREMENT,\r\n    Host            TEXT NOT NULL,\r\n    Port            INTEGER NOT NULL DEFAULT 3389,\r\n    UserName        TEXT NOT NULL DEFAULT '',\r\n    Password        TEXT NOT NULL DEFAULT '',\r\n    Remark          TEXT NOT NULL DEFAULT '',\r\n    SortOrder       INTEGER NOT NULL DEFAULT 0\r\n);\r\n\r\nCREATE TABLE IF NOT EXISTS Settings (\r\n    Key             TEXT PRIMARY KEY,\r\n    Value           TEXT NOT NULL DEFAULT ''\r\n);");
		sqliteCommand.ExecuteNonQuery();
	}

	private ConnectionScope AcquireConnection()
	{
		if (_ambientConnection != null)
		{
			return new ConnectionScope(_ambientConnection, _ambientTransaction, owned: false);
		}
		SqliteConnection sqliteConnection = new SqliteConnection(_connectionString);
		sqliteConnection.Open();
		return new ConnectionScope(sqliteConnection, null, owned: true);
	}

	public List<ConnectionItem> LoadAll()
	{
		List<ConnectionItem> list = new List<ConnectionItem>();
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("SELECT Id, Host, Port, UserName, Password, Remark, SortOrder FROM Connections ORDER BY SortOrder ASC, Id ASC;");
		using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
		int num = 0;
		while (sqliteDataReader.Read())
		{
			ConnectionItem obj = new ConnectionItem
			{
				Id = sqliteDataReader.GetInt64(0),
				Host = sqliteDataReader.GetString(1),
				Port = sqliteDataReader.GetInt32(2),
				UserName = sqliteDataReader.GetString(3),
				EncryptedPassword = sqliteDataReader.GetString(4),
				Remark = sqliteDataReader.GetString(5),
				SortOrder = sqliteDataReader.GetInt32(6)
			};
			num = (obj.Index = num + 1);
			list.Add(obj);
		}
		return list;
	}

	public void Insert(ConnectionItem item)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("INSERT INTO Connections (Host, Port, UserName, Password, Remark, SortOrder)\r\nVALUES ($host, $port, $user, $pwd, $remark, $sort);\r\nSELECT last_insert_rowid();");
		sqliteCommand.Parameters.AddWithValue("$host", item.Host);
		sqliteCommand.Parameters.AddWithValue("$port", item.Port);
		sqliteCommand.Parameters.AddWithValue("$user", item.UserName);
		sqliteCommand.Parameters.AddWithValue("$pwd", item.EncryptedPassword ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$remark", item.Remark ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$sort", item.SortOrder);
		item.Id = (long)(sqliteCommand.ExecuteScalar() ?? ((object)0L));
	}

	public void Update(ConnectionItem item)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("UPDATE Connections\r\n   SET Host = $host, Port = $port, UserName = $user,\r\n       Password = $pwd, Remark = $remark, SortOrder = $sort\r\n WHERE Id = $id;");
		sqliteCommand.Parameters.AddWithValue("$host", item.Host);
		sqliteCommand.Parameters.AddWithValue("$port", item.Port);
		sqliteCommand.Parameters.AddWithValue("$user", item.UserName);
		sqliteCommand.Parameters.AddWithValue("$pwd", item.EncryptedPassword ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$remark", item.Remark ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$sort", item.SortOrder);
		sqliteCommand.Parameters.AddWithValue("$id", item.Id);
		sqliteCommand.ExecuteNonQuery();
	}

	public void Delete(long id)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("DELETE FROM Connections WHERE Id = $id;");
		sqliteCommand.Parameters.AddWithValue("$id", id);
		sqliteCommand.ExecuteNonQuery();
	}

	public void UpdateSortOrders(IEnumerable<ConnectionItem> items)
	{
		IEnumerable<ConnectionItem> items2 = items;
		RunInTransaction(delegate
		{
			using ConnectionScope connectionScope = AcquireConnection();
			using SqliteCommand sqliteCommand = connectionScope.CreateCommand("UPDATE Connections SET SortOrder = $sort WHERE Id = $id;");
			SqliteParameter sqliteParameter = sqliteCommand.Parameters.Add("$sort", SqliteType.Integer);
			SqliteParameter sqliteParameter2 = sqliteCommand.Parameters.Add("$id", SqliteType.Integer);
			foreach (ConnectionItem item in items2)
			{
				sqliteParameter.Value = item.SortOrder;
				sqliteParameter2.Value = item.Id;
				sqliteCommand.ExecuteNonQuery();
			}
		});
	}

	public string? GetSetting(string key)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("SELECT Value FROM Settings WHERE Key = $k;");
		sqliteCommand.Parameters.AddWithValue("$k", key);
		return sqliteCommand.ExecuteScalar() as string;
	}

	public void SetSetting(string key, string value)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("INSERT INTO Settings (Key, Value) VALUES ($k, $v)\r\nON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;");
		sqliteCommand.Parameters.AddWithValue("$k", key);
		sqliteCommand.Parameters.AddWithValue("$v", value);
		sqliteCommand.ExecuteNonQuery();
	}

	public void DeleteSetting(string key)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("DELETE FROM Settings WHERE Key = $k;");
		sqliteCommand.Parameters.AddWithValue("$k", key);
		sqliteCommand.ExecuteNonQuery();
	}

	public void RunInTransaction(Action action)
	{
		if (_ambientConnection != null)
		{
			action();
			return;
		}
		SqliteConnection sqliteConnection = new SqliteConnection(_connectionString);
		sqliteConnection.Open();
		using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction();
		_ambientConnection = sqliteConnection;
		_ambientTransaction = sqliteTransaction;
		try
		{
			action();
			sqliteTransaction.Commit();
		}
		catch
		{
			try
			{
				sqliteTransaction.Rollback();
			}
			catch
			{
			}
			throw;
		}
		finally
		{
			_ambientConnection = null;
			_ambientTransaction = null;
			sqliteConnection.Dispose();
		}
	}

	public bool HasLaunchPassword()
	{
		if (!string.IsNullOrEmpty(GetSetting("launch.password.hash")))
		{
			return !string.IsNullOrEmpty(GetSetting("launch.password.salt"));
		}
		return false;
	}

	public void ReinitializeAfterImport()
	{
		Initialize();
	}
}
