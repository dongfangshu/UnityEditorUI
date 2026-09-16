Type: task
Status: resolved
Blocked by: 04

## Question

Odin 常用特性兼容层（attribute 驱动 UI，雾区毕业）：

- **零编译期依赖**：按全名反射读取 `Sirenix.OdinInspector.*` 特性（OdinCompat），Odin 不存在时全部落空、框架照常工作
- 首批支持：LabelText（覆盖显示名）、Tooltip、HideLabel、ReadOnly（禁用编辑）、Range（int/float → SliderInt/Slider）、MinValue/MaxValue（数值夹取）、TextArea（string 多行）、Title（字段上方加标题）、ShowIf/HideIf（同对象 bool 字段/属性/无参方法，支持 `!` 前缀）、Button（无参方法 → 按钮，Invoke 走 Undo + struct 回写链）
- FieldElement 契约演进：Setup() 变为模板方法（OnSetup 建控件 → ApplyAttributes 统一修饰）；元素设置 MainControl；条件可见性注册到根、任何值变更后重估

明确不做（回雾区）：FoldoutGroup/TabGroup 分组、ValidateInput/Required、ShowInInspector（属性）、ValueDropdown、ColorPalette、EnumToggleButtons、带参 Button。

验收：编译零错误；DemoModel 挂特性后面板实测：label 覆盖生效、Slider 出现、ReadOnly 禁用、ShowIf 随开关显隐、Title/多行/Button 渲染。

## Answer

已实现并全部实测通过（bridge 面板挂载验证）：

- **OdinCompat**（`Editor/OdinCompat.cs`）：按全名反射读取特性，候选名制——Sirenix.OdinInspector.* 与 UnityEngine.* 双覆盖。**关键发现：Odin 设计上直接复用 Unity 内建特性**，`[Range]/[Tooltip]/[TextArea]` 在用户代码里解析为 UnityEngine.RangeAttribute(min/max 小写字段) 等；LabelText/ReadOnly/ShowIf/MinValue 等才是 Sirenix 自己的。Read 支持属性/字段双读、多候选名（TooltipText|tooltip、Min|min）。
- **契约演进**：FieldElement.Setup() 改为模板方法（OnSetup 建控件 → ApplyAttributes 修饰）；元素设置 MainControl；DisplayName 变为特性感知（LabelText 覆盖 / HideLabel 置空）。
- **条件可见性**：ShowIf/HideIf（同对象 bool 字段/属性/无参方法，支持 ! 前缀）注册到根节点，任何 CommitValue 冒泡到根时全量重估。
- **Range**：int → SliderInt，float → Slider；**MinValue/MaxValue**：CommitValue 前 ClampNumeric 夹取（dynamic 实现）；**TextArea/Multiline**：TextField.multiline；**Title/Header**：字段上方 eui-header 标签；**Button**：ObjectElement 扫描无参方法生成按钮，调用走 Undo + struct 回写链。

实测记录：生命值 Slider(0-100, tooltip)✓、ReadOnly 禁用✓、HideLabel+TextArea 多行✓、Title 标题✓、ShowIf/HideIf 双向显隐（含开关往返）✓、Min/Max 夹取（-5→0、12345→9999）✓、Button 渲染✓。期间排掉一个测试方法学坑：前一轮测试把 showAdvanced 持久化为 true 导致「初始隐藏」假失败，重置状态后双向全通。
