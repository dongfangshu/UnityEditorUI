Type: task
Status: resolved
Blocked by: 05, 06, 07, 08, 09, 10

## Question

示例 + 编译验证：

- `Assets/Scripts/UIDemo/` 下建 `DemoModel : MonoBehaviour`，字段覆盖：基元（bool/int/float/double/long/string）、enum、UnityEngine.Object 引用、Vector2/Vector3、Color、List<int>、List<自定义 struct>、Dictionary<string,int>、嵌套自定义 class
- `DemoModelEditor : CustomEditorBase` + `[CustomEditor(typeof(DemoModel))]`
- 用 /unity-bridge skill 触发 Unity 编译，修到零错误

验收：编译通过；场景中挂 DemoModel 后 Inspector 全类型渲染编辑正常（若 Unity 编辑器在运行可截图确认，否则以编译通过为准）。

## Comments

- 示例代码已写完（`Assets/Scripts/UIDemo/DemoModel.cs` 覆盖全部 v1 类型 + `[SerializeField] private` 字段 + flags enum + List/struct List/Dictionary/嵌套 class；`Editor/DemoModelEditor.cs : CustomEditorBase`）。
- 首次 `core.refresh` 编译失败（3 类错误），编辑器进入 Safe Mode，bridge 离线。已修复：
  1. `ObjectFieldElement` 的 `as Object` 歧义 → `as UnityEngine.Object`（CS0104）
  2. `EnumField` 3 参构造在 2022.3 不存在 → `new EnumField(label)` + `Init(value, flags)`（CS1729）
  3. `Vector2Field/Vector3Field` 在 2022.3 不存在（2023.2 才加入）→ 改为 Label + X/Y(/Z) FloatField 组合元素（CS0246）
- **阻塞：等待用户在 Unity 编辑器退出 Safe Mode**（弹窗点 Ignore，或工具栏 Exit Safe Mode），重编译后 bridge 心跳恢复，继续执行 core.refresh + 错误检查。

## Answer

**编译通过（0 error / 0 warning），端到端验证全部通过。**

修复过程：
1. CS0104 `as Object` 歧义 → `as UnityEngine.Object`；CS1729 EnumField 3 参构造 → `new EnumField(label)` + `Init(value, flags)`；CS0246 Vector2/3Field 不存在于 UnityEditor.UIElements → 用户改用 `UnityEngine.UIElements.Vector2Field/Vector3Field`（2022.3 运行时命名空间有），补 `using UnityEngine.UIElements`（RegisterValueChangedCallback 是 INotifyValueChanged 扩展方法，需要该命名空间）。
2. CS0108 警告：`ListElement.RemoveAt` 遮蔽 `VisualElement.RemoveAt` → 改名 `RemoveItemAt`。
3. 验证中发现并修复关键 bug：RootFieldData 被工厂按 UnityEngine.Object 派生为 ObjectFieldElement（根应始终展开字段）→ 工厂最前面特判 `data is RootFieldData → ObjectElement`。

bridge 验证（2022.3.60f1）：
- 无头构建：rootType=ObjectElement，205 个后代元素，6 个 Foldout（与 DemoModel 的 6 个复合字段精确对应），控制台零异常。
- 面板挂载（临时 EditorWindow）后值回写全部通过：int 99✓、嵌套 struct weight 0.5✓（回写链有效）、list 元素 42✓、enum High✓、bool False✓、string world✓。
- 注意：未挂面板的树上设置 value 不会派发 ChangeEvent（测试方法学坑，非框架 bug）。

场景中留有 `DemoModelAgentTest` 对象（未保存场景），选中即可在 Inspector 查看实际效果。
