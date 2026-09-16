using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// LuaHost 检视器：头部 = 固定目录（TextField + 浏览）+ lua 文件下拉；
    /// 选择文件后解析 ---@field 注解、同步条目袋（同名同类型保值）、重建字段树。
    /// </summary>
    [CustomEditor(typeof(LuaHost))]
    public class LuaHostEditor : CustomEditorBase
    {
        static LuaHostEditor()
        {
            FieldElementFactory.RegisterProvider(d => d is LuaRootData root ? new LuaSchemaElement(root) : null);
        }

        LuaHost Host => (LuaHost)target;

        DropdownField _fileDropdown;

        protected override FieldData CreateRootData()
        {
            if (string.IsNullOrEmpty(Host.luaFile)) return null;
            var text = LuaSchemaParser.ReadFile(Host.luaFile);
            if (text == null) return null;
            return new LuaRootData(Host, LuaSchemaParser.ParseClassName(text));
        }

        protected override void OnCreateHeader(VisualElement container)
        {
            var folderRow = new VisualElement();
            folderRow.AddToClassList("eui-row");

            var folderField = new TextField("Lua 目录") { value = Host.luaFolder };
            folderField.style.flexGrow = 1;
            folderField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(Host, "Change Lua Folder");
                Host.luaFolder = e.newValue;
                EditorUtility.SetDirty(Host);
                RefreshFileList();
            });
            folderRow.Add(folderField);

            var browse = new Button(() =>
            {
                var abs = EditorUtility.OpenFolderPanel("选择 Lua 目录", Host.luaFolder, "");
                if (string.IsNullOrEmpty(abs)) return;
                var rel = ToProjectRelative(abs);
                if (rel == null)
                {
                    EditorUtility.DisplayDialog("LuaHost", "目录必须在项目内", "确定");
                    return;
                }
                Undo.RecordObject(Host, "Change Lua Folder");
                Host.luaFolder = rel;
                EditorUtility.SetDirty(Host);
                folderField.SetValueWithoutNotify(rel);
                RefreshFileList();
            }) { text = "…" };
            browse.AddToClassList("eui-item-button");
            folderRow.Add(browse);
            container.Add(folderRow);

            _fileDropdown = new DropdownField("Lua 文件");
            _fileDropdown.RegisterValueChangedCallback(e => OnFileSelected(e.newValue));
            container.Add(_fileDropdown);
            RefreshFileList();
        }

        static string ToProjectRelative(string absPath)
        {
            var full = Path.GetFullPath(absPath).Replace('\\', '/');
            var root = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/') + "/";
            return full.StartsWith(root) ? full.Substring(root.Length) : null;
        }

        List<string> ListLuaFiles()
        {
            try
            {
                if (string.IsNullOrEmpty(Host.luaFolder) || !Directory.Exists(Host.luaFolder))
                    return new List<string>();
                return Directory.GetFiles(Host.luaFolder, "*.lua")
                    .Select(p => p.Replace('\\', '/'))
                    .OrderBy(p => p)
                    .ToList();
            }
            catch { return new List<string>(); }
        }

        void RefreshFileList()
        {
            var files = ListLuaFiles();
            _fileDropdown.choices = files.Select(Path.GetFileName).ToList();
            _fileDropdown.SetValueWithoutNotify(files.Contains(Host.luaFile) ? Path.GetFileName(Host.luaFile) : null);
        }

        void OnFileSelected(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Host.luaFolder.TrimEnd('/') + "/" + fileName;
            if (path == Host.luaFile) return;

            var text = LuaSchemaParser.ReadFile(path);
            if (text == null) return;

            Undo.RecordObject(Host, "Select Lua File");
            Host.luaFile = path;
            SyncFields(LuaSchemaParser.ParseFields(text));
            EditorUtility.SetDirty(Host);
            Rebuild();
        }

        /// <summary>按模式同步条目袋：同名同类型保留旧值，新增补默认值，多余条目删除。</summary>
        void SyncFields(List<(string name, string type)> schema)
        {
            var old = Host.fields ?? new List<LuaFieldEntry>();
            var synced = new List<LuaFieldEntry>(schema.Count);
            foreach (var (name, type) in schema)
            {
                var existing = old.FirstOrDefault(e => e.name == name && e.type == type);
                if (existing != null)
                {
                    synced.Add(existing);
                    continue;
                }
                var t = LuaTypeMap.Map(type);
                string defaultValue = null;
                if (t != null)
                {
                    if (t == typeof(string)) defaultValue = string.Empty;
                    else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t)) defaultValue = "{}"; // 集合默认空表
                    else defaultValue = Convert.ToString(Activator.CreateInstance(t), CultureInfo.InvariantCulture);
                }
                synced.Add(new LuaFieldEntry
                {
                    name = name,
                    type = type,
                    value = defaultValue
                });
            }
            Host.fields = synced;
        }
    }
}
