Type: task
Status: resolved
Blocked by: 04

## Question

实现 `DictElement`（Dictionary<K,V> / IDictionary）完整编辑：

- Foldout 展示键值对数；每行：key 只读显示 + value 子 element（工厂 + `DictEntryData`）+ 删除按钮
- 底部新增行：新 key 编辑器（key 限可比较基元：string/int/float/bool/enum 等）+ 添加按钮；重复键拒绝并提示；key 类型不支持时新增行禁用并提示
- key 创建后不可改；value 编辑走工厂分派（嵌套类型递归展开）
- 所有变更走 `Undo.RecordObject`

验收：Dictionary<string,int>、Dictionary<int,自定义 class> 增删改正常；重复键被拒；非法 key 类型有提示。

## Answer

已实现 `Elements/DictElement.cs`：Foldout（标题含 Count）+ 行容器；每行 = key 只读 Label（eui-dict-key）+ value 子 element（工厂 + DictEntryData）+「-」删除。新增行：key 类型限 string/int/float/double/long/bool/enum（其余显示禁用提示）；新 key 编辑器复用工厂 + 内部 TempValueData（不记 Undo）；「+ 添加」校验空键/重复键（eui-error 行内提示）。所有真实变更走 Undo.RecordObject + SetDirty。key 创建后不可改（行内无 key 编辑器）。
