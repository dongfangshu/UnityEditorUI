Type: task
Status: resolved
Blocked by: 04

## Question

新增 Lua 类挂载器 LuaHost（与 DataHost 同构）：

- 运行时 `LuaHost : MonoBehaviour`：`luaFolder`（固定目录，项目相对路径）+ `luaFile`（选中文件）+ `List<LuaFieldEntry> fields`（值条目袋：name/type/value 字符串，不变文化序列化）
- 编辑器解析 EmmyLua 注解：`---@class X` / `---@field v1 int`（正则），得字段模式
- `LuaFieldData : FieldData`：包装条目，BaseType = lua 类型名映射（int/long/float/double/number/string/bool/boolean），GetValue/SetValue 做字符串↔类型双向转换 → 工厂体系零改动复用
- `LuaRootData` + 工厂谓词 → `LuaSchemaElement`（按条目袋建树，非反射）
- `LuaHostEditor : CustomEditorBase`：头部 = 目录 TextField + 浏览按钮 + lua 文件下拉；选文件后解析→同步条目袋（同名同类型保值）→Rebuild
- 值编辑走标准 CommitValue（Undo 宿主 = LuaHost）

边界：不执行 lua（无 VM，纯数据挂载）；不支持的 lua 类型 → UnsupportedElement。

验收：编译零错误；TestClass.lua 选入后 v1(int)/v2(long) 渲染可编辑，值写回条目袋。

## Answer

已实现并 bridge 实测通过：

- **Runtime**：`LuaHost`（luaFolder + luaFile + List\<LuaFieldEntry\> fields 值袋，[Serializable] 随场景持久化）
- **Editor/Lua/**：`LuaSchemaParser`（正则解析 ---@class / ---@field name type）；`LuaTypeMap`（int/long/float/double/number/string/bool/boolean → C# 类型，未识别 → null → UnsupportedElement）；`LuaFieldData`（条目包装，字符串↔类型双向 Convert，不变文化）；`LuaRootData`（BaseType=object，供工厂谓词识别）
- `LuaSchemaElement`：按值袋经工厂建树（非反射路径），工厂谓词在 LuaHostEditor 静态构造注册
- `LuaHostEditor`：头部 = Lua 目录 TextField + 「…」浏览（EditorUtility.OpenFolderPanel，限项目内转相对路径）+ lua 文件下拉（Directory.GetFiles *.lua）；选文件 → RecordObject → 解析 → SyncFields（同名同类型保值，新增补默认）→ SetDirty → Rebuild
- 示例：Assets/Lua/TestClass.lua（v1 int / v2 long）、TestClass2.lua（string/float/number/bool）

实测：解析得 className=TestClass、schema=[v1:int, v2:long]；工厂路由 rootElement=LuaSchemaElement；面板挂载后 v1→123、v2→9876543210 写回条目袋成功；选中宿主真实 Inspector（目录行+文件下拉+字段树）构建零异常。
