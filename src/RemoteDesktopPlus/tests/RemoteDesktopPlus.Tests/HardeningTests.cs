using System;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 锁死本次健壮性改造引入的三条不变量：
/// 1) .rdp 文件内容不能被 CRLF 注入（否则可写入任意配置行）；
/// 2) 连接字段长度有上限，超限必须被拒绝而不是静默截断；
/// 3) 主密码解锁失败必须可区分「失败」与「本来就是空密码」，且解密失败不得
///    被当作空字符串继续使用（否则会用空密码覆盖真实密码）。
/// </summary>
public class HardeningTests
{
	[Theory]
	[InlineData("evil\r\nadministrative mode:s:1")]
	[InlineData("evil\nadministrative mode:s:1")]
	[InlineData("evil\radmin")]
	public void SanitizeRdpValue_StripsCrLfAndControlChars(string hostile)
	{
		string sanitized = RdpLauncher.SanitizeRdpValue(hostile);
		// 换行与控制字符必须被剔除，剩余文本只能作为同一行内容存在。
		Assert.DoesNotContain("\r", sanitized);
		Assert.DoesNotContain("\n", sanitized);
		Assert.DoesNotContain("\t", sanitized);
		// 注入文本被压平后不再是独立的一行。
		foreach (string line in sanitized.Split('\n'))
		{
			Assert.False(line.StartsWith("administrative mode:s:1", StringComparison.Ordinal));
		}
	}

	[Fact]
	public void BuildRdpContent_HostWithCrLf_CannotInjectExtraConfigLine()
	{
		string content = RdpLauncher.BuildRdpContent(new RdpLauncher.RdpOptions(
			"10.0.0.1\r\nadministrative mode:s:1",
			3389,
			"admin",
			"pw",
			null,
			null,
			false,
			null,
			false,
			true));

		// 注入内容必须被压成同一行，不能形成新的配置项。
		Assert.DoesNotContain("full address:s:10.0.0.1\r\n", content);
		foreach (string line in content.Split('\n'))
		{
			Assert.False(line.StartsWith("administrative mode:s:1", StringComparison.Ordinal));
		}
		int lineCount = content.Split('\n').Length;
		// 全屏模式固定若干行，注入只会增加换行数量；断言换行数不因注入而暴涨。
		Assert.True(lineCount < 30, "注入后行数异常: " + lineCount);
	}

	[Fact]
	public void SanitizeRdpValue_KeepsNormalHostUnchanged()
	{
		Assert.Equal("10.0.0.1", RdpLauncher.SanitizeRdpValue("10.0.0.1"));
		Assert.Equal("host.example.com", RdpLauncher.SanitizeRdpValue("host.example.com"));
		Assert.Equal(string.Empty, RdpLauncher.SanitizeRdpValue(null));
		Assert.Equal(string.Empty, RdpLauncher.SanitizeRdpValue(string.Empty));
	}

	[Fact]
	public void ValidateLengths_RejectsOverlongFields()
	{
		ConnectionFormResult form = new ConnectionFormResult
		{
			Host = new string('h', ConnectionFormResult.MaxHostLength + 1)
		};
		Assert.NotNull(form.ValidateLengths());

		form.Host = new string('h', ConnectionFormResult.MaxHostLength);
		form.UserName = new string('u', ConnectionFormResult.MaxUserNameLength);
		form.Password = new string('p', ConnectionFormResult.MaxPasswordLength);
		form.Remark = new string('r', ConnectionFormResult.MaxRemarkLength);
		form.GroupName = new string('g', ConnectionFormResult.MaxGroupNameLength);
		Assert.Null(form.ValidateLengths());
	}

	[Fact]
	public void TryDecrypt_EmptyCipher_IsSuccessButEmptyPlain()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());

		// 空密文表示「这条记录本来就没有密码」，不是解密失败。
		Assert.True(crypto.TryDecrypt(string.Empty, out string plain));
		Assert.Equal(string.Empty, plain);

		Assert.True(crypto.TryDecrypt(null, out string plainNull));
		Assert.Equal(string.Empty, plainNull);
	}

	[Fact]
	public void TryDecrypt_GarbageCipher_ReturnsFalseAndEmptyPlain()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());

		Assert.False(crypto.TryDecrypt("D1$not-a-valid-ciphertext", out string plain));
		Assert.Equal(string.Empty, plain);
	}

	[Fact]
	public void Decrypt_InvalidCipher_ReturnsEmptyInsteadOfThrowing()
	{
		using TempDb tmp = new TempDb();
		CryptoService crypto = new CryptoService(tmp.NewService());

		// 旧实现会抛异常导致上层崩溃；这里必须返回空串。
		Assert.Equal(string.Empty, crypto.Decrypt("D1$garbage"));
	}

	[Fact]
	public void CollectDecryptableItems_WithUndecryptableItem_ThrowsAndNamesTheHost()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		crypto.SetMasterPassword("master-1", Array.Empty<ConnectionItem>());

		ConnectionItem good = new ConnectionItem { Host = "good.example", EncryptedPassword = crypto.Encrypt("pw") };
		ConnectionItem bad = new ConnectionItem { Host = "bad.example", EncryptedPassword = "M1$corrupted" };

		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
			delegate { crypto.CollectDecryptableItems(new[] { good, bad }); });
		Assert.Contains("bad.example", ex.Message);
	}

	[Fact]
	public void CollectDecryptableItems_AllDecryptable_ReturnsPlaintexts()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		crypto.SetMasterPassword("master-1", Array.Empty<ConnectionItem>());

		ConnectionItem item = new ConnectionItem { Host = "h1", EncryptedPassword = crypto.Encrypt("secret") };
		System.Collections.Generic.List<(ConnectionItem Item, string OriginalCipher, string Plain)> plains =
			crypto.CollectDecryptableItems(new[] { item });
		Assert.Single(plains);
		Assert.Equal("secret", plains[0].Plain);
	}
}
