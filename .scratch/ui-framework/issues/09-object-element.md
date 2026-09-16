Type: task
Status: resolved
Blocked by: 04

## Question

实现 `ObjectElement`（兜底 element，处理可序列化自定义 class/struct，也是整棵对象树的根）：

- Foldout（显示 Name）+ `eui-indent` 容器
- 反射 BaseType 实例字段：public 字段 + 带 [SerializeField] 的非 public 字段；沿继承链收集；排除 static/const/[HideInInspector]
- 每字段 → `ReflectionFieldData` → 工厂构造子 element → 子 Setup()
- struct（BaseType.IsValueType）时：子值变更后把当前装箱实例经自身 FieldData.SetValue 回写父级，保证嵌套 struct 编辑可持久化

验收：嵌套自定义 class 与 struct 字段可显示可编辑；struct 嵌套编辑回写链不断。

## Answer

已实现 `Elements/ObjectElement.cs`：根（RootFieldData）直接作容器，非根包 Foldout + eui-indent。CollectFields 沿继承链收集（基类在前）：public（排除 [NonSerialized]）或 [SerializeField] 非 public；排除 static/const/[HideInInspector]/[CompilerGenerated]。每字段 → ReflectionFieldData → 工厂 → 子 Setup()。struct 回写：覆写 OnValueChanged，当 BaseType.IsValueType 时把捕获的装箱实例 Data.SetValue 写回宿主再向上冒泡；子元素 CommitValue 触发冒泡。null 实例显示「暂不支持创建」提示。
