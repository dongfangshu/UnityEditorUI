Type: task
Status: resolved
Blocked by: 10

## Question

支持「非 MonoBehaviour 纯数据对象在 Inspector 显示」：

- `DataHost : MonoBehaviour`：`[SerializeReference] public object target`（不加此特性域重载后数据丢失）
- `[DataObject]` 特性标记候选数据类（TypeCache 收集；过滤：class、非抽象、非泛型定义、有无参构造、非 UnityEngine.Object）
- `DataHostEditor : CustomEditorBase`：顶部类型下拉；选择后 Activator 创建实例并建树；切换/清空时确认「将丢失当前数据」
- 用 target 运行时实例构建根（RootFieldData 接受任意对象）；Undo 宿主 = DataHost 本身

框架改动：CustomEditorBase 增加 `CreateRootData()` / `UndoHost` 虚方法与 `OnCreateHeader` 钩子；新增 Runtime 程序集（DataObjectAttribute + DataHost），Editor asmdef 引用之。

验收：编译零错误；场景中 DataHost 选类型后整树渲染、值可编辑可 Undo。

## Answer

已实现并验证：

- **Runtime 程序集**（EditorUIFramework.Runtime，全平台）：`DataObjectAttribute` + `DataHost`（[SerializeReference] object target）
- **框架钩子**：CustomEditorBase 新增虚方法 `CreateRootData()`（默认 RootFieldData(target)）、`UndoHost`（默认 target）、`OnCreateHeader(container)`；Rebuild 对空根数据（未选类型）安全跳过
- **DataHostEditor**：[CustomEditor(typeof(DataHost))]，OnCreateHeader 注入类型下拉（TypeCache.GetTypesWithAttribute\<DataObjectAttribute\> + class/非抽象/非泛型/无参构造/非 UnityEngine.Object 过滤）；选择/切换/清空均有确认弹窗（EditorUtility.DisplayDialog），切换即 Activator.CreateInstance + RecordObject(host) + SetDirty + Rebuild
- 修复 CS0104：`typeof(Object)` → `typeof(UnityEngine.Object)`
- 示例：[DataObject] DemoPlayerData（含嵌套 class + List）/ DemoShopData（含 Dictionary + Color）

bridge 验证：编译零错误；TypeCache 扫出且仅扫出两个示例类型；以 DemoPlayerData 建树后 string/int/嵌套 class 值回写全部通过（改造人/77/详细资料）；选中宿主 GO 真实 Inspector 构建零异常。场景中留有 `DataHostAgentTest` 对象可直接查看。
