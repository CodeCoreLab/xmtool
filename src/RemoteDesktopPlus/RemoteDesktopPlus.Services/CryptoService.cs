using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RemoteDesktopPlus.Models;

namespace RemoteDesktopPlus.Services;

/// <summary>
/// 凭据加解密与主密码/启动密码管理。
/// 密文前缀语义：<c>D1$</c> = DPAPI(CurrentUser)，<c>M1$</c> = 主密码派生的 AES-256-GCM。
/// </summary>
public sealed class CryptoService
{
	private static readonly Lazy<CryptoService> _instance =
		new Lazy<CryptoService>(() => new CryptoService(DatabaseService.Instance));

	private const string PrefixDpapi = "D1$";

	private const string PrefixMaster = "M1$";

	private const int Pbkdf2Iter = 200000;

	private const int KeyBytes = 32;

	private const int SaltBytes = 16;

	private const int NonceBytes = 12;

	private const int TagBytes = 16;

	private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("RemoteDesktopPlus.Salt.v1");

	private readonly DatabaseService _db;

	private byte[]? _masterKey;

	/// <summary>生产环境默认实例（绑定 <see cref="DatabaseService.Instance"/>）。</summary>
	public static CryptoService Instance => _instance.Value;

	public bool HasMasterKey => _masterKey != null;

	/// <summary>以指定的存储创建实例，便于单元测试注入临时库。</summary>
	public CryptoService(DatabaseService db)
	{
		_db = db ?? throw new ArgumentNullException(nameof(db));
	}

	public bool IsMasterPasswordConfigured()
	{
		return !string.IsNullOrEmpty(_db.GetSetting(SettingKeys.MasterKeyVerifier));
	}

	public bool Unlock(string masterPassword)
	{
		string saltB64 = _db.GetSetting(SettingKeys.MasterKeySalt);
		string verifier = _db.GetSetting(SettingKeys.MasterKeyVerifier);
		if (string.IsNullOrEmpty(saltB64) || string.IsNullOrEmpty(verifier))
		{
			return false;
		}
		byte[] salt = Convert.FromBase64String(saltB64);
		byte[] key = DeriveKey(masterPassword, salt);
		try
		{
			if (AesGcmDecrypt(verifier, key) == "OK")
			{
				_masterKey = key;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	/// <summary>设置/修改主密码：全量重加密所有条目，事务内完成，失败回滚。</summary>
	public void SetMasterPassword(string newPassword, IEnumerable<ConnectionItem> existingItems)
	{
		var source = existingItems
			.Select(item => (Item: item, OriginalCipher: item.EncryptedPassword, Plain: Decrypt(item.EncryptedPassword)))
			.ToList();
		byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
		byte[] key = DeriveKey(newPassword, salt);
		string verifier = AesGcmEncrypt("OK", key);
		string saltB64 = Convert.ToBase64String(salt);
		var newCiphers = source
			.Select(s => (s.Item, s.OriginalCipher, NewCipher: PrefixMaster + AesGcmEncrypt(s.Plain, key)))
			.ToList();

		byte[]? previousKey = _masterKey;
		try
		{
			_db.RunInTransaction(delegate
			{
				_db.SetSetting(SettingKeys.MasterKeySalt, saltB64);
				_db.SetSetting(SettingKeys.MasterKeyVerifier, verifier);
				foreach (var entry in newCiphers)
				{
					entry.Item.EncryptedPassword = entry.NewCipher;
					_db.Update(entry.Item);
				}
			});
			_masterKey = key;
		}
		catch
		{
			foreach (var entry in newCiphers)
			{
				entry.Item.EncryptedPassword = entry.OriginalCipher;
			}
			_masterKey = previousKey;
			throw;
		}
	}

	/// <summary>移除主密码：回退为 DPAPI 形态，事务内完成，失败回滚。</summary>
	public void RemoveMasterPassword(IEnumerable<ConnectionItem> existingItems)
	{
		if (!HasMasterKey)
		{
			throw new InvalidOperationException("请先解锁。");
		}
		var source = existingItems
			.Select(item => (Item: item, OriginalCipher: item.EncryptedPassword, Plain: Decrypt(item.EncryptedPassword)))
			.ToList();
		var newCiphers = source
			.Select(s => (s.Item, s.OriginalCipher, NewCipher: PrefixDpapi + DpapiEncrypt(s.Plain)))
			.ToList();

		byte[]? previousKey = _masterKey;
		try
		{
			_db.RunInTransaction(delegate
			{
				_db.DeleteSetting(SettingKeys.MasterKeySalt);
				_db.DeleteSetting(SettingKeys.MasterKeyVerifier);
				foreach (var entry in newCiphers)
				{
					entry.Item.EncryptedPassword = entry.NewCipher;
					_db.Update(entry.Item);
				}
			});
			_masterKey = null;
		}
		catch
		{
			foreach (var entry in newCiphers)
			{
				entry.Item.EncryptedPassword = entry.OriginalCipher;
			}
			_masterKey = previousKey;
			throw;
		}
	}

	public string Encrypt(string plain)
	{
		if (string.IsNullOrEmpty(plain))
		{
			return string.Empty;
		}
		if (_masterKey != null)
		{
			return PrefixMaster + AesGcmEncrypt(plain, _masterKey);
		}
		return PrefixDpapi + DpapiEncrypt(plain);
	}

	/// <summary>解密失败一律返回空串，不抛出（调用方无需处理损坏密文）。</summary>
	public string Decrypt(string cipher)
	{
		if (string.IsNullOrEmpty(cipher))
		{
			return string.Empty;
		}
		try
		{
			if (cipher.StartsWith(PrefixMaster, StringComparison.Ordinal))
			{
				if (_masterKey == null)
				{
					return string.Empty;
				}
				return AesGcmDecrypt(cipher.Substring(PrefixMaster.Length), _masterKey);
			}
			if (cipher.StartsWith(PrefixDpapi, StringComparison.Ordinal))
			{
				return DpapiDecrypt(cipher.Substring(PrefixDpapi.Length));
			}
			return DpapiDecrypt(cipher);
		}
		catch
		{
			return string.Empty;
		}
	}

	public static string HashLaunchPassword(string password, out string saltB64)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
		saltB64 = Convert.ToBase64String(salt);
		return Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iter, HashAlgorithmName.SHA256, KeyBytes));
	}

	/// <summary>定时安全比较，防时序侧信道。</summary>
	public static bool VerifyLaunchPassword(string password, string hashB64, string saltB64)
	{
		try
		{
			byte[] salt = Convert.FromBase64String(saltB64);
			byte[] expected = Convert.FromBase64String(hashB64);
			byte[] actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iter, HashAlgorithmName.SHA256, KeyBytes);
			return CryptographicOperations.FixedTimeEquals(actual, expected);
		}
		catch
		{
			return false;
		}
	}

	private static byte[] DeriveKey(string password, byte[] salt)
	{
		return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iter, HashAlgorithmName.SHA256, KeyBytes);
	}

	private static string AesGcmEncrypt(string plain, byte[] key)
	{
		byte[] nonce = RandomNumberGenerator.GetBytes(NonceBytes);
		byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
		byte[] cipher = new byte[plainBytes.Length];
		byte[] tag = new byte[TagBytes];
		using (AesGcm aes = new AesGcm(key, TagBytes))
		{
			aes.Encrypt(nonce, plainBytes, cipher, tag);
		}
		// 装机格式：nonce | tag | cipher，再整体 Base64
		byte[] packed = new byte[nonce.Length + tag.Length + cipher.Length];
		Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
		Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
		Buffer.BlockCopy(cipher, 0, packed, nonce.Length + tag.Length, cipher.Length);
		return Convert.ToBase64String(packed);
	}

	private static string AesGcmDecrypt(string cipherB64, byte[] key)
	{
		byte[] packed = Convert.FromBase64String(cipherB64);
		if (packed.Length < NonceBytes + TagBytes)
		{
			throw new CryptographicException("密文长度不足");
		}
		byte[] nonce = new byte[NonceBytes];
		byte[] tag = new byte[TagBytes];
		byte[] cipher = new byte[packed.Length - NonceBytes - TagBytes];
		Buffer.BlockCopy(packed, 0, nonce, 0, NonceBytes);
		Buffer.BlockCopy(packed, NonceBytes, tag, 0, TagBytes);
		Buffer.BlockCopy(packed, NonceBytes + TagBytes, cipher, 0, cipher.Length);
		byte[] plain = new byte[cipher.Length];
		using (AesGcm aes = new AesGcm(key, TagBytes))
		{
			aes.Decrypt(nonce, cipher, tag, plain);
		}
		return Encoding.UTF8.GetString(plain);
	}

	private static string DpapiEncrypt(string plain)
	{
		return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), DpapiEntropy, DataProtectionScope.CurrentUser));
	}

	private static string DpapiDecrypt(string b64)
	{
		byte[] bytes = ProtectedData.Unprotect(Convert.FromBase64String(b64), DpapiEntropy, DataProtectionScope.CurrentUser);
		return Encoding.UTF8.GetString(bytes);
	}
}
