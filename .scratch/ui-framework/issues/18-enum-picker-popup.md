Type: task
Status: resolved
Blocked by: 17

## Question

枚举弹窗统一改造 + 布局修复：

1. **EnumPickerPopup : PopupWindow**（自带点击外部关闭）：顶部搜索框 + 滚动列表
   - 普通枚举：单选即关，无类型名头部
   - flags 枚举：None 在首、每个条目带圆点（未选 ○ 灰 / 选中 ● 绿）、All 在尾、点击切换不关窗
2. **布局溢出修复**：inspector 根容器右 padding 避让滚动条；行内字段 flex-shrink:1、按钮 flex-shrink:0；行 overflow hidden
3. EnumElement 两种枚举都走按钮 + 弹窗；flags 按钮文本为组合值（A, B）

验收：编译零错误；普通枚举 3 行、点选即关且值写回；flags 弹窗 5 行（None/A/B/C/All）、切换与 All/None 语义正确、不关窗；行不再溢出。

## Answer

已实现并 bridge 实测全绿：

- **EnumPickerPopup : PopupWindowContent**（`Elements/EnumPickerPopup.cs`）：搜索框 + ScrollView 列表；普通枚举单选即关（无类型名头部）；flags：None 在首、All 在尾、圆点 ○ 灰 / ● 绿（eui-enum-circle-on）；点击外部自动关闭（PopupWindow 天然行为）。EnumSearchDropdown（AdvancedDropdown）已删除——2022.3 无 showHeader 且单选即关不满足 flags。
- **EnumElement**：普通/flags 统一按钮 + 弹窗；flags 组合值显示 "A, B"；空 flags 显示 "None"。
- **布局修复**：eui-inspector-root 右 padding 避让滚动条；行内 .eui-field flex-shrink:1、按钮 flex-shrink:0、行 overflow:hidden。

2022.3 弹窗 API 坑（实测）：`PopupWindow.Show(Rect, PopupWindowContent)` 是正解（不是继承 PopupWindow 窗体——那样 GetWindowSize 不存在）；同一帧内「关一个弹窗再开新弹窗」会延迟到下一帧初始化（editorWindow 为 null），真实使用无此场景。

实测：flags 行序 None,A,B,C,All ✓；A|B+C→A,B,C ✓；All→全选 ✓；None→清空 ✓；A 绿●/B 灰○ ✓；搜索 "a" 过滤剩 2 行 ✓；普通枚举 3 行无圆点、选 High 即关且回调写回 ✓。
