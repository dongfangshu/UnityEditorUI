using System;
using System.Collections;
using UnityEditor;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// List&lt;T&gt; / IList 完整编辑：增、删、上移/下移排序、改值。
    /// 元素经工厂 + ListItemData 递归构造；结构性变更后重建子树。
    /// </summary>
    public class ListElement : FieldElement
    {
        IList _list;
        Type _elementType;
        Foldout _foldout;
        VisualElement _items;

        public ListElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            _list = Data.GetValue() as IList;
            if (_list == null)
            {
                MainControl = UnsupportedLabel("null 列表，暂不支持创建");
                Add(MainControl);
                return;
            }

            var t = Data.BaseType;
            _elementType = t.IsGenericType ? t.GetGenericArguments()[0] : typeof(object);

            _foldout = new Foldout { text = Title(), value = true };
            _foldout.AddToClassList("eui-foldout");
            MainControl = _foldout;
            Add(_foldout);

            _items = new VisualElement();
            _items.AddToClassList("eui-indent");
            _foldout.Add(_items);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("eui-list-toolbar");
            var addButton = new Button(AddItem) { text = "+" };
            addButton.AddToClassList("eui-item-button");
            toolbar.Add(addButton);
            _foldout.Add(toolbar);

            RebuildItems();
        }

        string Title() => $"{DisplayName} ({_list.Count})";

        void RebuildItems()
        {
            _foldout.text = Title();
            _items.Clear();
            for (int i = 0; i < _list.Count; i++)
            {
                int index = i;
                var row = new VisualElement();
                row.AddToClassList("eui-list-item");

                var data = new ListItemData(_list, index, _elementType);
                var element = FieldElementFactory.Create(data);
                element.ParentFieldElement = this;
                element.UndoTarget = UndoTarget;
                element.style.flexGrow = 1;
                row.Add(element);
                element.Setup();

                var remove = new Button(() => RemoveItemAt(index)) { text = "-" };
                remove.AddToClassList("eui-item-button");
                row.Add(remove);

                var up = new Button(() => Move(index, -1)) { text = "▲" };
                up.AddToClassList("eui-item-button");
                row.Add(up);

                var down = new Button(() => Move(index, 1)) { text = "▼" };
                down.AddToClassList("eui-item-button");
                row.Add(down);

                _items.Add(row);
            }
        }

        void RecordAndRefresh(string action, Action op)
        {
            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, action);
            op();
            Data.SetValue(_list); // 反射列表为无害重设；lua 条目袋触发序列化回写
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);
            RebuildItems();
        }

        /// <summary>子元素改值冒泡：同步写回（lua 场景下列表是字符串物化缓存，必须回写）。</summary>
        public override void OnValueChanged()
        {
            Data.SetValue(_list);
            base.OnValueChanged();
        }

        void AddItem() =>
            RecordAndRefresh($"Add {Data.Name}", () => _list.Add(DefaultValue(_elementType)));

        void RemoveItemAt(int index) =>
            RecordAndRefresh($"Remove {Data.Name}", () => _list.RemoveAt(index));

        void Move(int index, int delta)
        {
            int target = index + delta;
            if (target < 0 || target >= _list.Count) return;
            RecordAndRefresh($"Reorder {Data.Name}", () =>
            {
                var tmp = _list[index];
                _list[index] = _list[target];
                _list[target] = tmp;
            });
        }

        static object DefaultValue(Type t)
        {
            if (t == typeof(string)) return string.Empty;
            if (t.IsValueType) return Activator.CreateInstance(t);
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return null;
            try { return Activator.CreateInstance(t); }
            catch { return null; }
        }
    }
}
