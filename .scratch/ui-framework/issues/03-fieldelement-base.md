Type: task
Status: resolved
Blocked by: 02

## Question

实现 `FieldElement` 抽象基类（继承 VisualElement），确立两阶段契约：

- 第一阶段（构造）：`FieldElement(FieldData data)` 仅存储数据，不建视觉。
- 第二阶段（组装）：`abstract Setup()` —— 构建自身视觉、用 `FieldData.GetValue()` 初始化控件值、挂 `eui-` 类名；复合 element（Object/List/Dict）在 Setup 内经工厂构造子 element 并调用子 Setup()。整树构造成功后由 CustomEditorBase 调用**根**的 Setup() 级联组装。
- 控件值变更回调统一走：`FieldData.SetValue` + `Undo.RecordObject` + `EditorUtility.SetDirty`。
- 基类提供 label 工具（Name 美化）与 Unsupported 提示辅助。

验收：契约稳定，ticket 04 工厂与 05-09 各 element 均按此实现。

## Answer

已实现 `FieldElement.cs`：抽象基类 FieldElement : VisualElement（构造存 FieldData + 挂 eui-field 类；abstract Setup()；CommitValue 统一走 Undo.RecordObject → SetValue → SetDirty → OnValueChanged 冒泡；ParentFieldElement 冒泡链；UndoTarget 注入；NameUtil.Nicify 字段名美化）。
