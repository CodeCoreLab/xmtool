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
			new RemoteDesktopPlus.Services.RdpLauncher.RdpOptions("h", 3389, "u", "p", w, h, false, "", false));
		Assert.Contains("screen mode id:i:2", content);
	}
}
