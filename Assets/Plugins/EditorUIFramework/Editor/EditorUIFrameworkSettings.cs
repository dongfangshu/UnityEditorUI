using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// UI 框架基础设置（ScriptableObject，资产放 Editor 目录、仅编辑器使用）。
    /// 访问入口：EditorUIFrameworkSettings.Current（不存在时自动在默认路径创建资产）。
    /// 菜单位置：Tools/EditorUIFramework/Settings（选中并定位设置资产）。
    /// </summary>
    public class EditorUIFrameworkSettings : ScriptableObject
    {
        const string DefaultAssetPath = "Assets/Plugins/EditorUIFramework/Editor/EditorUIFrameworkSettings.asset";

        /// <summary>框架样式表路径；检视器根节点与各弹窗都从这里加载。</summary>
        public const string UssPath = "Assets/Plugins/EditorUIFramework/Editor/USS/EditorUIFramework.uss";

        [Tooltip("枚举选择弹窗尺寸")]
        public Vector2 enumPopupSize = new Vector2(240f, 260f);

        [Tooltip("通用搜索选择弹窗尺寸（如实体选择器）")]
        public Vector2 pickerPopupSize = new Vector2(280f, 340f);

        [Tooltip("搜索选择弹窗每页条目数")]
        public int pickerPageSize = 10;

        [Tooltip("追加到检视器根节点的自定义样式表（在内建 USS 之后加载，可覆盖 eui- 类）")]
        public StyleSheet customStyleSheet;

        [Tooltip("LuaHost 默认 Lua 目录（组件未指定目录时的缺省值）")]
        public string defaultLuaFolder = "Assets/Lua";

        static EditorUIFrameworkSettings _current;

        /// <summary>全局设置实例；项目中不存在资产时在默认路径创建。</summary>
        public static EditorUIFrameworkSettings Current
        {
            get
            {
                if (_current == null)
                {
                    var guids = AssetDatabase.FindAssets("t:EditorUIFrameworkSettings");
                    if (guids.Length > 0)
                        _current = AssetDatabase.LoadAssetAtPath<EditorUIFrameworkSettings>(
                            AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                if (_current == null)
                {
                    _current = CreateInstance<EditorUIFrameworkSettings>();
                    AssetDatabase.CreateAsset(_current, DefaultAssetPath);
                    AssetDatabase.SaveAssets();
                }
                return _current;
            }
        }

        [MenuItem("Tools/EditorUIFramework/Settings")]
        static void SelectSettings()
        {
            Selection.activeObject = Current;
            EditorGUIUtility.PingObject(Current);
        }
    }
}
