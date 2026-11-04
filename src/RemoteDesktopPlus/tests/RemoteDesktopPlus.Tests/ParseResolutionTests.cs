using RemoteDesktopPlus.ViewModels;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 锁死分辨率解析。这是连接路径上唯一的字符串解析逻辑，最容易在重构中被改坏。
/// 原实现是 private static，为可测已提升为 public static（纯函数，无副作用）。
/// </summary>
public class ParseResolutionTests
{
	[Theory]
	[InlineData("1920*1080", 1920, 1080)]
	[InlineData("1920x1080", 1920, 1080)]
	[InlineData("1920X1080", 1920, 1080)]
	[InlineData("1280*720", 1280, 720)]
	[InlineData("3840*2160", 3840, 2160)]
	[InlineData("1024*768", 1024, 768)]
	public void ParseResolution_ValidFormats_ReturnsDimensions(string input, int expectedW, int expectedH)
	{
		var (w, h) = MainViewModel.ParseResolution(input);
		Assert.Equal(expectedW, w);
		Assert.Equal(expectedH, h);
	}

	[Theory]
	[InlineData("全屏")]
	[InlineData("")]
	[InlineData(null)]
	public void ParseResolution_FullScreenOrEmpty_ReturnsNullPair(string? input)
	{
		var (w, h) = MainViewModel.ParseResolution(input!);
		Assert.Null(w);
		Assert.Null(h);
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("1920")]
	[InlineData("1920*")]
	[InlineData("*1080")]
	[InlineData("1920*1080*60")]
	[InlineData("19a0*1080")]
	[InlineData("1920*10x80")]
	[InlineData("-1920*1080")]
	[InlineData("0*1080")]
	[InlineData("1920*0")]
	[InlineData("-1920*-1080")]
	public void ParseResolution_Malformed_ReturnsNullPair(string input)
	{
		var (w, h) = MainViewModel.ParseResolution(input);
		Assert.Null(w);
		Assert.Null(h);
	}

	[Theory]
	[InlineData(" 1920*1080")]
	[InlineData("1920*1080 ")]
	[InlineData("\t1920x1080\t")]
	public void ParseResolution_SurroundingWhitespace_IsTolerated(string input)
	{
		// 反编译原实现依赖 int.TryParse 自带的前导空白容忍，但尾部空白会失败；
		// 这里统一为 Trim，行为更可预期。
		var (w, h) = MainViewModel.ParseResolution(input);
		Assert.Equal(1920, w);
		Assert.Equal(1080, h);
	}

	[Fact]
	public void ParseResolution_ResultIsConsistentWithRdpLauncherFullScreenGuard()
	{
		// 解析出 (null,null) 时必须落到 .rdp 的全屏分支，两者不能互相矛盾。
		var (w, h) = MainViewModel.ParseResolution("全屏");
		string content = RemoteDesktopPlus.Services.RdpLauncher.BuildRdpContent(
			new RemoteDesktopPlus.Services.RdpLauncher.RdpOptions("h", 3389, "u", "p", w, h, false));
		Assert.Contains("screen mode id:i:2", content);
	}

	[Fact]
	public void ParseResolution_Maximized_UsesSuppliedScreenWorkArea()
	{
		// 「最大化」档要给 smart sizing 一个与工作区一致的初始内容尺寸，
		// 给定 1920x1040 工作区就必须算出该尺寸，而不是退回某个写死的分辨率档。
		var (w, h) = MainViewModel.ParseResolution(
			MainViewModel.MaximizedResolutionDisplayName,
			() => (1920, 1040));

		Assert.Equal(1920, w);
		Assert.Equal(1040, h);
	}

	[Fact]
	public void ParseResolution_Maximized_WithoutScreenInfo_FallsBackToFullScreen()
	{
		// 取不到屏幕尺寸（无 UI 线程）时必须退回全屏，绝不能返回 0x0 —— 后者会被
		// BuildRdpContent 当作非法尺寸，行为虽一致但语义上不可依赖。
		var (w, h) = MainViewModel.ParseResolution(MainViewModel.MaximizedResolutionDisplayName);
		Assert.Null(w);
		Assert.Null(h);
	}

	[Fact]
	public void ParseResolution_Maximized_WithZeroScreenInfo_FallsBackToFullScreen()
	{
		var (w, h) = MainViewModel.ParseResolution(MainViewModel.MaximizedResolutionDisplayName, () => (0, 0));
		Assert.Null(w);
		Assert.Null(h);
	}

	[Theory]
	[InlineData("最大化（铺满可用区域，不遮任务栏）", true)]
	[InlineData(" 最大化（铺满可用区域，不遮任务栏） ", true)]
	[InlineData("全屏", false)]
	[InlineData("1920*1080", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void IsMaximizedResolution_DistinguishesFromOtherModes(string? input, bool expected)
	{
		// 这条判据决定「远程桌面窗口是否以最大化打开」，必须与「全屏」「固定像素」
		// 严格区分 —— 三者是不同形态，混判会让窗口该最大化时却盖住任务栏。
		Assert.Equal(expected, MainViewModel.IsMaximizedResolution(input));
	}
}
