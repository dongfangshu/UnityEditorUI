Type: task
Status: resolved
Blocked by: 04

## Question

实现简单值 element（均遵守 ticket 03 契约，值双向同步 + Undo）：

- bool → Toggle；int → IntegerField；float → FloatField；double → DoubleField；long → LongField；string → TextField
- enum → EnumElement（EnumField，区分 flags / 非 flags）
- UnityEngine.Object 引用 → ObjectFieldElement（ObjectField，objectType = BaseType）
- Color → ColorElement（ColorField）

验收：每种类型「读取显示 / 编辑回写 + Undo」双向正确。

## Answer

已实现 `Elements/SimpleElements.cs`：BoolElement(Toggle) / IntElement(IntegerField) / FloatElement(FloatField) / DoubleElement(DoubleField) / LongElement(LongField) / StringElement(TextField) / EnumElement(EnumField，自动识别 [Flags]) / ObjectFieldElement(ObjectField，objectType=BaseType) / ColorElement(ColorField)；UnsupportedElement 同文件。初值读取均做 null 安全（Convert.ToXxx(?? 默认值) / as 判空）。
