Type: task
Status: resolved
Blocked by: 15

## Question

Lua 挂载增强：

1. **泛型类型**：注解解析支持 `List<int>` / `Dictionary<string,int>`（递归泛型解析器），映射到 C# 泛型 → 工厂产 ListElement/DictElement；`--_` 注释行不解析
2. **集合值存储**：条目 value 直接存 lua 字面量（list `{ 1, 2 }`、dict `{ ["k"] = 1 }`）；LuaFieldData 物化缓存 + 序列化回写
3. **写回钩子**：ListElement/DictElement 在结构变更与子值变更时 `Data.SetValue(集合实例)`（反射列表无害，lua 触发序列化）
4. **`LuaHost.GetLuaData()`**（Runtime）：返回 lua 表字符串 `{ v1 = 123, v3 = { 1, 2 }, v4 = { ["a"] = 1 } }`
5. LuaTypeMap/LuaValue 移到 Runtime 程序集（GetLuaData 运行时可用）

验收：v3 List<int> 渲染为 ListElement、注释行 v4 不解析；集合序列化/解析往返一致；GetLuaData 输出合法 lua 表。

## Answer

已实现并 bridge 实测全绿：

- **LuaTypeMap**（移入 Runtime）：递归泛型解析器，`List<int>`/`Dictionary<string,int>` → C# 泛型；解析器正则类型 token 改 `\S+`；`--_` 注释行天然不匹配
- **LuaValue**（Runtime，新增）：lua 字面量序列化/解析——list `{ 1, 2, 3 }`、dict `{ ["a"] = 1 }`；转义引号/反斜杠/换行；ParseTo 物化为泛型集合（表解析器支持嵌套表、键值对、引号串）
- **LuaFieldData**：集合走物化缓存 + SetValue 序列化回写；基元维持字符串转换
- **写回钩子**：ListElement/DictElement 覆写 OnValueChanged + 结构操作后 `Data.SetValue(集合)`（反射列表无害重设，lua 触发序列化）
- **`LuaHost.GetLuaData()`**：Runtime 可调，输出多行 lua 表构造

实测：schema=[v1:int, v2:long, v3:List\<int\>]（v4 注释行正确忽略）；泛型映射 OK；dict/list 解析序列化往返一致；v3 渲染为 ListElement；物化列表 Add(7,9) 冒泡后条目 `v3={ 7, 9 }`；GetLuaData 输出：
```lua
{
	v1 = 123,
	v2 = 9876543210,
	v3 = { 7, 9 },
}
```
