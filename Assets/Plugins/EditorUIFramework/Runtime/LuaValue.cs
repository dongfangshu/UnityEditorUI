using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EditorUIFramework
{
    /// <summary>
    /// lua 字面量序列化/解析：list → { 1, 2 }，dictionary → { ["k"] = 1 }。
    /// 纯 C#（Runtime 程序集可用），供 LuaHost.GetLuaData 与编辑器侧 LuaFieldData 共用。
    /// </summary>
    public static class LuaValue
    {
        // ---------- 序列化 ----------

        /// <summary>把运行时值序列化为 lua 字面量（按实际类型分派）。</summary>
        public static string ToLua(object value)
        {
            if (value == null) return "nil";
            if (value is string s) return Quote(s);
            if (value is bool b) return b ? "true" : "false";
            if (value is IDictionary dict) return DictToLua(dict);
            if (value is IList list) return ListToLua(list);
            if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
            return Quote(value.ToString());
        }

        static string ListToLua(IList list)
        {
            var sb = new StringBuilder("{");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(' ').Append(ToLua(list[i]));
            }
            sb.Append(list.Count > 0 ? " }" : "}");
            return sb.ToString();
        }

        static string DictToLua(IDictionary dict)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(',');
                sb.Append(" [").Append(KeyToLua(entry.Key)).Append("] = ").Append(ToLua(entry.Value));
                first = false;
            }
            sb.Append(first ? "}" : " }");
            return sb.ToString();
        }

        /// <summary>字典键：string 加引号，数字/布尔原样。</summary>
        static string KeyToLua(object key)
        {
            if (key is string ks) return Quote(ks);
            if (key is bool kb) return kb ? "true" : "false";
            if (key is IFormattable kf) return kf.ToString(null, CultureInfo.InvariantCulture);
            return Quote(key?.ToString() ?? string.Empty);
        }

        static string Quote(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }

        // ---------- 解析（物化为指定类型实例） ----------

        /// <summary>把 lua 表字面量物化为 t（List&lt;T&gt; / Dictionary&lt;K,V&gt;）实例；失败给空集合。</summary>
        public static object ParseTo(Type t, string lua)
        {
            var table = ParseTable(lua);
            try { return Materialize(t, table); }
            catch { return Activator.CreateInstance(t); }
        }

        sealed class LuaTable
        {
            public readonly List<object> Items = new List<object>();                 // 位置项（string | LuaTable）
            public readonly List<KeyValuePair<object, object>> Pairs =               // 键值项
                new List<KeyValuePair<object, object>>();
        }

        static object Materialize(Type t, LuaTable table)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elem = t.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(t);
                foreach (var item in table.Items)
                    list.Add(ConvertItem(elem, item));
                // 容错：键为整数的对也按位置收
                return list;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var args = t.GetGenericArguments();
                var dict = (IDictionary)Activator.CreateInstance(t);
                foreach (var pair in table.Pairs)
                    dict.Add(ConvertItem(args[0], pair.Key), ConvertItem(args[1], pair.Value));
                return dict;
            }
            return Activator.CreateInstance(t);
        }

        static object ConvertItem(Type t, object token)
        {
            if (token is LuaTable nested) return Materialize(t, nested);
            var raw = (string)token;
            if (t == typeof(string)) return raw;
            if (t == typeof(bool)) return raw == "true" || raw == "True";
            if (t.IsEnum) return Enum.Parse(t, raw);
            return Convert.ChangeType(raw, t, CultureInfo.InvariantCulture);
        }

        /// <summary>解析 { ... } 表构造；非表输入给空表。</summary>
        static LuaTable ParseTable(string s)
        {
            var table = new LuaTable();
            if (string.IsNullOrEmpty(s)) return table;
            int pos = 0;
            ParseTableInto(s.Trim(), ref pos, table);
            return table;
        }

        static void ParseTableInto(string s, ref int pos, LuaTable table)
        {
            SkipWs(s, ref pos);
            if (pos >= s.Length || s[pos] != '{') return;
            pos++;
            while (pos < s.Length)
            {
                SkipWs(s, ref pos);
                if (pos >= s.Length) break;
                if (s[pos] == '}') { pos++; break; }
                if (s[pos] == ',') { pos++; continue; }

                if (s[pos] == '[')
                {
                    pos++;
                    var key = ReadValue(s, ref pos);
                    SkipWs(s, ref pos);
                    if (pos < s.Length && s[pos] == ']') pos++;
                    SkipWs(s, ref pos);
                    if (pos < s.Length && s[pos] == '=') pos++;
                    var value = ReadValue(s, ref pos);
                    table.Pairs.Add(new KeyValuePair<object, object>(key, value));
                }
                else
                {
                    table.Items.Add(ReadValue(s, ref pos));
                }
            }
        }

        /// <summary>读一个值：引号串 / 嵌套表 / 裸标量（到逗号或右括号）。</summary>
        static object ReadValue(string s, ref int pos)
        {
            SkipWs(s, ref pos);
            if (pos >= s.Length) return string.Empty;
            if (s[pos] == '{')
            {
                var nested = new LuaTable();
                ParseTableInto(s, ref pos, nested);
                return nested;
            }
            if (s[pos] == '"')
            {
                var sb = new StringBuilder();
                pos++;
                while (pos < s.Length && s[pos] != '"')
                {
                    if (s[pos] == '\\' && pos + 1 < s.Length)
                    {
                        pos++;
                        sb.Append(s[pos] == 'n' ? '\n' : s[pos]);
                    }
                    else sb.Append(s[pos]);
                    pos++;
                }
                if (pos < s.Length) pos++; // 收尾引号
                return sb.ToString();
            }
            int start = pos;
            while (pos < s.Length && s[pos] != ',' && s[pos] != '}' && s[pos] != ']')
                pos++;
            return s.Substring(start, pos - start).Trim();
        }

        static void SkipWs(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        }
    }
}
