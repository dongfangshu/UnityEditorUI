using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EditorUIFramework
{
    /// <summary>EmmyLua 风格注解解析：---@class X 与 ---@field name type（行尾注释忽略）。</summary>
    public static class LuaSchemaParser
    {
        static readonly Regex ClassRe = new Regex(@"^\s*---@class\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex FieldRe = new Regex(@"^\s*---@field\s+(\w+)\s+(\S+)", RegexOptions.Multiline | RegexOptions.Compiled);

        public static string ParseClassName(string luaText)
        {
            if (luaText == null) return null;
            var m = ClassRe.Match(luaText);
            return m.Success ? m.Groups[1].Value : null;
        }

        public static List<(string name, string type)> ParseFields(string luaText)
        {
            var list = new List<(string, string)>();
            if (luaText == null) return list;
            foreach (Match m in FieldRe.Matches(luaText))
                list.Add((m.Groups[1].Value, m.Groups[2].Value));
            return list;
        }

        /// <summary>按项目相对路径读文件，失败返回 null。</summary>
        public static string ReadFile(string projectRelativePath)
        {
            try { return File.ReadAllText(projectRelativePath); }
            catch { return null; }
        }
    }
}
