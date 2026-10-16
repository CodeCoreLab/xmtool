using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RemoteDesktopPlus.Models;

namespace RemoteDesktopPlus.Services;

public sealed class CryptoService
{
	private static readonly Lazy<CryptoService> _instance = new Lazy<CryptoService>(() => new CryptoService());

	private const string PrefixDpapi = "D1$";

	private const string PrefixMaster = "M1$";

	private const int Pbkdf2Iter = 200000;

	private const int KeyBytes = 32;

	private const int SaltBytes = 16;

	private const int NonceBytes = 12;

	private const int TagBytes = 16;

	private byte[]? _masterKey;

	private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("RemoteDesktopPlus.Salt.v1");

	public static CryptoService Instance => _instance.Value;

	public bool HasMasterKey => _masterKey != null;

	public static bool IsMasterPasswordConfigured()
	{
		return !string.IsNullOrEmpty(DatabaseService.Instance.GetSetting("master.key.verifier"));
	}

	public bool Unlock(string masterPassword)
	{
		string setting = DatabaseService.Instance.GetSetting("master.key.salt");
		string setting2 = DatabaseService.Instance.GetSetting("master.key.verifier");
		if (string.IsNullOrEmpty(setting) || string.IsNullOrEmpty(setting2))
		{
			return false;
		}
		byte[] salt = Convert.FromBase64String(setting);
		byte[] array = DeriveKey(masterPassword, salt);
		try
		{
			if (AesGcmDecrypt(setting2, array) == "OK")
			{
				_masterKey = array;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public void SetMasterPassword(string newPassword, IEnumerable<ConnectionItem> existingItems)
	{
		List<(ConnectionItem, string, string)> source = existingItems.Select((ConnectionItem it) => (Item: it, OriginalCipher: it.EncryptedPassword, Plain: Decrypt(it.EncryptedPassword))).ToList();
		byte[] bytes = RandomNumberGenerator.GetBytes(16);
		byte[] key = DeriveKey(newPassword, bytes);
		string verifier = AesGcmEncrypt("OK", key);
		string saltB64 = Convert.ToBase64String(bytes);
		List<(ConnectionItem Item, string OriginalCipher, string NewCipher)> newCiphers = source.Select<(ConnectionItem, string, string), (ConnectionItem, string, string)>(((ConnectionItem Item, string OriginalCipher, string Plain) s) => (Item: s.Item, OriginalCipher: s.OriginalCipher, NewCipher: "M1$" + AesGcmEncrypt(s.Plain, key))).ToList();
		byte[] masterKey = _masterKey;
		try
		{
			DatabaseService.Instance.RunInTransaction(delegate
			{
				DatabaseService.Instance.SetSetting("master.key.salt", saltB64);
				DatabaseService.Instance.SetSetting("master.key.verifier", verifier);
				foreach (var item5 in newCiphers)
				{
					ConnectionItem item3 = item5.Item;
					string item4 = item5.NewCipher;
					item3.EncryptedPassword = item4;
					DatabaseService.Instance.Update(item3);
				}
			});
			_masterKey = key;
		}
		catch
		{
			foreach (var item6 in newCiphers)
			{
				ConnectionItem item = item6.Item;
				string item2 = item6.OriginalCipher;
				item.EncryptedPassword = item2;
			}
			_masterKey = masterKey;
			throw;
		}
	}

	public void RemoveMasterPassword(IEnumerable<ConnectionItem> existingItems)
	{
		if (!HasMasterKey)
		{
			throw new InvalidOperationException("请先解锁。");
		}
		List<(ConnectionItem, string, string)> source = existingItems.Select((ConnectionItem it) => (Item: it, OriginalCipher: it.EncryptedPassword, Plain: Decrypt(it.EncryptedPassword))).ToList();
		List<(ConnectionItem Item, string OriginalCipher, string NewCipher)> newCiphers = source.Select<(ConnectionItem, string, string), (ConnectionItem, string, string)>(((ConnectionItem Item, string OriginalCipher, string Plain) s) => (Item: s.Item, OriginalCipher: s.OriginalCipher, NewCipher: "D1$" + DpapiEncrypt(s.Plain))).ToList();
		byte[] masterKey = _masterKey;
		try
		{
			DatabaseService.Instance.RunInTransaction(delegate
			{
				DatabaseService.Instance.DeleteSetting("master.key.salt");
				DatabaseService.Instance.DeleteSetting("master.key.verifier");
				foreach (var item5 in newCiphers)
				{
					ConnectionItem item3 = item5.Item;
					string item4 = item5.NewCipher;
					item3.EncryptedPassword = item4;
					DatabaseService.Instance.Update(item3);
				}
			});
			_masterKey = null;
		}
		catch
		{
			foreach (var item6 in newCiphers)
			{
				ConnectionItem item = item6.Item;
				string item2 = item6.OriginalCipher;
				item.EncryptedPassword = item2;
			}
			_masterKey = masterKey;
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
			return "M1$" + AesGcmEncrypt(plain, _masterKey);
		}
		return "D1$" + DpapiEncrypt(plain);
	}

	public string Decrypt(string cipher)
	{
		if (string.IsNullOrEmpty(cipher))
		{
			return string.Empty;
		}
		try
		{
			if (cipher.StartsWith("M1$", StringComparison.Ordinal))
			{
				if (_masterKey == null)
				{
					return string.Empty;
				}
				return AesGcmDecrypt(cipher.AsSpan("M1$".Length).ToString(), _masterKey);
			}
			if (cipher.StartsWith("D1$", StringComparison.Ordinal))
			{
				return DpapiDecrypt(cipher.AsSpan("D1$".Length).ToString());
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
		byte[] bytes = RandomNumberGenerator.GetBytes(16);
		saltB64 = Convert.ToBase64String(bytes);
		return Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), bytes, 200000, HashAlgorithmName.SHA256, 32));
	}

	public static bool VerifyLaunchPassword(string password, string hashB64, string saltB64)
	{
		try
		{
			byte[] salt = Convert.FromBase64String(saltB64);
			byte[] array = Convert.FromBase64String(hashB64);
			return CryptographicOperations.FixedTimeEquals(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 200000, HashAlgorithmName.SHA256, 32), array);
		}
		catch
		{
			return false;
		}
	}

	private static byte[] DeriveKey(string password, byte[] salt)
	{
		return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 200000, HashAlgorithmName.SHA256, 32);
	}

	private static string AesGcmEncrypt(string plain, byte[] key)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(12);
		byte[] bytes2 = Encoding.UTF8.GetBytes(plain);
		byte[] array = new byte[bytes2.Length];
		byte[] array2 = new byte[16];
		using AesGcm aesGcm = new AesGcm(key, 16);
		aesGcm.Encrypt(bytes, bytes2, array, array2);
		byte[] array3 = new byte[bytes.Length + array2.Length + array.Length];
		Buffer.BlockCopy(bytes, 0, array3, 0, bytes.Length);
		Buffer.BlockCopy(array2, 0, array3, bytes.Length, array2.Length);
		Buffer.BlockCopy(array, 0, array3, bytes.Length + array2.Length, array.Length);
		return Convert.ToBase64String(array3);
	}

	private static string AesGcmDecrypt(string cipherB64, byte[] key)
	{
		byte[] array = Convert.FromBase64String(cipherB64);
		if (array.Length < 28)
		{
			throw new CryptographicException("密文长度不足");
		}
		byte[] nonce = array.AsSpan(0, 12).ToArray();
		byte[] tag = array.AsSpan(12, 16).ToArray();
		byte[] array2 = array.AsSpan(28).ToArray();
		byte[] array3 = new byte[array2.Length];
		using AesGcm aesGcm = new AesGcm(key, 16);
		aesGcm.Decrypt(nonce, array2, tag, array3);
		return Encoding.UTF8.GetString(array3);
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
