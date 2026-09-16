Type: map
Status: active

# UIToolkit 编辑器 UI 框架

## Destination

v1 落地：CustomEditorBase（继承 Editor）在创建时反射目标对象构建对象树 → 类型工厂按 FieldData.BaseType 派生 FieldElement → 构造成功后调用根 Setup() 组装；通用 USS 样式表；Unity 版 .gitignore；示例 MonoBehaviour 覆盖全部已支持类型并可渲染可编辑；用 /unity-bridge skill 编译验证通过。

## Notes

- 本 effort **覆盖执行**（不只决策）：票据即实施切片，逐个 claim → 实施 → resolve。
- 域：Unity 2022.3.60f1 Editor + UIToolkit（com.unity.modules.uielements）。
- 代码位置：Assets/Plugins/EditorUIFramework/；Editor 程序集 EditorUIFramework.Editor.asmdef；命名空间 EditorUIFramework。
- 编译验证统一用 /unity-bridge skill（用户指定）。
- USS 类名约定：统一前缀 `eui-`（eui-foldout / eui-header / eui-row / eui-field-label / eui-list-item / eui-list-toolbar / eui-dict-row / eui-unsupported / eui-indent）。
- 值写回：控件变更时 Undo.RecordObject(target) + EditorUtility.SetDirty(target)；struct 嵌套沿父链回写装箱实例。
- v1 单目标编辑（不加 CanEditMultipleObjects）；纯反射，不走 SerializedProperty。

已定基础决策（charting 期 grilling，全体票据的前提）：

- FieldInfo 抽象 = 抽象基类 FieldData（Name / BaseType / GetValue / SetValue），派生 ReflectionFieldData / ListItemData / DictEntryData / RootFieldData。
- 嵌套自定义 class/struct → 兜底 ObjectElement 递归展开（根节点本身即 ObjectElement）。
- List/Dictionary = 完整编辑：List 增删+排序；Dict 增删键值对，key 限可比较基元且创建后不可改。
- 类型范围 v1：基元（bool/int/float/double/long/string）+ enum + UnityEngine.Object 引用 + Vector2/Vector3 + Color + List<T> + Dictionary<K,V> + 嵌套自定义类型。
- 目的地 = 框架 + 示例 MonoBehaviour 验证。

## Decisions so far

- [01-common-uss](issues/01-common-uss.md) — USS 落地 Editor/USS/EditorUIFramework.uss，eui- 类名全集，仅用 2022.3 USS 子集
- [02-fielddata-abstraction](issues/02-fielddata-abstraction.md) — FieldData 抽象 + Reflection/ListItem/DictEntry/Root 四派生；struct 回写改由 ObjectElement 冒泡负责
- [12-unity-gitignore](issues/12-unity-gitignore.md) — 根目录标准 Unity.gitignore，.agents/.scratch 保持跟踪
- [03-fieldelement-base](issues/03-fieldelement-base.md) — FieldElement : VisualElement 两阶段契约 + CommitValue（Undo→写回→标脏→冒泡）
- [04-type-factory](issues/04-type-factory.md) — 工厂分派链：注册表 > 谓词 > 内建（enum/Obj/数组排除/Dict/List/Object 兜底）> Unsupported
- [05-primitive-elements](issues/05-primitive-elements.md) — 基元+enum+Obj引用+Color 九种 element + UnsupportedElement
- [06-vector-elements](issues/06-vector-elements.md) — Vector2/Vector3 element
- [07-list-element](issues/07-list-element.md) — 受控行容器方案（弃 ListView 虚拟化）：增/删/▲▼排序/改值，结构变更整树刷新
- [08-dict-element](issues/08-dict-element.md) — 键只读+值工厂递归+TempValueData 新键编辑（不记 Undo），重复键行内报错
- [09-object-element](issues/09-object-element.md) — 继承链字段收集规则 + struct 装箱回写冒泡；根节点不包 Foldout
- [10-customeditorbase](issues/10-customeditorbase.md) — CreateInspectorGUI 装配（USS→RootFieldData→工厂→根 Setup），undoRedoPerformed 重建
- [11-demo-and-compile-verify](issues/11-demo-and-compile-verify.md) — 编译零错误零警告；端到端验证通过（205 元素/6 Foldout/6 类值回写）；工厂特判 RootFieldData→ObjectElement；2022.3 API 坑见票据
- [13-datahost-inspector](issues/13-datahost-inspector.md) — 纯数据对象上 Inspector：Runtime 程序集（[DataObject] + DataHost[SerializeReference]）+ CreateRootData/UndoHost/OnCreateHeader 钩子 + DataHostEditor 类型下拉（确认弹窗）；验证全通过
- [14-odin-attributes](issues/14-odin-attributes.md) — Odin/Unity 特性兼容层：OdinCompat 零依赖反射读取（Sirenix+UnityEngine 双候选）；Setup 模板方法化；LabelText/Tooltip/HideLabel/ReadOnly/Range/MinMax/TextArea/Title/Header/ShowIf/HideIf/Button 全实测通过
- [15-lua-host](issues/15-lua-host.md) — LuaHost：EmmyLua 注解解析 + LuaFieldData 值袋（字符串↔类型转换）+ 工厂谓词接 LuaSchemaElement；目录浏览+文件下拉+保值同步；v1/v2 写回实测通过
- [16-lua-collections](issues/16-lua-collections.md) — lua 泛型集合：LuaTypeMap 递归泛型解析 + LuaValue 字面量往返（Runtime）+ List/DictElement 写回钩子 + LuaHost.GetLuaData() 导出 lua 表
- [17-searchable-enum-modern-ui](issues/17-searchable-enum-modern-ui.md) — 可搜索枚举（AdvancedDropdown 在 IMGUI.Controls、回调名 ItemSelected）+ USS 现代化（圆角/悬停/rgba 主题无关叠色）
- [18-enum-picker-popup](issues/18-enum-picker-popup.md) — 枚举弹窗统一改自绘 EnumPickerPopup（PopupWindowContent）：搜索+单选即关/flags 多选圆点+None/All；替代 AdvancedDropdown；布局防溢出修复

**v1 全部票据已解决，目的地达成。**

## Not yet specified

- T[] 数组支持（机制同 List<T>，增删需 Array.Resize）。
- 多目标编辑（CanEditMultipleObjects）。
- 更多特性：FoldoutGroup/TabGroup 分组、ValidateInput/Required 校验、ShowInInspector（属性）、ValueDropdown、ColorPalette、EnumToggleButtons、带参 Button。
- HashSet / SortedDictionary 等其他集合类型。
- 属性（Property）展示与校验装饰。

## Out of scope

- 运行时（非编辑器）UI。
- 替换 Unity 序列化系统。
- IMGUI 兼容层。
