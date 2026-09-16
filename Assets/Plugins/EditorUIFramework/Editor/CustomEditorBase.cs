using UnityEditor;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 框架主体基类：创建时反射目标对象类型构建 FieldElement 对象树，
    /// 构造成功后调用根的 Setup() 级联组装内部。
    /// 用法：[CustomEditor(typeof(X))] class XEditor : CustomEditorBase {}
    /// </summary>
    public abstract class CustomEditorBase : Editor
    {
        const string UssPath = "Assets/Plugins/EditorUIFramework/Editor/USS/EditorUIFramework.uss";

        VisualElement _host;
        FieldElement _root;

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (uss != null)
                container.styleSheets.Add(uss);

            _host = new VisualElement();
            container.Add(_host);
            Rebuild();

            Undo.undoRedoPerformed -= Rebuild;
            Undo.undoRedoPerformed += Rebuild;
            return container;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Rebuild;
        }

        /// <summary>重建整棵元素树（Undo/Redo 后界面同步）。</summary>
        protected void Rebuild()
        {
            if (_host == null || target == null)
                return;

            _host.Clear();
            _root = FieldElementFactory.Create(new RootFieldData(target));
            _root.UndoTarget = target;
            _host.Add(_root);
            _root.Setup();
        }
    }
}
