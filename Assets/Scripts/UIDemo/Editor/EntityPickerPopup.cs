using System;
using System.Collections.Generic;
using EditorUIFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIDemo
{
    /// <summary>
    /// 实体搜索弹窗（PopupWindowContent，点击弹窗外自动关闭）：
    /// 顶部搜索框（按名称/id 过滤），中间分页列表（item 显示 实体名 + id），底部翻页栏。
    /// </summary>
    public class EntityPickerPopup : PopupWindowContent
    {
        static int PageSize => Mathf.Max(1, EditorUIFrameworkSettings.Current.pickerPageSize);

        readonly List<DemoEntity> _filtered = new List<DemoEntity>();
        int _page;
        Action<int> _onSelected;

        TextField _search;
        ScrollView _list;
        Button _prev;
        Button _next;
        Label _pageLabel;

        /// <summary>在 anchor（通常为按钮 worldBound）处弹出；返回内容实例（可编程关闭）。</summary>
        public static EntityPickerPopup Open(Rect anchor, int currentId, Action<int> onSelected)
        {
            var content = new EntityPickerPopup { _onSelected = onSelected };
            UnityEditor.PopupWindow.Show(anchor, content);
            return content;
        }

        public override Vector2 GetWindowSize() => EditorUIFrameworkSettings.Current.pickerPopupSize;

        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;
            root.AddToClassList("eui-enum-popup");

            _search = new TextField { tooltip = "搜索名称或 id" };
            _search.AddToClassList("eui-enum-search");
            _search.RegisterValueChangedCallback(_ => Refilter());
            root.Add(_search);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            root.Add(_list);

            var pager = new VisualElement();
            pager.AddToClassList("eui-entity-pager");
            _prev = new Button(() => ChangePage(-1)) { text = "◀" };
            _prev.AddToClassList("eui-item-button");
            _pageLabel = new Label();
            _pageLabel.AddToClassList("eui-entity-pager-label");
            _next = new Button(() => ChangePage(1)) { text = "▶" };
            _next.AddToClassList("eui-item-button");
            pager.Add(_prev);
            pager.Add(_pageLabel);
            pager.Add(_next);
            root.Add(pager);

            Refilter();
            root.schedule.Execute(() => _search.Focus());
        }

        void Refilter()
        {
            _filtered.Clear();
            var text = _search != null ? _search.value : null;
            foreach (var e in DemoEntityDatabase.All)
            {
                if (string.IsNullOrEmpty(text)
                    || e.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    || e.Id.ToString().Contains(text))
                    _filtered.Add(e);
            }
            _page = 0;
            RefreshPage();
        }

        int PageCount => Math.Max(1, (_filtered.Count + PageSize - 1) / PageSize);

        void ChangePage(int delta)
        {
            _page = Mathf.Clamp(_page + delta, 0, PageCount - 1);
            RefreshPage();
        }

        void RefreshPage()
        {
            _page = Mathf.Clamp(_page, 0, PageCount - 1);
            _list.Clear();

            int start = _page * PageSize;
            int end = Mathf.Min(start + PageSize, _filtered.Count);
            for (int i = start; i < end; i++)
                _list.Add(BuildRow(_filtered[i]));

            if (_filtered.Count == 0)
            {
                var empty = new Label("无匹配结果");
                empty.style.opacity = 0.5f;
                empty.style.alignSelf = Align.Center;
                empty.style.marginTop = 8;
                _list.Add(empty);
            }

            _pageLabel.text = $"{_page + 1} / {PageCount}";
            _prev.SetEnabled(_page > 0);
            _next.SetEnabled(_page < PageCount - 1);
        }

        VisualElement BuildRow(DemoEntity e)
        {
            var row = new VisualElement();
            row.AddToClassList("eui-enum-item");
            row.Add(new Label(e.Name) { style = { flexGrow = 1 } });

            var idLabel = new Label(e.Id.ToString());
            idLabel.AddToClassList("eui-entity-id");
            row.Add(idLabel);

            row.RegisterCallback<ClickEvent>(_ =>
            {
                _onSelected(e.Id);
                editorWindow.Close();
            });
            return row;
        }
    }
}
