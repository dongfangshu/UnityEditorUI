Type: task
Status: resolved
Blocked by: none

## Question

实现 FieldData 抽象（命名空间 EditorUIFramework，Editor 程序集），统一「字段 / 列表元素 / 字典项 / 根对象」四种取值来源：

抽象基类 `FieldData`：
- `string Name`（显示名，字段名美化）
- `Type BaseType`（工厂分派依据）
- `object GetValue()` / `void SetValue(object value)`（构造时捕获 owner）
- 可选父链（父 FieldData），供 struct 嵌套时把装箱实例回写父级

派生：
- `ReflectionFieldData(System.Reflection.FieldInfo, object owner)`
- `ListItemData(IList list, int index)`
- `DictEntryData(IDictionary dict, object key)`
- `RootFieldData(object target)`：GetValue 返回 target 本身，SetValue 不支持

同票建立 `EditorUIFramework.Editor.asmdef`（Assets/Plugins/EditorUIFramework/Editor/ 下）。

验收：四类 FieldData 对引用类型读写正确；ListItemData/DictEntryData 对 struct 元素读写正确（list[i]=v / dict[k]=v）。

## Answer

已实现 `Assets/Plugins/EditorUIFramework/Editor/FieldData.cs`：抽象基类 FieldData（Name/BaseType/GetValue/SetValue，构造捕获宿主）+ ReflectionFieldData / ListItemData / DictEntryData / RootFieldData 四派生。DictEntryData 的 Name 置空（键由 DictElement 行首单独展示）。asmdef：EditorUIFramework.Editor（includePlatforms=Editor，autoReferenced=true）。struct 嵌套回写不走 FieldData 父链，改由 ObjectElement.OnValueChanged 冒泡实现（见 ticket 09）。
