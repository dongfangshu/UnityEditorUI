using System;
using System.Collections.Generic;

namespace EditorUIFramework
{
    /// <summary>
    /// lua 类型名 → C# 类型映射，支持递归泛型：List&lt;int&gt; / Dictionary&lt;string,int&gt;。
    /// 未识别返回 null（工厂落 UnsupportedElement）。
    /// </summary>
    public static class LuaTypeMap
    {
        public static Type Map(string luaType)
        {
            if (string.IsNullOrEmpty(luaType)) return null;
            int pos = 0;
            var t = ParseType(luaType.Trim(), ref pos);
            SkipWs(luaType, ref pos);
            return pos >= luaType.Length ? t : null; // 尾部有残串视为不合法
        }

        static Type ParseType(string s, ref int pos)
        {
            SkipWs(s, ref pos);
            int start = pos;
            while (pos < s.Length && (char.IsLetterOrDigit(s[pos]) || s[pos] == '_' || s[pos] == '.'))
                pos++;
            if (pos == start) return null;
            var name = s.Substring(start, pos - start);

            SkipWs(s, ref pos);
            if (pos < s.Length && s[pos] == '<')
            {
                pos++;
                var args = new List<Type>();
                while (true)
                {
                    var arg = ParseType(s, ref pos);
                    if (arg == null) return null;
                    args.Add(arg);
                    SkipWs(s, ref pos);
                    if (pos < s.Length && s[pos] == ',') { pos++; continue; }
                    if (pos < s.Length && s[pos] == '>') { pos++; break; }
                    return null; // 缺少 > 或分隔符
                }
                return MakeGeneric(name, args);
            }
            return MapPrimitive(name);
        }

        static Type MakeGeneric(string name, List<Type> args)
        {
            try
            {
                if (name == "List" && args.Count == 1)
                    return typeof(List<>).MakeGenericType(args[0]);
                if (name == "Dictionary" && args.Count == 2)
                    return typeof(Dictionary<,>).MakeGenericType(args[0], args[1]);
            }
            catch { }
            return null;
        }

        static Type MapPrimitive(string name)
        {
            switch (name)
            {
                case "int": return typeof(int);
                case "long": return typeof(long);
                case "float": return typeof(float);
                case "double":
                case "number": return typeof(double);
                case "string": return typeof(string);
                case "bool":
                case "boolean": return typeof(bool);
                default: return null;
            }
        }

        static void SkipWs(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        }
    }
}
