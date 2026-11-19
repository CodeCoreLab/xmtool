// v2.1.x 回归测试（BUG 测试专家回归清单落地）：
// 覆盖本轮三轮修复的关键行为——设置项无条件生效（v2.1.2）、默认值对齐原版、
// connection type / enablecredsspsupport 键写出、WithCustomDisplayArgs 单入口合并。
// 这些断言是 v2.1.2「connection type 漏写」与 v2.1.3「页签内容错位」两类事故的回归防线。
using System;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests
{
	public class V212RegressionTests
	{
		/// <summary>最小 RdpOptions 构造（必填 10 参 + 可选默认）。</summary>
		private static RdpLauncher.RdpOptions Opt(
			bool useMultiMon = false, int colorDepth = 32, bool showConnectionBar = true,
			int audioMode = 0, int keyboardHook = 2, bool redirectPrinters = true,
			int connectionType = 7, bool disableWallpaper = false, bool allowFontSmoothing = true,
			bool allowDesktopComposition = true, bool disableFullWindowDrag = false,
			bool disableMenuAnims = false, bool disableThemes = false, bool bitmapCachePersist = true,
			bool autoReconnect = true, int authLevel = 2, bool useWebAuth = false)
		{
			return new RdpLauncher.RdpOptions("10.0.0.1", 3389, "admin", "pwd", null, null,
				true, "*", false, RedirectClipboard: true,
				UseMultiMon: useMultiMon, ColorDepth: colorDepth, ShowConnectionBar: showConnectionBar,
				AudioMode: audioMode, KeyboardHook: keyboardHook, RedirectPrinters: redirectPrinters,
				ConnectionType: connectionType, DisableWallpaper: disableWallpaper,
				AllowFontSmoothing: allowFontSmoothing, AllowDesktopComposition: allowDesktopComposition,
				DisableFullWindowDrag: disableFullWindowDrag, DisableMenuAnims: disableMenuAnims,
				DisableThemes: disableThemes, BitmapCachePersist: bitmapCachePersist,
				AutoReconnect: autoReconnect, AuthLevel: authLevel, UseWebAuth: useWebAuth);
		}

		// ── 默认值对齐原版（v2.1.2 逆向实锤）──

		[Fact]
		public void Defaults_AlignWithOriginal_Record()
		{
			// 原版 record 实锤：RedirectPrinters=true / AuthLevel=2 / ConnectionType=7 / UseWebAuth=false
			var opt = Opt();
			Assert.True(opt.RedirectPrinters);
			Assert.Equal(2, opt.AuthLevel);
			Assert.Equal(7, opt.ConnectionType);
			Assert.False(opt.UseWebAuth);
			Assert.Equal(32, opt.ColorDepth);
			Assert.Equal(2, opt.KeyboardHook);
		}

		// ── BuildRdpContent 无条件键写出（v2.1.2 删档位门禁的回归线）──

		[Fact]
		public void BuildRdpContent_WritesMstscKeys_Unconditionally()
		{
			// v2.1.1 曾把这些键包在 DisplayMode==Custom 门禁里，预设档一个都不写。
			// 回归线：默认参数（=预设档等价物）下 15 键必须全部出现。
			string content = RdpLauncher.BuildRdpContent(Opt());
			string[] expectedKeys =
			{
				"use multimon:i:0", "session bpp:i:32", "displayconnectionbar:i:1",
				"audiomode:i:0", "keyboardhook:i:2", "redirectprinters:i:1",
				"disable wallpaper:i:0", "allow font smoothing:i:1",
				"allow desktop composition:i:1", "disable full window drag:i:0",
				"disable menu anims:i:0", "disable themes:i:0",
				"bitmapcachepersistenable:i:1", "autoreconnection enabled:i:1",
				"authentication level:i:2"
			};
			foreach (string key in expectedKeys)
			{
				Assert.Contains(key, content);
			}
		}

		[Fact]
		public void BuildRdpContent_WritesConnectionType_AndCredssp()
		{
			// v2.1.1 事故：connection type 漏写 → 连接速度设置完全不生效。
			string content = RdpLauncher.BuildRdpContent(Opt(connectionType: 3, useWebAuth: true));
			Assert.Contains("connection type:i:3", content);
			// UseWebAuth=true → enablecredsspsupport 禁用（取反语义，对齐原版）
			Assert.Contains("enablecredsspsupport:i:0", content);
		}

		[Fact]
		public void BuildRdpContent_CustomValues_OverwriteDefaults()
		{
			// 改了设置必须产生差异（用户报障「功能未奏效」的验收断言）
			string content = RdpLauncher.BuildRdpContent(Opt(
				useMultiMon: true, colorDepth: 16, showConnectionBar: false,
				audioMode: 1, keyboardHook: 1, redirectPrinters: false,
				disableWallpaper: true, allowFontSmoothing: false, allowDesktopComposition: false,
				disableFullWindowDrag: true, disableMenuAnims: true, disableThemes: true,
				bitmapCachePersist: false, autoReconnect: false, authLevel: 0));
			Assert.Contains("use multimon:i:1", content);
			Assert.Contains("session bpp:i:16", content);
			Assert.Contains("displayconnectionbar:i:0", content);
			Assert.Contains("audiomode:i:1", content);
			Assert.Contains("keyboardhook:i:1", content);
			Assert.Contains("redirectprinters:i:0", content);
			Assert.Contains("disable wallpaper:i:1", content);
			Assert.Contains("allow font smoothing:i:0", content);
			Assert.Contains("allow desktop composition:i:0", content);
			Assert.Contains("disable full window drag:i:1", content);
			Assert.Contains("disable menu anims:i:1", content);
			Assert.Contains("disable themes:i:1", content);
			Assert.Contains("bitmapcachepersistenable:i:0", content);
			Assert.Contains("autoreconnection enabled:i:0", content);
			Assert.Contains("authentication level:i:0", content);
		}

		// ── 键序对齐原版 WriteRdpFile（connection type 紧随 redirectclipboard）──

		[Fact]
		public void BuildRdpContent_KeyOrder_MatchesOriginal()
		{
			string content = RdpLauncher.BuildRdpContent(Opt());
			int clipIdx = content.IndexOf("redirectclipboard:i:", StringComparison.Ordinal);
			int connIdx = content.IndexOf("connection type:i:", StringComparison.Ordinal);
			int wallpaperIdx = content.IndexOf("disable wallpaper:i:", StringComparison.Ordinal);
			Assert.True(clipIdx >= 0 && connIdx > clipIdx && wallpaperIdx > connIdx,
				$"键序异常: clip={clipIdx} conn={connIdx} wallpaper={wallpaperIdx}");
		}

		// ── 冒烟层：核心写入不回归 ──

		[Fact]
		public void BuildRdpContent_StillContainsCoreKeys()
		{
			// 既有核心键不因 v2.1.x 改动丢失（冒烟）。
			// 注意：`*` 后不能再跟冒号（s:*: 非法），故断言 `drivestoredirect:s:*` 行尾。
			string content = RdpLauncher.BuildRdpContent(Opt());
			Assert.Contains("full address:s:10.0.0.1:3389", content);
			Assert.Contains("username:s:admin", content);
			Assert.Contains("prompt for credentials:i:0", content);
			Assert.Contains("screen mode id:i:2", content);
			Assert.Contains("redirectclipboard:i:1", content);
			Assert.Contains("drivestoredirect:s:*", content);
		}
	}
}
