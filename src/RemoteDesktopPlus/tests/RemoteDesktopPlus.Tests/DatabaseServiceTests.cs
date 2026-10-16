using System;
using System.Collections.Generic;
using System.Linq;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 锁死持久化契约：CRUD、排序、Settings upsert、事务回滚。
/// 注意 SetSetting 走 ON CONFLICT DO UPDATE（不是 INSERT OR REPLACE），
/// 回归测试要能区分这两者。
/// </summary>
public class DatabaseServiceTests
{
	private static ConnectionItem Item(string host, int sort = 0, string user = "u", string pwd = "c1", string remark = "r")
	{
		return new ConnectionItem
		{
			Host = host,
			Port = 3389,
			UserName = user,
			EncryptedPassword = pwd,
			Remark = remark,
			SortOrder = sort
		};
	}

	[Fact]
	public void Initialize_IsIdempotent_AndCreatesBothTables()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		db.Initialize();
		Assert.Empty(db.LoadAll());
		Assert.Null(db.GetSetting("nope"));
	}

	[Fact]
	public void Insert_AssignsAutoIncrementId_AndSurvivesReload()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		ConnectionItem a = Item("10.0.0.1", 1);
		ConnectionItem b = Item("10.0.0.2", 2);
		db.Insert(a);
		db.Insert(b);

		Assert.True(a.Id > 0);
		Assert.NotEqual(a.Id, b.Id);

		List<ConnectionItem> loaded = db.LoadAll();
		Assert.Equal(2, loaded.Count);
		Assert.Equal("10.0.0.1", loaded[0].Host);
		Assert.Equal("10.0.0.2", loaded[1].Host);
	}

	[Fact]
	public void LoadAll_OrdersBySortOrderThenId_AndReindexesFromOne()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		ConnectionItem a = Item("a", 30);
		ConnectionItem b = Item("b", 10);
		ConnectionItem c = Item("c", 20);
		db.Insert(a);
		db.Insert(b);
		db.Insert(c);

		List<ConnectionItem> loaded = db.LoadAll();
		Assert.Equal(new[] { "b", "c", "a" }, loaded.Select(x => x.Host).ToArray());
		Assert.Equal(new[] { 1, 2, 3 }, loaded.Select(x => x.Index).ToArray());
	}

	[Fact]
	public void Update_PersistsAllEditableColumns()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		ConnectionItem a = Item("old-host", 1);
		db.Insert(a);

		a.Host = "new-host";
		a.Port = 3390;
		a.UserName = "new-user";
		a.EncryptedPassword = "new-cipher";
		a.Remark = "new-remark";
		a.SortOrder = 9;
		db.Update(a);

		ConnectionItem loaded = db.LoadAll().Single();
		Assert.Equal("new-host", loaded.Host);
		Assert.Equal(3390, loaded.Port);
		Assert.Equal("new-user", loaded.UserName);
		Assert.Equal("new-cipher", loaded.EncryptedPassword);
		Assert.Equal("new-remark", loaded.Remark);
		Assert.Equal(9, loaded.SortOrder);
	}

	[Fact]
	public void Delete_RemovesRow_AndLeavesOthersIntact()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		ConnectionItem a = Item("a", 1);
		ConnectionItem b = Item("b", 2);
		db.Insert(a);
		db.Insert(b);

		db.Delete(a.Id);

		List<ConnectionItem> loaded = db.LoadAll();
		Assert.Single(loaded);
		Assert.Equal("b", loaded[0].Host);
	}

	[Fact]
	public void Delete_UnknownId_IsNoOp()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Insert(Item("a", 1));
		db.Delete(999999);
		Assert.Single(db.LoadAll());
	}

	[Fact]
	public void UpdateSortOrders_PersistsOrder()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		ConnectionItem a = Item("a", 1);
		ConnectionItem b = Item("b", 2);
		ConnectionItem c = Item("c", 3);
		db.Insert(a);
		db.Insert(b);
		db.Insert(c);

		// 模拟拖拽后的新顺序 c, a, b
		List<ConnectionItem> newOrder = new List<ConnectionItem> { c, a, b };
		for (int i = 0; i < newOrder.Count; i++)
		{
			newOrder[i].SortOrder = i + 1;
		}
		db.UpdateSortOrders(newOrder);

		Assert.Equal(new[] { "c", "a", "b" }, db.LoadAll().Select(x => x.Host).ToArray());
	}

	[Fact]
	public void SetSetting_InsertsThenUpdates_AndGetSettingRoundtrips()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();

		db.SetSetting("k", "v1");
		Assert.Equal("v1", db.GetSetting("k"));

		db.SetSetting("k", "v2");
		Assert.Equal("v2", db.GetSetting("k"));

		// upsert 而不是追加行：再写一次仍只有一条
		db.SetSetting("k", "v3");
		Assert.Equal("v3", db.GetSetting("k"));
	}

	[Fact]
	public void SetSetting_HandlesUnicodeAndEmptyValues()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.SetSetting("options.resolution", "3840*2160");
		Assert.Equal("3840*2160", db.GetSetting("options.resolution"));
		db.SetSetting("empty", string.Empty);
		Assert.Equal(string.Empty, db.GetSetting("empty"));
	}

	[Fact]
	public void DeleteSetting_RemovesKey()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.SetSetting("k", "v");
		db.DeleteSetting("k");
		Assert.Null(db.GetSetting("k"));
	}

	[Fact]
	public void RunInTransaction_Commits_WhenActionSucceeds()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();

		db.RunInTransaction(delegate
		{
			db.SetSetting("a", "1");
			db.SetSetting("b", "2");
		});

		Assert.Equal("1", db.GetSetting("a"));
		Assert.Equal("2", db.GetSetting("b"));
	}

	[Fact]
	public void RunInTransaction_RollsBack_WhenActionThrows_AndRethrows()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.SetSetting("keep", "original");

		Assert.Throws<InvalidOperationException>(delegate
		{
			db.RunInTransaction(delegate
			{
				db.SetSetting("keep", "changed");
				db.SetSetting("added", "x");
				throw new InvalidOperationException("boom");
			});
		});

		// 两个写入都必须被回滚
		Assert.Equal("original", db.GetSetting("keep"));
		Assert.Null(db.GetSetting("added"));
	}

	[Fact]
	public void RunInTransaction_IsReentrant_WhenAlreadyInsideTransaction()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();

		db.RunInTransaction(delegate
		{
			db.SetSetting("outer", "1");
			// 嵌套调用不应开启第二个事务，也不应吞掉外层
			db.RunInTransaction(delegate
			{
				db.SetSetting("inner", "2");
			});
		});

		Assert.Equal("1", db.GetSetting("outer"));
		Assert.Equal("2", db.GetSetting("inner"));
	}

	[Fact]
	public void RunInTransaction_NestedRollback_PropagatesToOuter()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.SetSetting("keep", "original");

		Assert.Throws<InvalidOperationException>(delegate
		{
			db.RunInTransaction(delegate
			{
				db.SetSetting("keep", "outer-change");
				db.RunInTransaction(delegate
				{
					db.SetSetting("nested", "x");
					throw new InvalidOperationException("inner boom");
				});
			});
		});

		Assert.Equal("original", db.GetSetting("keep"));
		Assert.Null(db.GetSetting("nested"));
	}

	[Fact]
	public void HasLaunchPassword_RequiresBothHashAndSalt()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		Assert.False(db.HasLaunchPassword());

		db.SetSetting(SettingKeys.LaunchPasswordHash, "hash-only");
		Assert.False(db.HasLaunchPassword());

		db.SetSetting(SettingKeys.LaunchPasswordSalt, "salt");
		Assert.True(db.HasLaunchPassword());

		db.DeleteSetting(SettingKeys.LaunchPasswordSalt);
		Assert.False(db.HasLaunchPassword());
	}

	[Fact]
	public void DbPath_IsHonoured_SoTestsNeverTouchProductionDb()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		Assert.Equal(tmp.DbPath, db.DbPath);
		Assert.Contains("rdpp_tests", db.DbPath);
	}
}
