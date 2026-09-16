Type: task
Status: resolved
Blocked by: none

## Question

实现通用样式表 `Assets/Plugins/EditorUIFramework/Editor/USS/EditorUIFramework.uss`，提供 map Notes 约定的 `eui-` 类名基础样式：

- `eui-foldout` / `eui-header`：分组标题样式与容器间距
- `eui-row` / `eui-field-label`：字段行布局与标签对齐（label 宽 ~120px，观感对齐 Unity 默认 Inspector）
- `eui-list-item` / `eui-list-toolbar`：集合元素行、增删工具条（+/- 按钮样式）
- `eui-dict-row`：字典键值对行
- `eui-unsupported`：不支持类型的提示样式（灰字斜体）
- `eui-indent`：嵌套层级缩进

验收：ticket 10 的 CustomEditorBase 能通过 `AssetDatabase.LoadAssetAtPath` 加载该 USS 并挂到 rootVisualElement，各 element 取用类名后观感统一。

## Answer

已实现 `Assets/Plugins/EditorUIFramework/Editor/USS/EditorUIFramework.uss`：覆盖 eui-field / eui-row / eui-field-label / eui-foldout / eui-header / eui-indent / eui-list-item / eui-list-toolbar / eui-item-button / eui-dict-row / eui-dict-key / eui-unsupported / eui-error 全部约定类名，仅使用 2022.3 USS 子集属性（不用 gap）。
