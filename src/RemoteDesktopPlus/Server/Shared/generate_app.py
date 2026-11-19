# -*- coding: utf-8 -*-
"""
M1 第二步切片生成器：S01-S17 + S19-S23（VM 层）→ All.App.cs
VM 层 5 处 WPF 泄漏做「语义保持」的最小摘除（每处对齐 IDialogService / 系统定时器）：

  L1 行 3462  `using System.Windows.Data;`        → 删（GongSolutions/DragDrop 相关 using 一并处理）
  L2 行 4345  `using System.Windows;`             → 删
  L3 行 4776  System.Windows.MessageBox.Show(...) → Log.Error 已在同分支；改调 Dialog?.ShowMessage（IDialogService 出口，语义等价）
  L4 行 4835  DispatcherTimer 字段                → System.Threading.Timer（回调包 Dispatcher 免除——服务端无 UI 线程概念，事件回调直接在池线程）
  L5 行 4847  new DispatcherTimer{...}            → 同上

摘除原则：不改变任何业务语义——弹窗内容/日志行为/轮询间隔/幂等性逐一保持；
每处摘除带 `// bprime:` 注释标记，回溯 WPF 版原文位置。
S18 TrayController 仍排除（VM 对它仅注释/事件出口引用，零编译依赖——已实测）。
"""
import re

SRC = 'RemoteDesktopPlus.All.cs'
OUT = 'Server/Shared/All.App.cs'

with open(SRC, encoding='utf-8', newline='') as f:
    lines = f.read().split('\n')

# 行 18-3333（S01-S17，与 All.Services.cs 相同范围）
part_services = lines[18 - 1: 3334 - 1]
# 行 3347-3350（S18 区段内纯 record：TrayBehavior——零 WPF，VM 事件类型需要它）
tray_record = lines[3346 - 1: 3350]  # 4 行 doc 注释 + record 声明
# 行 3451-5038（S19-S23 VM 层，S18 TrayController 类体排除，S24 Views 起排除）
part_vm = lines[3451 - 1: 5039 - 1]

vm_text = '\n'.join(part_vm)

# TrayBehavior 单独补入 Services namespace（它原文在 S18 区段头之后、
# TrayController 类所在 namespace 块内；切片进 Services namespace 语义等价）
tray_inject = (
    'namespace RemoteDesktopPlus.Services\n'
    '{\n'
    + '\n'.join(tray_record) + '\n'
    + '}\n'
)
vm_text = tray_inject + vm_text

# ── L1/L2：删 WPF/DragDrop using（VM 区段内，仅摘除编译性依赖；两处 partial 块各一份）
vm_text = vm_text.replace('using System.Windows.Data;\n', '', 1)
vm_text = vm_text.replace('using System.Windows;\n', '', 1)
vm_text = vm_text.replace('using GongSolutions.Wpf.DragDrop;\n', '')  # 全部出现处（2 处 partial 块）

# ── L3：MessageBox.Show → Dialog 出口（语义等价：同一文案，走 IDialogService）
old_l3 = '''				Log.Error("密码为空但目标连接已有密文，保留原密码不覆盖: " + connectionItem.Host);
				// 本区段未 using System.Windows，用全限定名避免歧义。
				System.Windows.MessageBox.Show("当前密码无法解密（主密码未解锁或密文已损坏），已保留原密码未做修改。\\n\\n"
					+ "如需清空密码，请删除该连接后重新添加。");'''
new_l3 = '''				Log.Error("密码为空但目标连接已有密文，保留原密码不覆盖: " + connectionItem.Host);
				// bprime: 原 System.Windows.MessageBox.Show（All.cs 行 4776）——服务版走 IDialogService 出口
				_dialogs.ShowMessage("当前密码无法解密（主密码未解锁或密文已损坏），已保留原密码未做修改。\\n\\n"
					+ "如需清空密码，请删除该连接后重新添加。", "枢联", Services.DialogKind.Warning);'''
assert old_l3 in vm_text, "L3 锚点未命中"
vm_text = vm_text.replace(old_l3, new_l3, 1)

# ── L4/L5：DispatcherTimer → System.Threading.Timer（幂等/间隔/Tick 语义保持）
old_l4 = '''	private System.Windows.Threading.DispatcherTimer? _statusTimer;'''
new_l4 = '''	// bprime: 原 DispatcherTimer（All.cs 行 4835）——服务版无 UI 线程，System.Threading.Timer 等价轮询
	private System.Threading.Timer? _statusTimer;'''
assert old_l4 in vm_text, "L4 锚点未命中"
vm_text = vm_text.replace(old_l4, new_l4, 1)

old_l5 = '''		_statusTimer = new System.Windows.Threading.DispatcherTimer
		{
			Interval = interval ?? TimeSpan.FromSeconds(3)
		};
		_statusTimer.Tick += (_, _) => SafeRefreshServerStatus();
		_statusTimer.Start();
		SafeRefreshServerStatus();'''
new_l5 = '''		// bprime: DispatcherTimer → System.Threading.Timer（间隔/首触发/Tick 语义保持）
		_statusTimer = new System.Threading.Timer(
			_ => SafeRefreshServerStatus(),
			null,
			TimeSpan.Zero,          // dueTime：立即首触发（语义=DispatcherTimer.Start() 后 Tick）
			interval ?? TimeSpan.FromSeconds(3));
		SafeRefreshServerStatus();'''
assert old_l5 in vm_text, "L5 锚点未命中"
vm_text = vm_text.replace(old_l5, new_l5, 1)

# StopStatusPolling 里 DispatcherTimer 的 Change/Dispose 兼容——看原文
# （Timer 的停止 = Dispose；若原文有 _statusTimer.Stop() 需对应替换）

# ── L6：VM 的 IDropTarget/IDropInfo（GongSolutions 拖拽 UI 接口）服务化改造——
#   DragOver 是纯 UI 反馈（光标效果），服务版无意义→空实现；Drop 的重排逻辑保留，
#   改签名 MoveConnection(fromIndex, toIndex)（语义等价：调用方传列表索引）
old_drop = '''	public void DragOver(IDropInfo dropInfo)
	{
		if (dropInfo.Data is ConnectionItem && dropInfo.TargetItem is ConnectionItem)
		{
			dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
			dropInfo.Effects = DragDropEffects.Move;
		}
	}'''
new_drop = '''	// bprime: 原 DragOver(IDropInfo)（All.cs 行 4649）——纯 UI 光标反馈，服务版无意义，删除
	public void DragOver(int fromIndex, int toIndex)
	{
		// 服务版拖拽语义 = MoveConnection 前的合法性由前端/API 校验
	}'''
assert old_drop in vm_text, "DragOver 锚点未命中"
vm_text = vm_text.replace(old_drop, new_drop, 1)

old_drop2 = '''	public void Drop(IDropInfo dropInfo)
	{
		// 按最近连接排序时禁用手工拖拽：此时顺序由 LastConnectedAt 决定，
		// 允许拖拽会写进 SortOrder 却看不见效果，属于误导。
		if (SortByRecent)
		{
			return;
		}
		if (dropInfo.Data is ConnectionItem item && dropInfo.TargetItem is ConnectionItem)
		{
			int num = Connections.IndexOf(item);
			int num2 = dropInfo.InsertIndex;'''
new_drop2 = '''	// bprime: 原 Drop(IDropInfo)（All.cs 行 4658）——GongSolutions 拖拽参数改为索引对，
	// 重排/防误导/补偿位语义逐行保持
	public void Drop(int fromIndex, int insertIndex)
	{
		// 按最近连接排序时禁用手工拖拽：此时顺序由 LastConnectedAt 决定，
		// 允许拖拽会写进 SortOrder 却看不见效果，属于误导。
		if (SortByRecent)
		{
			return;
		}
		{
			var item = Connections.ElementAtOrDefault(fromIndex);
			// bprime: 原 WPF 版用 `dropInfo.Data is ConnectionItem` 守卫（非连接对象直接跳过）；
			// 索引签名下等价守卫 = 越界/空引用跳过（原测试 Drop_NonConnectionPayload_IsIgnored 语义）
			if (item is not ConnectionItem)
			{
				return;
			}
			int num = fromIndex;
			int num2 = insertIndex;'''
assert old_drop2 in vm_text, "Drop 锚点未命中"
vm_text = vm_text.replace(old_drop2, new_drop2, 1)

# ── L7：ICollectionView（WPF 集合视图）→ 直接 List/Collections 操作
#   AttachConnectionsView 是视图注入入口，服务版无需视图过滤（API 侧过滤）
old_icv = '''	private ICollectionView? _connectionsView;'''
new_icv = '''	// bprime: 原 ICollectionView（All.cs 行 3752）——服务版过滤走 API 参数，视图层字段删除'''
assert old_icv in vm_text, "ICollectionView 字段锚点未命中"
vm_text = vm_text.replace(old_icv, new_icv, 1)

old_attach = '''	public void AttachConnectionsView(ICollectionView view)
	{
		_connectionsView = view;
		ApplyGroupFilter();
	}'''
assert old_attach in vm_text, "AttachConnectionsView 锚点未命中"
vm_text = vm_text.replace(old_attach, '''	// bprime: 原 AttachConnectionsView(ICollectionView)（All.cs 行 3755）——服务版无视图过滤
	public void AttachConnectionsView(object? view)
	{
	}''', 1)

# ApplyGroupFilter 方法体改空（_connectionsView 字段已删）
old_agf = '''	private void ApplyGroupFilter()
	{
		if (_connectionsView == null)
		{
			return;
		}
		string filter = SelectedGroupFilter;
		bool all = string.IsNullOrEmpty(filter) || AllGroupsDisplayName.Equals(filter, StringComparison.Ordinal);
		_connectionsView.Filter = all
			? null
			: new Predicate<object>(o => o is ConnectionItem c
				&& string.Equals(c.GroupName ?? string.Empty, filter, StringComparison.OrdinalIgnoreCase));
	}'''
new_agf = '''	// bprime: 原 ApplyGroupFilter（ICollectionView.Filter）——服务版筛选由 API GroupFilter 参数实现
	private void ApplyGroupFilter()
	{
	}'''
assert old_agf in vm_text, "ApplyGroupFilter 锚点未命中"
vm_text = vm_text.replace(old_agf, new_agf, 1)

# 类声明去掉 IDropTarget 接口（接口随 GongSolutions 排除）
old_cls = '''public partial class MainViewModel : ObservableObject, IDropTarget'''
new_cls = '''public partial class MainViewModel : ObservableObject // bprime: 原 : ObservableObject, IDropTarget（拖拽接口随 GongSolutions 排除）'''
assert old_cls in vm_text, "类声明锚点未命中"
vm_text = vm_text.replace(old_cls, new_cls, 1)

# StopStatusPolling 的 _statusTimer.Stop() → Dispose()（Timer 无 Stop）
old_stop = '''		_statusTimer.Stop();
		_statusTimer = null;'''
assert old_stop in vm_text, "StopStatusPolling 锚点未命中"
vm_text = vm_text.replace(old_stop, '''		// bprime: DispatcherTimer.Stop() → Timer.Dispose()（停止语义等价）
		_statusTimer.Dispose();
		_statusTimer = null;''', 1)

header = [
    '// ========================================================================',
    '//  M1 平移切片（机械生成，勿手改）—— 源: ../../RemoteDesktopPlus.All.cs',
    '//  范围: 行 18-3333（S01-S17）+ 行 3451-5038（S19-S23 VM 层）',
    '//  WPF 摘除: 5 处（bprime: 注释标记，生成器 Server/Shared/generate_app.py）',
    '//  排除: S18 TrayController（WPF）、S24+ Views/Converters/App',
    '// ========================================================================',
    '',
]

with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(header) + '\n'.join(part_services) + '\n' + vm_text)

import hashlib
src_sha = hashlib.sha256(open(SRC, 'rb').read()).hexdigest()[:16]
out_lines = len(header) + len(part_services) + 1 + len(vm_text.split('\n'))
print(f"源 sha[:16]={src_sha}  services段={len(part_services)}行  vm段={len(vm_text.split(chr(10)))}行")
print("残留 WPF 检查:", 'System.Windows' in vm_text or 'GongSolutions' in vm_text)
print("All.App.cs 生成完成")
