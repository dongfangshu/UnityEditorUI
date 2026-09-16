using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 框架主体基类：创建时反射目标对象类型构建 FieldElement 对象树，
    /// 构造成功后调用根的 Setup() 级联组装。
    /// 用法：[CustomEditor(typeof(X))] class XEditor : CustomEditorBase {}
    /// </summary>
    public abstract class CustomEditorBase : Editor
    {
        const string UssPath = EditorUIFrameworkSettings.UssPath;

        VisualElement _host;
        FieldElement _root;

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            container.AddToClassList("eui-inspector-root");
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (uss != null)
                container.styleSheets.Add(uss);
            // 框架设置：可选自定义样式表（在内建 USS 之后，可覆盖 eui- 类）
            var customUss = EditorUIFrameworkSettings.Current.customStyleSheet;
            if (customUss != null)
                container.styleSheets.Add(customUss);

            OnCreateHeader(container);

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

        /// <summary>根数据节点来源；默认检视 target 本身，可覆写以检视其他对象（如 DataHost.target）。</summary>
        protected virtual FieldData CreateRootData() => new RootFieldData(target);

        /// <summary>Undo/SetDirty 的宿主对象；默认 target。</summary>
        protected virtual Object UndoHost => target;

        /// <summary>在元素树之前插入头部 UI 的钩子（如类型选择器），默认空。</summary>
        protected virtual void OnCreateHeader(VisualElement container) { }

        /// <summary>重建整棵元素树（Undo/Redo 后界面同步）。</summary>
        protected void Rebuild()
        {
            if (_host == null || target == null)
                return;

            _host.Clear();
            var data = CreateRootData();
            if (data == null || data.GetValue() == null)
                return; // 无根数据（如 DataHost 尚未选择类型），只显示头部

            _root = FieldElementFactory.Create(data);
            _root.UndoTarget = UndoHost;
            _host.Add(_root);
            _root.Setup();
        }
    }
}
