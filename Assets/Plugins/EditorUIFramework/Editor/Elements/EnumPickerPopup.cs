using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 枚举选择弹窗（PopupWindowContent，点击弹窗外自动关闭），外观对齐 Odin 的 EnumSelector 下拉：
    /// 顶部工具栏式搜索框（内嵌放大镜图标）→ 选项列表，选中项为绿色 ✓、未选中为空心 ○（flags 位），
    /// 选中行带背景高亮；All 行为特殊项（不画标记，仅占位保持文字对齐）。
    /// 普通枚举单选即关；flags 枚举多选不关窗：None 在首、条目按位切换、All 在尾。
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
            public bool IsMeta;      // None / All
            public bool HasMarker;   // 标记列是否可显示状态（All 行为 false，仅占位）
            public VisualElement Element;
            public Label Marker;
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

        /// <summary>
        /// 高度随选项数自适应（Odin 的下拉是内容高度），超过设置里的上限才出现滚动条；
        /// 宽度取设置值。搜索行 22 + 每行 20 + 上下留白。
        /// </summary>
        public override Vector2 GetWindowSize()
        {
            var configured = EditorUIFrameworkSettings.Current.enumPopupSize;

            int optionCount = 0;
            foreach (var name in Enum.GetNames(_enumType))
            {
                if (_flags && Convert.ToInt64(Enum.Parse(_enumType, name)) == 0) continue; // 0 值并入 None
                optionCount++;
            }
            if (_flags) optionCount += 2; // None + All

            float height = SearchRowHeight + optionCount * OptionRowHeight + 6f;
            return new Vector2(configured.x, Mathf.Min(height, configured.y));
        }

        const float SearchRowHeight = 22f;
        const float OptionRowHeight = 22f; // Odin 的选项行高（实测）

        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;
            // Unity 给弹窗窗口根元素设了不透明的默认底色（inline style，USS 覆盖不了），
            // 这里直接改成 Odin 下拉弹窗的深色；圆角与边框由 USS 的 .eui-enum-popup-toolbar 提供。
            root.style.backgroundColor = new Color(49f / 255f, 49f / 255f, 49f / 255f, 1f);
            // 弹窗是独立的编辑器窗口，不会继承检视器的样式表，必须自己加载
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorUIFrameworkSettings.UssPath);
            if (uss != null) root.styleSheets.Add(uss);
            root.AddToClassList("eui-enum-popup");
            root.AddToClassList("eui-enum-popup-toolbar");

            // 搜索行 = Odin 的搜索工具栏：带底纹的一条，内嵌带放大镜图标的搜索框
            var searchRow = new VisualElement();
            searchRow.AddToClassList("eui-enum-search-row");

            _search = new TextField { tooltip = "搜索" };
            _search.AddToClassList("eui-enum-search");
            _search.RegisterValueChangedCallback(_ => Refilter());
            searchRow.Add(_search);

            // 图标必须后加：UI Toolkit 按添加顺序绘制，先加会被搜索框盖住
            var icon = new Image { image = SearchIcon(), pickingMode = PickingMode.Ignore };
            icon.AddToClassList("eui-enum-search-icon");
            searchRow.Add(icon);

            root.Add(searchRow);

            _list = new ScrollView();
            _list.AddToClassList("eui-enum-list");
            _list.style.flexGrow = 1;
            root.Add(_list);

            BuildRows();
            root.schedule.Execute(() => _search.Focus());
        }

        /// <summary>
        /// 放大镜图标。注意 "d_Search Icon" 是 64×64 的图集纹理，缩到 12px 后内容几乎不可见，
        /// 因此优先取 16×16 的搜索窗口图标。
        /// </summary>
        static Texture2D SearchIcon()
        {
            foreach (var name in new[] { "d_SearchWindow", "SearchWindow", "d_Search Icon", "Search Icon" })
            {
                var content = EditorGUIUtility.IconContent(name);
                if (content != null && content.image is Texture2D tex && tex.width <= 32)
                    return tex;
            }
            return null;
        }

        void BuildRows()
        {
            _rows.Clear();
            _list.Clear();

            if (_flags)
                AddRow("None", 0, true, true);

            foreach (var name in Enum.GetNames(_enumType))
            {
                var value = Convert.ToInt64(Enum.Parse(_enumType, name));
                if (_flags && value == 0) continue; // 0 值已由 None 表示
                AddRow(ObjectNames.NicifyVariableName(name), value, false, true);
            }

            if (_flags)
                AddRow("All", _allBits, true, false); // Odin 的 All 行不画标记
        }

        void AddRow(string name, long value, bool isMeta, bool hasMarker)
        {
            var row = new VisualElement();
            row.AddToClassList("eui-enum-item");

            Label marker = null;
            if (hasMarker)
            {
                marker = new Label("○");
                marker.AddToClassList("eui-enum-marker");
                row.Add(marker);
            }
            else
            {
                // 标记列占位：让 All 的文字与其它行左对齐
                var spacer = new VisualElement();
                spacer.AddToClassList("eui-enum-marker");
                row.Add(spacer);
            }
            row.Add(new Label(name) { style = { flexGrow = 1 } });

            var r = new Row { Name = name, Value = value, IsMeta = isMeta, HasMarker = hasMarker, Element = row, Marker = marker };
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
            bool on;
            if (row.IsMeta)
                on = row.Name == "None" ? _bits == 0 : (_bits == _allBits && _allBits != 0);
            else if (_flags)
                on = row.Value != 0 && (_bits & row.Value) == row.Value; // flags：按位判断（0 值已由 None 代表）
            else
                on = _bits == row.Value; // 普通枚举：按值相等判断，0 值项（如 Low = 0）同样要能显示 ✓

            row.Element.EnableInClassList("eui-enum-item-selected", on); // Odin：选中行加背景高亮

            if (row.Marker == null) return;
            // 选中 = 绿色 ✓；未选中的 flags 位 = 空心 ○（普通枚举留空）
            row.Marker.text = on ? "✓" : (_flags ? "○" : string.Empty);
            row.Marker.EnableInClassList("eui-enum-marker-on", on);
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
