Type: task
Status: resolved
Blocked by: 04

## Question

实现 `ListElement`（List<T> / IList）完整编辑：

- Foldout 展示元素数；每个元素经工厂 + `ListItemData` 递归构造子 element
- 增：追加 default(T)（引用类型尝试 `Activator.CreateInstance`）；删：移除所选；排序：上移/下移（或用 editor ListView 的 reorderable）
- 推荐 ListView（showAddRemoveFooter + reorderable）；若 makeItem/bindItem 与工厂模式冲突，退为手动行容器 + 工具条
- 结构性变更后重建子树刷新；所有变更走 `Undo.RecordObject`

验收：List<int>、List<string>、List<自定义 struct> 增删改排序正常；List<List<T>> 嵌套不崩。

## Answer

已实现 `Elements/ListElement.cs`：Foldout（标题含 Count）+ eui-indent 行容器 + 底部「+」工具条；每行 = 工厂递归构造的子 element（ListItemData）+ 「-」删除 + 「▲/▼」上移下移。弃用 ListView 改用受控行容器（避免虚拟化与工厂 per-item 的绑定复杂度，Inspector 列表规模下足够）。结构性变更统一 RecordAndRefresh（Undo → 变更 → SetDirty → RebuildItems）。新增默认值规则：string→""，值类型→default，Object 引用→null，其余尝试 Activator。null 列表显示不支持提示。
