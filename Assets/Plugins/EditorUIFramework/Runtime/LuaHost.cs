using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EditorUIFramework
{
    /// <summary>Lua 字段条目：名字 + lua 类型名 + 值（基元为不变文化字符串；集合为 lua 字面量）。</summary>
    [Serializable]
    public class LuaFieldEntry
    {
        public string name;
        public string type;
        public string value;
    }

    /// <summary>
    /// Lua 类挂载器：选择固定目录下的 .lua 文件，解析 ---@class / ---@field 注解得到字段模式；
    /// 实例值持久化在 fields 条目袋中，由 LuaHostEditor 建树编辑。
    /// 注意：本组件只做数据挂载与检视，不执行 lua（项目中无 lua VM）。
    /// </summary>
    public class LuaHost : MonoBehaviour
    {
        [Tooltip("Lua 文件所在的固定目录（项目相对路径）")]
        public string luaFolder = "Assets/Lua";

        [Tooltip("当前选择的 lua 文件（项目相对路径）")]
        public string luaFile;

        [Tooltip("解析 lua 字段注解得到的实例数据")]
        public List<LuaFieldEntry> fields = new List<LuaFieldEntry>();

        /// <summary>
        /// 导出 lua 对象字符串（表构造）：
        /// 基元 = 标量；list → { 1, 2 }；dictionary → { ["k"] = 1 }。
        /// </summary>
        public string GetLuaData()
        {
            var sb = new StringBuilder("{");
            foreach (var e in fields)
            {
                sb.Append('\n').Append('\t').Append(e.name).Append(" = ");
                var t = LuaTypeMap.Map(e.type);
                if (t != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string))
                {
                    var collection = LuaValue.ParseTo(t, e.value);
                    sb.Append(LuaValue.ToLua(collection));
                }
                else
                {
                    sb.Append(ScalarToLua(e.value, t));
                }
                sb.Append(',');
            }
            sb.Append('\n').Append('}');
            return sb.ToString();
        }

        static string ScalarToLua(string raw, Type t)
        {
            if (t == null) return "nil";
            if (string.IsNullOrEmpty(raw)) raw = null;
            try
            {
                if (t == typeof(string)) return LuaValue.ToLua(raw ?? string.Empty);
                var value = raw == null ? Activator.CreateInstance(t) : Convert.ChangeType(raw, t, System.Globalization.CultureInfo.InvariantCulture);
                return LuaValue.ToLua(value);
            }
            catch { return "nil"; }
        }
    }
}

