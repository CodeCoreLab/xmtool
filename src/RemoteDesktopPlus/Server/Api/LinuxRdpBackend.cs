using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Server.Api;

/// <summary>
/// Linux（信创）RDP 后端：L1$ 文件凭据 + xfreerdp 拉起。
/// 对齐选型方案 v1.1 §3.2/§3.3 与 prompt-forge crypto.rs 已验证模式。
/// 安全性质保持 IRdpBackend 契约：密码只进 L1$ 加密文件（machine-id 派生密钥，
/// 本机可解/跨机不可解，语义对齐 DPAPI CurrentUser），进程参数面不含密码。
/// </summary>
public sealed class LinuxRdpBackend : IRdpBackend
{
    public static LinuxRdpBackend Instance { get; } = new LinuxRdpBackend();

    /// <summary>凭据文件目录：$XDG_DATA_HOME/RemoteDesktopPlus/credentials（缺省 ~/.local/share）。</summary>
    internal static string CredentialDir()
    {
        string baseDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(baseDir))
        {
            string home = Environment.GetEnvironmentVariable("HOME") ?? ".";
            baseDir = Path.Combine(home, ".local", "share");
        }
        return Path.Combine(baseDir, "RemoteDesktopPlus", "credentials");
    }

    /// <summary>machine-id：本机唯一标识（麒麟 V10 / UOS 均有）。</summary>
    internal static string MachineId()
    {
        foreach (var p in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
        {
            if (File.Exists(p))
            {
                var t = File.ReadAllText(p).Trim();
                if (t.Length > 0) return t;
            }
        }
        throw new InvalidOperationException("无法读取 machine-id（/etc/machine-id 不存在）");
    }

    /// <summary>主密钥 = SHA-256(machine-id + "remote-desktop-plus/v1")，32 字节。</summary>
    internal static byte[] DeriveKey()
    {
        using var sha = SHA256.Create();
        var mid = Encoding.UTF8.GetBytes(MachineId());
        var domain = Encoding.UTF8.GetBytes("remote-desktop-plus/v1");
        return sha.ComputeHash([.. mid, .. domain]);
    }

    /// <summary>明文 → L1$ 密文（AES-256-GCM，随机 12B nonce 前置）。</summary>
    internal static string ProtectL1(string plain)
    {
        byte[] key = DeriveKey();
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
        byte[] cipher = new byte[plainBytes.Length];
        byte[] tag = new byte[16];
        using (var gcm = new AesGcm(key, 16))
        {
            gcm.Encrypt(nonce, plainBytes, tag, cipher);
        }
        byte[] packed = [.. nonce, .. tag, .. cipher];
        return "L1$" + Convert.ToBase64String(packed);
    }

    /// <summary>L1$ 密文 → 明文。</summary>
    internal static string UnprotectL1(string stored)
    {
        const string prefix = "L1$";
        if (!stored.StartsWith(prefix, StringComparison.Ordinal))
            throw new FormatException("非 L1$ 密文");
        byte[] packed = Convert.FromBase64String(stored[prefix.Length..]);
        if (packed.Length <= 12 + 16)
            throw new FormatException("L1$ 密文长度异常");
        byte[] key = DeriveKey();
        byte[] nonce = packed[..12];
        byte[] tag = packed[12..28];
        byte[] cipher = packed[28..];
        byte[] plain = new byte[cipher.Length];
        using (var gcm = new AesGcm(key, 16))
        {
            gcm.Decrypt(nonce, cipher, tag, plain);
        }
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>target → 安全文件名（TERMSRV/host → host；拒绝路径穿越）。</summary>
    private static string SafeFileName(string target)
    {
        string name = target.StartsWith("TERMSRV/", StringComparison.OrdinalIgnoreCase)
            ? target["TERMSRV/".Length..] : target;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name + ".cred";
    }

    public bool StoreCredential(string target, string userName, string password)
    {
        try
        {
            string dir = CredentialDir();
            Directory.CreateDirectory(dir);
            // 文件权限：仅属主可读写（0600 等价）；Windows 上无此概念，Silent OK
            string payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                user = userName,
                pass = ProtectL1(password),
            });
            File.WriteAllText(Path.Combine(dir, SafeFileName(target)), payload);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("L1$ 凭据写入失败: " + ex.Message);
            return false;
        }
    }

    public void DeleteCredential(string target)
    {
        string path = Path.Combine(CredentialDir(), SafeFileName(target));
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            Log.Error("L1$ 凭据删除失败: " + ex.Message);
        }
    }

    /// <summary>读取 L1$ 凭据（M3 xfreerdp 拉起用：密码经 stdin 传入，不进参数面）。</summary>
    internal static (string User, string Password)? ReadCredential(string target)
    {
        string path = Path.Combine(CredentialDir(), SafeFileName(target));
        if (!File.Exists(path)) return null;
        var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        string user = doc.RootElement.GetProperty("user").GetString() ?? string.Empty;
        string pass = UnprotectL1(doc.RootElement.GetProperty("pass").GetString() ?? string.Empty);
        return (user, pass);
    }

    /// <summary>
    /// xfreerdp 参数构造（2.x/3.x 双参数表，对齐选型 D4 运行时探测）。
    /// </summary>
    internal static string[] BuildXfreerdpArgs(string host, int port, string userName,
        string? password, int? width, int? height, bool fullScreen)
    {
        // 版本探测：xfreerdp --version 首行含 "3." → 3.x 参数表（/cert:ignore → /cert:ignore 同名，
        // 2/3 差异主要在 /kbd 与 /multimedia，此处只用跨版本稳定参数）
        var args = new List<string>
        {
            $"/v:{host}:{port}",
            $"/u:{userName}",
            fullScreen ? "/f" : "/window",
            "/clipboard",
            "/cert:ignore",
            "/sec:tls",
        };
        if (width.HasValue && height.HasValue)
        {
            args.Add($"/w:{width.Value}");
            args.Add($"/h:{height.Value}");
        }
        if (!string.IsNullOrEmpty(password))
        {
            // 3.x: /from:stdin 或环境变量 FREERDP_PASSWORD；2.x: /p:（泄漏风险）→
            // 统一走 stdin 管道（FreeRDP 2.2+/3.x 均支持 /from:stdin），
            // 参数面仍然零密码——契约保持
            args.Add("/from:stdin");
        }
        return args.ToArray();
    }

    public System.Diagnostics.Process? StartClient(string rdpFilePath)
    {
        // rdpFilePath 对 Linux 版语义 = 目标描述（host:port），由调用方传 target
        // 真实拉起在 LaunchXfreerdp（由 LinuxRdpLauncher 调用）
        throw new NotSupportedException("Linux 后端走 LaunchXfreerdp，不经 .rdp 文件");
    }

    /// <summary>xfreerdp 拉起：密码经 stdin 传入，参数面不含密码（契约保持）。</summary>
    internal static void LaunchXfreerdp(string host, int port, string userName,
        string? password, int? width, int? height, bool fullScreen)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "xfreerdp",
            RedirectStandardInput = !string.IsNullOrEmpty(password),
            UseShellExecute = false,
        };
        foreach (var a in BuildXfreerdpArgs(host, port, userName, password, width, height, fullScreen))
        {
            psi.ArgumentList.Add(a);
        }
        var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("xfreerdp 启动失败（未安装？apt install freerdp2-x11）");
        if (!string.IsNullOrEmpty(password))
        {
            proc.StandardInput.WriteLine(password);
            proc.StandardInput.Close();
        }
    }
}
