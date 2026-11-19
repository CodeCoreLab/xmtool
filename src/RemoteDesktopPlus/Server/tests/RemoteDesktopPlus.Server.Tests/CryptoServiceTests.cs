using System;
using System.Collections.Generic;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 锁死加密契约：前缀语义、往返一致性、主密码全量重加密、启动密码定时安全比较。
/// 这些是最容易在重构中被“静默改坏”的行为，故优先固化。
/// </summary>
public class CryptoServiceTests
{
	[Fact]
	public void Encrypt_Then_Decrypt_Roundtrips()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());
		string cipher = crypto.Encrypt("p@ssw0rd-中文-🔐");
		Assert.NotEqual("p@ssw0rd-中文-🔐", cipher);
		Assert.Equal("p@ssw0rd-中文-🔐", crypto.Decrypt(cipher));
	}

	[Fact]
	public void Encrypt_WithoutMasterKey_UsesDpapiPrefix()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());
		Assert.StartsWith("D1$", crypto.Encrypt("abc"));
	}

	[Fact]
	public void Encrypt_WithMasterKey_UsesMasterPrefix_AndDoesNotUseDpapi()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		crypto.SetMasterPassword("master-1", Array.Empty<ConnectionItem>());

		string cipher = crypto.Encrypt("abc");
		Assert.StartsWith("M1$", cipher);
		Assert.Equal("abc", crypto.Decrypt(cipher));
	}

	[Fact]
	public void Encrypt_Empty_ReturnsEmpty()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());
		Assert.Equal(string.Empty, crypto.Encrypt(string.Empty));
		Assert.Equal(string.Empty, crypto.Decrypt(string.Empty));
	}

	[Fact]
	public void Decrypt_FailedMasterDecrypt_ReturnsEmpty_NotThrow()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		crypto.SetMasterPassword("master-1", Array.Empty<ConnectionItem>());
		string cipher = crypto.Encrypt("secret");

		// 另一个未解锁的实例读同一份密文：必须安静地返回空串，而不是抛异常。
		CryptoService locked = new CryptoService(db);
		Assert.Equal(string.Empty, locked.Decrypt(cipher));
	}

	[Fact]
	public void SetMasterPassword_ReEncryptsAllItems_AndUnlockWorksOnNewInstance()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);

		ConnectionItem item = new ConnectionItem { Host = "10.0.0.1", Port = 3389, UserName = "admin" };
		item.EncryptedPassword = crypto.Encrypt("orig-pw");
		db.Insert(item);
		Assert.StartsWith("D1$", item.EncryptedPassword);

		crypto.SetMasterPassword("master-1", new[] { item });

		Assert.StartsWith("M1$", item.EncryptedPassword);
		// 数据库里也必须是新密文（而不只是内存对象被改了）
		List<ConnectionItem> reloaded = db.LoadAll();
		Assert.Single(reloaded);
		Assert.StartsWith("M1$", reloaded[0].EncryptedPassword);

		CryptoService fresh = new CryptoService(db);
		Assert.False(fresh.HasMasterKey);
		Assert.True(fresh.Unlock("master-1"));
		Assert.Equal("orig-pw", fresh.Decrypt(reloaded[0].EncryptedPassword));
	}

	[Fact]
	public void Unlock_WrongPassword_ReturnsFalse_AndKeepsLocked()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		crypto.SetMasterPassword("right", Array.Empty<ConnectionItem>());

		CryptoService fresh = new CryptoService(db);
		Assert.False(fresh.Unlock("wrong"));
		Assert.False(fresh.HasMasterKey);
	}

	[Fact]
	public void Unlock_WhenNotConfigured_ReturnsFalse()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());
		Assert.False(crypto.IsMasterPasswordConfigured());
		Assert.False(crypto.Unlock("anything"));
	}

	[Fact]
	public void RemoveMasterPassword_FallsBackToDpapi_AndClearsSettings()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);

		ConnectionItem item = new ConnectionItem { Host = "h", Port = 3389, UserName = "u" };
		item.EncryptedPassword = crypto.Encrypt("pw");
		db.Insert(item);

		crypto.SetMasterPassword("master-1", new[] { item });
		Assert.True(crypto.HasMasterKey);

		crypto.RemoveMasterPassword(new[] { item });

		Assert.False(crypto.HasMasterKey);
		Assert.False(crypto.IsMasterPasswordConfigured());
		Assert.StartsWith("D1$", item.EncryptedPassword);
		Assert.Equal("pw", crypto.Decrypt(item.EncryptedPassword));
	}

	[Fact]
	public void RemoveMasterPassword_WhenLocked_Throws()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());
		Assert.Throws<InvalidOperationException>(() => crypto.RemoveMasterPassword(Array.Empty<ConnectionItem>()));
	}

	[Fact]
	public void HashLaunchPassword_Then_VerifyLaunchPassword_Accepts()
	{
		string hash = CryptoService.HashLaunchPassword("launch-pw", out string salt);
		Assert.False(string.IsNullOrEmpty(hash));
		Assert.False(string.IsNullOrEmpty(salt));
		Assert.True(CryptoService.VerifyLaunchPassword("launch-pw", hash, salt));
	}

	[Fact]
	public void VerifyLaunchPassword_WrongOrMalformed_ReturnsFalse()
	{
		string hash = CryptoService.HashLaunchPassword("launch-pw", out string salt);
		Assert.False(CryptoService.VerifyLaunchPassword("other-pw", hash, salt));
		Assert.False(CryptoService.VerifyLaunchPassword("launch-pw", hash, "not-base64!!"));
		Assert.False(CryptoService.VerifyLaunchPassword("launch-pw", "not-base64!!", salt));
	}

	[Fact]
	public void HashLaunchPassword_IsDeterministic_GivenSameSalt()
	{
		// PBKDF2 确定性：同一个 salt 必须派生出同一个 hash（32 字节 → Base64 44 字符）。
		// 锁死这一点，防止重构时误把它换成「每次调用都随机」的实现。
		string hash = CryptoService.HashLaunchPassword("pw", out string salt);
		Assert.Equal(44, hash.Length);
		Assert.Equal(32, Convert.FromBase64String(hash).Length);
		Assert.True(CryptoService.VerifyLaunchPassword("pw", hash, salt));
	}

	[Fact]
	public void HashLaunchPassword_DifferentCalls_UseDifferentSalts()
	{
		CryptoService.HashLaunchPassword("pw", out string salt1);
		CryptoService.HashLaunchPassword("pw", out string salt2);
		Assert.NotEqual(salt1, salt2);
	}
}
