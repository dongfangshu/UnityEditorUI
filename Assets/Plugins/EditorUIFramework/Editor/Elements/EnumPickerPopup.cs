using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 枚举选择弹窗（PopupWindowContent，点击弹窗外自动关闭）：顶部搜索框 + 滚动列表。
    /// 普通枚举单选即关；flags 枚举多选不关窗：None 在首、条目带圆点（○ 未选 / ● 绿色已选）、All 在尾。
    /// </summary>
    public class EnumPickerPopup : PopupWindowContent
    {
        Type _enumType;
        bool _flags;
        long _bits;
        long _allBits;
        Action<Enum> _onChanged;

        TextField _search;
        ScrollView _list;
        readonly List<Row> _rows = new List<Row>();

        sealed class Row
        {
            public string Name;
            public long Value;
            public bool IsMeta; // None / All
            public VisualElement Element;
            public Label Circle;
        }

        /// <summary>在 anchor（通常为按钮 worldBound）处弹出；返回内容实例（可编程关闭）。</summary>
        public static EnumPickerPopup Open(Rect anchor, Type enumType, bool flags, Enum current, Action<Enum> onChanged)
        {
            var content = new EnumPickerPopup
            {
                _enumType = enumType,
                _flags = flags,
                _bits = Convert.ToInt64(current),
                _onChanged = onChanged
            };
            content._allBits = ComputeAllBits(enumType);
            UnityEditor.PopupWindow.Show(anchor, content);
            return content;
        }

        public override Vector2 GetWindowSize() => EditorUIFrameworkSettings.Current.enumPopupSize;

        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;
            root.AddToClassList("eui-enum-popup");

            _search = new TextField { tooltip = "搜索" };
            _search.AddToClassList("eui-enum-search");
            _search.RegisterValueChangedCallback(_ => Refilter());
            root.Add(_search);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            root.Add(_list);

            BuildRows();
            root.schedule.Execute(() => _search.Focus());
        }

        void BuildRows()
        {
            _rows.Clear();
            _list.Clear();

            if (_flags)
                AddRow("None", 0, true);

            foreach (var name in Enum.GetNames(_enumType))
            {
                var value = Convert.ToInt64(Enum.Parse(_enumType, name));
                if (_flags && value == 0) continue; // 0 值已由 None 表示
                AddRow(ObjectNames.NicifyVariableName(name), value, false);
            }

            if (_flags)
                AddRow("All", _allBits, true);
        }

        void AddRow(string name, long value, bool isMeta)
        {
            var row = new VisualElement();
            row.AddToClassList("eui-enum-item");

            Label circle = null;
            if (_flags)
            {
                circle = new Label("○");
                circle.AddToClassList("eui-enum-circle");
                row.Add(circle);
            }
            row.Add(new Label(name) { style = { flexGrow = 1 } });

            var r = new Row { Name = name, Value = value, IsMeta = isMeta, Element = row, Circle = circle };
            row.RegisterCallback<ClickEvent>(_ => OnRowClick(r));
            _rows.Add(r);
            _list.Add(row);
            RefreshRow(r);
        }

        void OnRowClick(Row row)
        {
            if (!_flags)
            {
                _bits = row.Value;
                NotifyChanged();
                editorWindow.Close(); // 单选即关
                return;
            }

            if (row.IsMeta && row.Name == "None") _bits = 0;
            else if (row.IsMeta && row.Name == "All") _bits = _allBits;
            else _bits ^= row.Value;

            foreach (var r in _rows) RefreshRow(r);
            NotifyChanged();
        }

        void NotifyChanged() =>
            _onChanged((Enum)Enum.ToObject(_enumType, _bits));

        void RefreshRow(Row row)
        {
            if (row.Circle == null) return;
            bool on = row.IsMeta
                ? (row.Name == "None" ? _bits == 0 : _bits == _allBits && _allBits != 0)
                : (row.Value != 0 && (_bits & row.Value) == row.Value);
            row.Circle.text = on ? "●" : "○";
            row.Circle.EnableInClassList("eui-enum-circle-on", on);
        }

        void Refilter()
        {
            var text = _search.value;
            foreach (var r in _rows)
            {
                bool show = string.IsNullOrEmpty(text)
                    || r.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
                r.Element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        static long ComputeAllBits(Type enumType)
        {
            long bits = 0;
            foreach (var name in Enum.GetNames(enumType))
                bits |= Convert.ToInt64(Enum.Parse(enumType, name));
            return bits;
        }
    }
}
