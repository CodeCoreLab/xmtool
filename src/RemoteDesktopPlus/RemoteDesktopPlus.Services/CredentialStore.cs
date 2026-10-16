using System;
using System.Runtime.InteropServices;

namespace RemoteDesktopPlus.Services;

/// <summary>
/// Windows 凭据管理器（Credential Manager）的编程式读写。
/// 用途：替代 <c>cmdkey.exe /pass:&lt;明文&gt;</c>——后者会把密码暴露在进程命令行上，
/// 任何进程都能通过 Win32_Process.CommandLine / 任务管理器看到。
/// 本实现等价于 cmdkey 的内部行为：CRED_TYPE_GENERIC + TargetName=TERMSRV/&lt;host&gt;，
/// CredentialBlob 为该密码的 UTF-16LE 字节。
/// </summary>
public static class CredentialStore
{
	private const int CRED_TYPE_GENERIC = 1;

	private const int CRED_PERSIST_LOCAL_MACHINE = 2;

	private const int ERROR_NOT_FOUND = 1168;

	[StructLayout(LayoutKind.Sequential)]
	private struct FILETIME
	{
		public uint dwLowDateTime;

		public uint dwHighDateTime;
	}

	/// <remarks>
	/// 字段顺序与 wincred.h 的 CREDENTIAL 一致；LastWritten 用 FILETIME（4 字节对齐）而非 long，
	/// 否则 x64 下后续字段会整体偏移 4 字节，导致 CredWrite 失败。
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	private struct CREDENTIAL
	{
		public int Flags;

		public int Type;

		public IntPtr TargetName;

		public IntPtr Comment;

		public FILETIME LastWritten;

		public int CredentialBlobSize;

		public IntPtr CredentialBlob;

		public int Persist;

		public int AttributeCount;

		public IntPtr Attributes;

		public IntPtr TargetAlias;

		public IntPtr UserName;
	}

	[DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredWrite(ref CREDENTIAL credential, int flags);

	[DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredDelete(string target, int type, int flags);

	/// <summary>写入通用凭据。失败返回 false，不抛异常。</summary>
	public static bool WriteGeneric(string target, string userName, string password)
	{
		if (string.IsNullOrEmpty(target))
		{
			return false;
		}
		IntPtr targetPtr = IntPtr.Zero;
		IntPtr userPtr = IntPtr.Zero;
		IntPtr blobPtr = IntPtr.Zero;
		int blobSize = 0;
		try
		{
			byte[] blob = System.Text.Encoding.Unicode.GetBytes(password ?? string.Empty);
			blobSize = blob.Length;
			targetPtr = Marshal.StringToHGlobalUni(target);
			userPtr = Marshal.StringToHGlobalUni(userName ?? string.Empty);
			blobPtr = Marshal.AllocHGlobal(blob.Length);
			Marshal.Copy(blob, 0, blobPtr, blob.Length);

			CREDENTIAL credential = new CREDENTIAL
			{
				Flags = 0,
				Type = CRED_TYPE_GENERIC,
				TargetName = targetPtr,
				Comment = IntPtr.Zero,
				CredentialBlobSize = blob.Length,
				CredentialBlob = blobPtr,
				Persist = CRED_PERSIST_LOCAL_MACHINE,
				AttributeCount = 0,
				Attributes = IntPtr.Zero,
				TargetAlias = IntPtr.Zero,
				UserName = userPtr
			};
			return CredWrite(ref credential, 0);
		}
		catch
		{
			return false;
		}
		finally
		{
			if (blobPtr != IntPtr.Zero)
			{
				// 先清零再释放，避免密码残留在未回收堆块里。
				for (int i = 0; i < blobSize; i++)
				{
					Marshal.WriteByte(blobPtr, i, 0);
				}
				Marshal.FreeHGlobal(blobPtr);
			}
			if (userPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(userPtr);
			}
			if (targetPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(targetPtr);
			}
		}
	}

	/// <summary>删除通用凭据。已不存在时视为成功（与 cmdkey /delete 幂等语义一致）。</summary>
	public static bool DeleteGeneric(string target)
	{
		if (string.IsNullOrEmpty(target))
		{
			return false;
		}
		try
		{
			if (CredDelete(target, CRED_TYPE_GENERIC, 0))
			{
				return true;
			}
			return Marshal.GetLastWin32Error() == ERROR_NOT_FOUND;
		}
		catch
		{
			return false;
		}
	}
}
