Type: task
Status: resolved
Blocked by: 02, 03

## Question

实现静态类 `FieldElementFactory`：

- `FieldElement Create(FieldData data)`：按 `data.BaseType` 分派——
  - 基元（bool/int/float/double/long/string）→ 对应基元 element（ticket 05）
  - enum → EnumElement；UnityEngine.Object 引用 → ObjectFieldElement；Color → ColorElement（05）
  - Vector2 / Vector3 → 对应 element（06）
  - 实现 IList 的 List<T> → ListElement（07）；实现 IDictionary 的 Dictionary<K,V> → DictElement（08）
  - 其他可序列化 class/struct（非 UnityEngine.Object）→ ObjectElement（09）
  - 均不匹配 → UnsupportedElement（不抛异常）
- 暴露注册表（`Dictionary<Type, Func<FieldData, FieldElement>>` + 谓词链），允许外部扩展自定义类型映射。
- list/dict/嵌套对象的子节点一律经本工厂递归构造。

验收：分派优先级为「注册表精确匹配 > 接口判定 > 兜底」；未知类型落到 UnsupportedElement。

## Answer

已实现 `FieldElementFactory.cs`：静态工厂 Create(FieldData)。内建精确注册 bool/int/float/double/long/string/Vector2/Vector3/Color；CreateBuiltin 判定链：enum → EnumElement，UnityEngine.Object → ObjectFieldElement，数组 → null（留雾区），IDictionary → DictElement，IList → ListElement，Delegate → null，其余 class/struct → ObjectElement，最终 null → UnsupportedElement。扩展点：Register(Type, ctor) 精确覆盖 + RegisterProvider 谓词链（后注册先匹配）。
