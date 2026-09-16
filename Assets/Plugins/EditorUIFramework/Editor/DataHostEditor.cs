using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// DataHost 的检视器：顶部下拉选择 [DataObject] 标记的数据类型，
    /// 选择后创建实例并用其运行时类型反射建树；切换/清空需确认（会丢失当前数据）。
    /// </summary>
    [CustomEditor(typeof(DataHost))]
    public class DataHostEditor : CustomEditorBase
    {
        const string NoneLabel = "(None)";

        List<Type> _types;
        DropdownField _dropdown;
        bool _syncing;

        DataHost Host => (DataHost)target;

        /// <summary>根数据 = 宿主上的数据实例（而非 DataHost 本身）。</summary>
        protected override FieldData CreateRootData() => new RootFieldData(Host.target);

        protected override void OnCreateHeader(VisualElement container)
        {
            _types = CollectDataTypes();
            var names = new List<string> { NoneLabel };
            names.AddRange(_types.Select(t => t.Name));

            _dropdown = new DropdownField("数据类型", names, CurrentTypeName());
            _dropdown.RegisterValueChangedCallback(e => OnTypeSelected(e.newValue));
            container.Add(_dropdown);
        }

        string CurrentTypeName() => Host.target != null ? Host.target.GetType().Name : NoneLabel;

        static List<Type> CollectDataTypes()
        {
            return TypeCache.GetTypesWithAttribute<DataObjectAttribute>()
                .Where(t => t.IsClass && !t.IsAbstract
                            && !t.IsGenericTypeDefinition
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToList();
        }

        void OnTypeSelected(string name)
        {
            if (_syncing) return;

            if (name == NoneLabel)
            {
                if (Host.target == null) return;
                if (!Confirm("清空数据对象，当前数据将丢失？")) { SyncDropdown(); return; }
                Undo.RecordObject(Host, "Clear Data Object");
                Host.target = null;
            }
            else
            {
                var type = _types.FirstOrDefault(t => t.Name == name);
                if (type == null || (Host.target != null && Host.target.GetType() == type))
                {
                    SyncDropdown();
                    return;
                }
                if (Host.target != null && !Confirm("切换类型将丢弃当前数据，继续？"))
                {
                    SyncDropdown();
                    return;
                }
                Undo.RecordObject(Host, "Change Data Type");
                Host.target = Activator.CreateInstance(type);
            }

            EditorUtility.SetDirty(Host);
            Rebuild();
            SyncDropdown();
        }

        void SyncDropdown()
        {
            _syncing = true;
            _dropdown.SetValueWithoutNotify(CurrentTypeName());
            _syncing = false;
        }

        static bool Confirm(string message) =>
            EditorUtility.DisplayDialog("DataHost", message, "确定", "取消");
    }
}
