using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// <see cref="IDropInfo"/> 的最小替身，只为把拖拽的「数据 / 目标 / 插入位置」喂给
/// <see cref="RemoteDesktopPlus.ViewModels.MainViewModel.Drop"/>。
/// <para>
/// 真实的 <c>DropInfo</c> 由 GongSolutions.Wpf.DragDrop 在 UI 线程构造、订阅真实鼠标事件，
/// 单元测试里无法产生，故这里只实现被测代码真正读取的三个成员（Data / TargetItem / InsertIndex），
/// 其余成员按接口契约给最小实现，不可空引用一律用 <c>null!</c> 显式标注「测试中不会访问」。
/// </para>
/// </summary>
internal sealed class StubDropInfo : IDropInfo
{
	public StubDropInfo(object data, object? targetItem, int insertIndex)
	{
		Data = data;
		TargetItem = targetItem;
		InsertIndex = insertIndex;
	}

	public object Data { get; set; }

	public object TargetItem { get; }

	public int InsertIndex { get; }

	// —— 以下成员 MainViewModel.Drop 并不读取 ——

	public IDragInfo DragInfo => null!;

	public Point DropPosition => default;

	public Type DropTargetAdorner { get; set; } = null!;

	public DragDropEffects Effects { get; set; }

	public int UnfilteredInsertIndex => InsertIndex;

	public IEnumerable TargetCollection => null!;

	public CollectionViewGroup TargetGroup => null!;

	public ScrollViewer TargetScrollViewer => null!;

	public ScrollingMode TargetScrollingMode => ScrollingMode.None;

	public UIElement VisualTarget => null!;

	public UIElement VisualTargetItem => null!;

	public Orientation VisualTargetOrientation => Orientation.Vertical;

	public FlowDirection VisualTargetFlowDirection => FlowDirection.LeftToRight;

	public string DestinationText { get; set; } = string.Empty;

	public string EffectText { get; set; } = string.Empty;

	public RelativeInsertPosition InsertPosition => RelativeInsertPosition.BeforeTargetItem;

	public DragDropKeyStates KeyStates => DragDropKeyStates.None;

	public bool NotHandled { get; set; }

	public bool IsSameDragDropContextAsSource => true;

	public EventType EventType => EventType.Bubbled;
}
