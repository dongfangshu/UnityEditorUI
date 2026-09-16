using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// List&lt;T&gt; / IList 完整编辑，布局与交互一比一复刻 Odin 的 CollectionDrawer&lt;T&gt;（反编译 Sirenix.OdinInspector.Editor 核对）：
    /// 标题栏 = Unity horizontal toolbar（高 22）：折叠三角 + 字段名 → 弹性空白 → "N items" 灰字 → 工具栏加号；
    /// 行 = 拖拽把手(20×20) + 值控件(无标签) + ✕(14×14)，行高 25、内边距(25,20,3,3)，偶/奇行 #3C3C3C/#333333 斑马纹；
    /// 拖拽 = Odin 式：源行原位蓝色高亮，浮动副本跟随鼠标，松手重排落位。
    /// 元素经工厂 + ListItemData 递归构造；结构性变更后重建子树。
    /// </summary>
    public class ListElement : FieldElement
    {
        IList _list;
        Type _elementType;
        Foldout _foldout;
        Label _countLabel;
        VisualElement _items;

        VisualElement _dragSourceRow; // 拖拽源行（原位保留，蓝色高亮）
        VisualElement _dragGhost;     // 浮动副本（跟随鼠标）
        int _dragFrom = -1;           // 拖拽开始时的数据索引
        int _dragPointerId;
        float _dragLastY;             // 最近一次指针 y（相对 _items 内容区）
        float _dragRowHeight;         // 拖拽源行高度（浮动副本与 clamp 用）
        bool _dragMoved;

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

            // Odin 式标题栏（对齐反编译 CollectionDrawer.DrawToolbar）：
            // Unity IMGUI 的 horizontal toolbar（高 22）：[Foldout：箭头+字段名] → FlexibleSpace → [计数] → [工具栏加号]
            var header = new VisualElement();
            header.AddToClassList("eui-list-header");

            _foldout = CreateFoldout(DisplayName);
            _foldout.AddToClassList("eui-list-foldout");
            header.Add(_foldout);
            MainControl = _foldout;

            // 计数：Odin 用 centeredGreyMiniLabel 显示 "N items"（空列表显示 "Empty"），紧贴加号左侧
            _countLabel = new Label(CountText());
            _countLabel.AddToClassList("eui-list-count");
            header.Add(_countLabel);

            var addButton = new Button(AddItem) { text = "+", tooltip = "添加元素" };
            addButton.AddToClassList("eui-toolbar-button");
            header.Add(addButton);

            Add(header);

            // 列表体独立于标题栏（对齐 Odin：toolbar 与列表体是前后两块），折叠由 Foldout 的 value 驱动
            _items = new VisualElement();
            _items.AddToClassList("eui-list-body");
            _items.style.overflow = Overflow.Hidden; // 裁剪浮动副本，防止拖拽时跑到列表外（如 Add Component）
            Add(_items);
            _foldout.RegisterValueChangedCallback(evt =>
                _items.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None);

            // 拖拽：指针捕获挂在 _items 上（行重建后仍有效）
            _items.RegisterCallback<PointerMoveEvent>(OnDragMove);
            _items.RegisterCallback<PointerUpEvent>(evt => EndDrag(evt.pointerId, ToLocalY(evt.position)));
            _items.RegisterCallback<PointerCaptureOutEvent>(evt => EndDrag(evt.pointerId, float.NaN));

            RebuildItems();
        }

        /// <summary>标题栏右侧计数文本（Odin：空列表显示 "Empty"，否则 "N items"）。</summary>
        string CountText() => _list.Count == 0 ? "Empty" : $"{_list.Count} items";

        void RebuildItems()
        {
            _countLabel.text = CountText();
            _items.Clear();
            for (int i = 0; i < _list.Count; i++)
            {
                int index = i;
                var row = new VisualElement();
                row.AddToClassList("eui-list-item");
                if (i % 2 == 1) row.AddToClassList("eui-list-item-odd"); // Odin 式斑马纹

                row.Add(BuildDragHandle(index));

                var data = new ListItemData(_list, index, _elementType);
                var element = FieldElementFactory.Create(data);
                element.ParentFieldElement = this;
                element.UndoTarget = UndoTarget;
                // Odin：元素标签默认不画，只有展开成多行的复合对象才需要 "Element N" 标题条
                element.HideLabel = !(element is ObjectElement);
                element.style.flexGrow = 1;
                row.Add(element);
                element.Setup();

                row.Add(BuildRemoveButton(index));

                _items.Add(row);
            }
        }

        /// <summary>行尾删除按钮：Odin 的 RemoveBtnRect 为 14×14，贴行右内边距 4 处。</summary>
        VisualElement BuildRemoveButton(int index)
        {
            var remove = new Button(() => RemoveItemAt(index)) { text = "✕", tooltip = "移除元素" };
            remove.AddToClassList("eui-list-remove");
            return remove;
        }

        /// <summary>
        /// 指针事件给的是面板（panel）坐标，而行布局给的是 _items 内容区坐标。
        /// 两者必须换算：否则浮动副本会被 clamp 到列表底部、落点也永远算成末尾。
        /// </summary>
        float ToLocalY(Vector2 panelPosition)
        {
            var b = _items.worldBound;
            return panelPosition.y - b.y - _items.resolvedStyle.borderTopWidth;
        }

        /// <summary>拖拽把手：三根横杠（贴近 Odin 列表图标），按下后进入拖拽排序。</summary>
        VisualElement BuildDragHandle(int index)
        {
            var handle = new VisualElement();
            handle.AddToClassList("eui-drag-handle");
            handle.tooltip = "拖拽排序";
            for (int i = 0; i < 3; i++)
            {
                var bar = new VisualElement();
                bar.AddToClassList("eui-drag-bar");
                handle.Add(bar);
            }
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_dragFrom >= 0) return; // 已在拖拽中
                var row = handle.parent;
                _dragFrom = index;
                _dragPointerId = evt.pointerId;
                _dragMoved = false;
                _dragLastY = row.layout.y;
                _dragRowHeight = row.layout.height;
                _dragSourceRow = row;
                _dragSourceRow.AddToClassList("eui-list-item-dragging"); // 源行蓝色高亮

                // Odin 式浮动副本：行骨架绝对定位跟随鼠标（UI Toolkit 无元素克隆 API，手动构建）
                _dragGhost = BuildDragGhost();
                _dragGhost.style.position = Position.Absolute;
                _dragGhost.style.left = row.layout.x;
                _dragGhost.style.top = row.layout.y;
                _dragGhost.style.width = row.layout.width;
                _dragGhost.style.height = _dragRowHeight; // 先给定高度，首帧就有正确的高度参与 clamp
                _items.Add(_dragGhost);

                _items.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            return handle;
        }

        /// <summary>浮动副本骨架：把手 + 值占位块 + ✕，拖拽预览用。</summary>
        VisualElement BuildDragGhost()
        {
            var ghost = new VisualElement();
            ghost.AddToClassList("eui-list-item");
            ghost.AddToClassList("eui-drag-ghost");

            var handle = new VisualElement();
            handle.AddToClassList("eui-drag-handle");
            for (int i = 0; i < 3; i++)
            {
                var bar = new VisualElement();
                bar.AddToClassList("eui-drag-bar");
                handle.Add(bar);
            }
            ghost.Add(handle);

            var valueBlock = new VisualElement();
            valueBlock.AddToClassList("eui-ghost-value");
            ghost.Add(valueBlock);

            var x = new Label("✕");
            x.AddToClassList("eui-list-remove");
            ghost.Add(x);

            return ghost;
        }

        void OnDragMove(PointerMoveEvent evt)
        {
            if (_dragFrom < 0 || _dragGhost == null) return;
            float y = ToLocalY(evt.position);
            _dragLastY = y;
            _dragMoved = true;
            // 副本跟手（垂直居中），并 clamp 在列表范围内
            float h = _dragRowHeight > 0f ? _dragRowHeight : 25f;
            float top = y - h * 0.5f;
            float maxTop = Mathf.Max(0f, _items.resolvedStyle.height - h);
            _dragGhost.style.top = Mathf.Clamp(top, 0f, maxTop);
            evt.StopPropagation();
        }

        void EndDrag(int pointerId, float y)
        {
            if (_dragFrom < 0) return;
            int from = _dragFrom;
            // 落点计算必须在清理拖拽状态之前（FindInsertIndex 依赖跳过源行）
            int target = FindInsertIndex(float.IsNaN(y) ? _dragLastY : y);

            _dragFrom = -1;
            if (_items.HasPointerCapture(pointerId))
                _items.ReleasePointer(pointerId);

            if (_dragSourceRow != null)
                _dragSourceRow.RemoveFromClassList("eui-list-item-dragging");
            _dragSourceRow = null;

            if (_dragGhost != null)
            {
                _dragGhost.RemoveFromHierarchy();
                _dragGhost = null;
            }

            if (!_dragMoved || target == from) return;
            _dragMoved = false;

            RecordAndRefresh($"Reorder {Data.Name}", () =>
            {
                from = Mathf.Clamp(from, 0, _list.Count - 1);
                target = Mathf.Clamp(target, 0, _list.Count - 1);
                var item = _list[from];
                _list.RemoveAt(from);
                _list.Insert(target, item);
            });
        }

        /// <summary>由指针局部坐标（相对 _items）推算插入索引：跳过源行与浮动副本，按其余行中心分段。</summary>
        int FindInsertIndex(float localY)
        {
            int idx = 0;
            float acc = 0;
            foreach (var c in _items.Children())
            {
                if (c == _dragSourceRow || c == _dragGhost) continue;
                float h = c.resolvedStyle.height;
                if (localY < acc + h * 0.5f) return idx;
                idx++;
                acc += h;
            }
            return idx;
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
