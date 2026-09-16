Type: task
Status: resolved
Blocked by: 05

## Question

枚举与整体观感现代化：

1. **可搜索枚举**：非 flags 枚举从 EnumField 改为「按钮 + AdvancedDropdown」（自带搜索框，Add Component 同款交互）；[Flags] 枚举保留 EnumField 多选；选中走 CommitValue；显示名 NicifyVariableName 美化
2. **USS 现代化**：圆角（border-radius）、行悬停、主题无关 rgba 叠色（明暗主题通用）、枚举按钮拟输入框样式；解决「白底」观感

验收：编译零错误；DemoModel.quality 渲染为 eui-enum-button 且文本为当前值；flags 字段仍为 EnumField；dropdown BuildRoot 子项数 == 枚举成员数；OnItemSelected 回调写回模型。

## Answer

已实现并 bridge 实测通过：

- **EnumSearchDropdown**（`Elements/EnumSearchDropdown.cs`）：AdvancedDropdown 自带搜索框。**2022.3 的 AdvancedDropdown 在 `UnityEditor.IMGUI.Controls` 命名空间（CoreModule），选中回调叫 `ItemSelected`（不是 UIElements 版的 OnItemSelected）**——两处 API 坑均实测定位修复。BuildRoot 平铺枚举成员（NicifyVariableName 美化）。
- **EnumElement**：非 flags → eui-row（label + eui-enum-button 按钮，点击 dropdown.Show(button.worldBound)）；flags → 保留 EnumField 多选。选中走 CommitValue 并刷新按钮文本。
- **USS 现代化**：foldout 卡片化（圆角+淡底+细边）、列表/字典行圆角+悬停、item 按钮圆角、eui-enum-button 拟输入框（左对齐+边框+悬停/按下态）；全部用 rgba(128,128,128,…) 主题无关叠色，明暗主题通用。

实测：quality 渲染为搜索按钮（文本=当前值）✓、flags 仍 EnumField ✓、dropdown 3 子项 ✓、ItemSelected 回调得 High ✓、真实 Inspector 构建 + USS 加载零异常。
