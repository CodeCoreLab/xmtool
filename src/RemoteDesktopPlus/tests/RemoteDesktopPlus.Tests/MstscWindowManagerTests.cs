using System.Diagnostics;
using RemoteDesktopPlus.Services;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// mstsc 窗口管理器的纯逻辑（尺寸钉死 / 居中坐标 / 归一化），
/// 不触碰 Win32 与真实进程，CI 可跑。
/// </summary>
public class MstscWindowManagerTests
{
	[Theory]
	// 用户正常态截图：1920×1200 客户区 + 27px 标题栏、无侧边框 → 外框 1920×1227
	[InlineData(1920, 1200, 0, 27, 1920, 1227)]
	// 标准可调边框窗口
	[InlineData(1920, 1200, 16, 31, 1936, 1231)]
	// 帧为负（测量异常）→ 钳 0，只按桌面分辨率算
	[InlineData(1920, 1200, -5, -3, 1920, 1200)]
	public void ComputeTargetOuterSize_DesktopPlusFrame(int dw, int dh, int fw, int fh, int ew, int eh)
	{
		var (w, h) = MstscWindowManager.ComputeTargetOuterSize(dw, dh, fw, fh);
		Assert.Equal(ew, w);
		Assert.Equal(eh, h);
	}

	[Fact]
	public void ComputeCenteredPosition_CentersInWorkArea()
	{
		var (x, y) = MstscWindowManager.ComputeCenteredPosition(0, 0, 2560, 1440, 1936, 1231);
		Assert.Equal((2560 - 1936) / 2, x);
		Assert.Equal((1440 - 1231) / 2, y);
	}

	[Fact]
	public void ComputeCenteredPosition_WindowLargerThanWorkArea_KeepsTitleBarVisible()
	{
		// 1920×1200 会话窗口放到 1080 高的工作区：纯居中会把标题栏顶出屏幕（y 为负），
		// 必须钳回工作区原点，否则用户永远拖不动窗口。
		var (x, y) = MstscWindowManager.ComputeCenteredPosition(0, 0, 1920, 1080, 1936, 1231);
		Assert.Equal(0, x);
		Assert.Equal(0, y);
	}

	[Theory]
	[InlineData(1920, 1200, 1920, 1200)] // 双正 → 原样返回
	[InlineData(null, 1200, null, null)] // 缺宽 → 全屏
	[InlineData(1920, null, null, null)] // 缺高 → 全屏
	[InlineData(0, 1200, null, null)]    // 零宽 → 全屏（与 BuildRdpContent 同约定）
	[InlineData(1920, 0, null, null)]    // 零高 → 全屏
	[InlineData(-1, -1, null, null)]     // 负值 → 全屏
	[InlineData(null, null, null, null)]
	public void NormalizeDesktopSize_RequiresBothPositive(int? w, int? h, int? ew, int? eh)
	{
		var (rw, rh) = MstscWindowManager.NormalizeDesktopSize(w, h);
		Assert.Equal(ew, rw);
		Assert.Equal(eh, rh);
	}

	[Fact]
	public void Track_NullProcess_IsNoOp()
	{
		// 测试替身 / 启动失败路径返回 null：不得建会话、不得装钩子
		int before = MstscWindowManager.ActiveSessionCount;
		MstscWindowManager.Track(null, 1920, 1200, redirectClipboard: true);
		Assert.Equal(before, MstscWindowManager.ActiveSessionCount);
	}

	[Fact]
	public void Track_NoSizeAndNoClipboard_IsNoOp()
	{
		// 进程句柄非空，但全屏 + 关闭剪贴板重定向：没有可管理的行为，不建会话
		using Process self = Process.GetCurrentProcess();
		int before = MstscWindowManager.ActiveSessionCount;
		MstscWindowManager.Track(self, null, null, redirectClipboard: false);
		Assert.Equal(before, MstscWindowManager.ActiveSessionCount);
	}
}
