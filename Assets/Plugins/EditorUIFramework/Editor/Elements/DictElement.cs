using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// Dictionary&lt;K,V&gt; / IDictionary 完整编辑，布局对齐 Odin 的 DictionaryDrawer&lt;TDictionary,TKey,TValue&gt;（反编译核对）：
    /// 标题栏 = 与列表同款工具栏（折叠三角 + 字段名 → "N items" → 加号）；
    /// 加号切换「添加键值对」面板（Key 输入 + Value 输入 + Add 按钮，键非法时按钮禁用并显示原因）；
    /// 列表体 = Key/Value 列标题行 + 行（拖拽把手 + Key 列 + Value 控件 + ✕），斑马纹与行高同列表。
    /// key 创建后不可改（改键等价于删旧插新，本框架暂不提供）。
    /// </summary>
    public class DictElement : FieldElement
    {
        static readonly HashSet<Type> SupportedKeyTypes = new HashSet<Type>
        {
            typeof(string), typeof(int), typeof(float),
            typeof(double), typeof(long), typeof(bool)
        };

        /// <summary>Key 列宽（Odin 的 keyWidthOffset 默认量级，且支持拖动调整；这里取固定值）。</summary>
        const float KeyColumnWidth = 120f;

        IDictionary _dict;
        Type _keyType;
        Type _valueType;
        Foldout _foldout;
        Label _countLabel;
        VisualElement _addPanel;
        VisualElement _items;
        Button _addButton;
        bool _addPanelRequested; // 由标题栏加号切换（Odin 的 showAddKeyGUI）
        TempValueData _newKeyData;
        TempValueData _newValueData;

        VisualElement _dragSourceRow;
        VisualElement _dragGhost;
        int _dragFrom = -1;
        int _dragPointerId;
        float _dragLastY;
        float _dragRowHeight;
        bool _dragMoved;

        public DictElement(FieldData data) : base(data) { }

        static bool IsKeySupported(Type t) => SupportedKeyTypes.Contains(t) || t.IsEnum;

        protected override void OnSetup()
        {
            _dict = Data.GetValue() as IDictionary;
            if (_dict == null)
            {
                MainControl = UnsupportedLabel("null 字典，暂不支持创建");
                Add(MainControl);
                return;
            }

            var t = Data.BaseType;
            if (t.IsGenericType && t.GetGenericArguments().Length == 2)
            {
                var args = t.GetGenericArguments();
                _keyType = args[0];
                _valueType = args[1];
            }
            else
            {
                _keyType = typeof(object);
                _valueType = typeof(object);
            }

            // 标题栏（与 ListElement 同款 Odin 工具栏）
            var header = new VisualElement();
            header.AddToClassList("eui-list-header");

            _foldout = CreateFoldout(DisplayName);
            _foldout.AddToClassList("eui-list-foldout");
            header.Add(_foldout);
            MainControl = _foldout;

            _countLabel = new Label(CountText());
            _countLabel.AddToClassList("eui-list-count");
            header.Add(_countLabel);

            var toggleAdd = new Button(ToggleAddPanel) { text = "+", tooltip = "添加键值对" };
            toggleAdd.AddToClassList("eui-toolbar-button");
            header.Add(toggleAdd);

            Add(header);

            // Odin 的顺序：toolbar → 添加面板 → 列表体
            BuildAddPanel();

            _items = new VisualElement();
            _items.AddToClassList("eui-list-body");
            _items.style.overflow = Overflow.Hidden;
            Add(_items);

            // 折叠时连带隐藏列表与添加面板；展开时恢复添加面板被 + 切换出来的状态
            _foldout.RegisterValueChangedCallback(evt =>
            {
                var style = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _items.style.display = style;
                _addPanel.style.display = _addPanelRequested && evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            // 拖拽排序（IDictionary 无顺序语义，重排 = 按新顺序重写条目）
            _items.RegisterCallback<PointerMoveEvent>(OnDragMove);
            _items.RegisterCallback<PointerUpEvent>(evt => EndDrag(evt.pointerId, ToLocalY(evt.position)));
            _items.RegisterCallback<PointerCaptureOutEvent>(evt => EndDrag(evt.pointerId, float.NaN));

            RebuildRows();
        }

        string CountText() => _dict.Count == 0 ? "Empty" : $"{_dict.Count} items";

        // ---------- 添加面板（Odin：由工具栏加号切换） ----------

        void ToggleAddPanel()
        {
            _addPanelRequested = !_addPanelRequested;
            _addPanel.style.display = _addPanelRequested ? DisplayStyle.Flex : DisplayStyle.None;
            if (_addPanelRequested)
                _foldout.value = true;
        }

        void BuildAddPanel()
        {
            _addPanel = new VisualElement();
            _addPanel.AddToClassList("eui-dict-add-panel");
            _addPanel.style.display = DisplayStyle.None;
            Add(_addPanel);

            if (!IsKeySupported(_keyType))
            {
                var hint = new Label($"键类型 {_keyType.Name} 不支持（仅限 string/int/float/double/long/bool/enum）");
                hint.AddToClassList("eui-unsupported");
                _addPanel.Add(hint);
                return;
            }

            // Key 输入：复用工厂 + 临时数据节点（不记 Undo、不回写真实对象）
            _newKeyData = new TempValueData("Key", _keyType) { OnChanged = ValidateNewKey };
            var keyElement = FieldElementFactory.Create(_newKeyData);
            keyElement.UndoTarget = null;
            keyElement.style.flexGrow = 1;
            _addPanel.Add(keyElement);
            keyElement.Setup();

            // Value 输入
            _newValueData = new TempValueData("Value", _valueType);
            var valueElement = FieldElementFactory.Create(_newValueData);
            valueElement.UndoTarget = null;
            valueElement.style.flexGrow = 1;
            _addPanel.Add(valueElement);
            valueElement.Setup();

            _addButton = new Button(TryAdd) { text = "Add" };
            _addButton.AddToClassList("eui-dict-add-button");
            _addPanel.Add(_addButton);
            ValidateNewKey();
        }

        /// <summary>键非法时按钮禁用并显示原因（Odin 同款行为）。</summary>
        void ValidateNewKey()
        {
            if (_addButton == null) return;
            var key = _newKeyData.Value;
            bool ok = key != null && !_dict.Contains(key);
            _addButton.SetEnabled(ok);
            _addButton.text = ok ? "Add" : (key == null ? "键为空" : "键已存在");
        }

        void TryAdd()
        {
            var key = _newKeyData.Value;
            if (key == null || _dict.Contains(key))
            {
                ValidateNewKey();
                return;
            }

            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, $"Add {Data.Name}");
            _dict.Add(key, _newValueData.Value);
            Data.SetValue(_dict); // lua 条目袋触发序列化回写
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);

            _newKeyData.Reset();
            _newValueData.Reset();
            RebuildRows();
            ValidateNewKey();
        }

        void RemoveKey(object key)
        {
            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, $"Remove {Data.Name}");
            _dict.Remove(key);
            Data.SetValue(_dict);
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);
            RebuildRows();
        }

        /// <summary>子元素改值冒泡：同步写回（lua 场景下字典是字符串物化缓存，必须回写）。</summary>
        public override void OnValueChanged()
        {
            Data.SetValue(_dict);
            base.OnValueChanged();
        }

        // ---------- 行 ----------

        void RebuildRows()
        {
            _countLabel.text = CountText();
            _items.Clear();

            if (_dict.Count > 0)
                _items.Add(BuildColumnHeader());

            int index = 0;
            foreach (DictionaryEntry entry in _dict)
            {
                _items.Add(BuildRow(entry, index));
                index++;
            }
        }

        /// <summary>Key / Value 列标题行（Odin 在非空时绘制，并带一条底部分隔线）。</summary>
        VisualElement BuildColumnHeader()
        {
            var row = new VisualElement();
            row.AddToClassList("eui-dict-columns");

            var key = new Label("Key");
            key.AddToClassList("eui-dict-column-key");
            row.Add(key);

            var value = new Label("Value");
            value.AddToClassList("eui-dict-column-value");
            row.Add(value);

            return row;
        }

        VisualElement BuildRow(DictionaryEntry entry, int index)
        {
            var row = new VisualElement();
            row.AddToClassList("eui-list-item");
            if (index % 2 == 1) row.AddToClassList("eui-list-item-odd");

            row.Add(BuildDragHandle(index, entry.Key));

            var keyLabel = new Label(entry.Key != null ? entry.Key.ToString() : "null");
            keyLabel.AddToClassList("eui-dict-key");
            keyLabel.style.width = KeyColumnWidth;
            row.Add(keyLabel);

            var data = new DictEntryData(_dict, entry.Key, _valueType);
            var element = FieldElementFactory.Create(data);
            element.ParentFieldElement = this;
            element.UndoTarget = UndoTarget;
            element.HideLabel = !(element is ObjectElement);
            element.style.flexGrow = 1;
            row.Add(element);
            element.Setup();

            var remove = new Button(() => RemoveKey(entry.Key)) { text = "✕", tooltip = "移除该键值对" };
            remove.AddToClassList("eui-list-remove");
            row.Add(remove);

            return row;
        }

        // ---------- 拖拽重排 ----------

        /// <summary>行把手；IDictionary 没有顺序语义，落位后按新顺序重写全部条目。</summary>
        VisualElement BuildDragHandle(int index, object key)
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
                if (_dragFrom >= 0) return;
                var row = handle.parent;
                _dragFrom = index;
                _dragPointerId = evt.pointerId;
                _dragMoved = false;
                _dragLastY = row.layout.y;
                _dragRowHeight = row.layout.height;
                _dragSourceRow = row;
                _dragSourceRow.AddToClassList("eui-list-item-dragging");

                _dragGhost = BuildDragGhost();
                _dragGhost.style.position = Position.Absolute;
                _dragGhost.style.left = row.layout.x;
                _dragGhost.style.top = row.layout.y;
                _dragGhost.style.width = row.layout.width;
                _dragGhost.style.height = _dragRowHeight;
                _items.Add(_dragGhost);

                _items.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            return handle;
        }

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

            var keyBlock = new VisualElement();
            keyBlock.AddToClassList("eui-ghost-value");
            keyBlock.style.width = KeyColumnWidth;
            keyBlock.style.flexGrow = 0;
            ghost.Add(keyBlock);

            var valueBlock = new VisualElement();
            valueBlock.AddToClassList("eui-ghost-value");
            ghost.Add(valueBlock);

            var x = new Label("✕");
            x.AddToClassList("eui-list-remove");
            ghost.Add(x);

            return ghost;
        }

        /// <summary>指针事件是面板坐标，行布局是 _items 内容区坐标，必须换算（同 ListElement）。</summary>
        float ToLocalY(Vector2 panelPosition)
        {
            var b = _items.worldBound;
            return panelPosition.y - b.y - _items.resolvedStyle.borderTopWidth;
        }

        void OnDragMove(PointerMoveEvent evt)
        {
            if (_dragFrom < 0 || _dragGhost == null) return;
            float y = ToLocalY(evt.position);
            _dragLastY = y;
            _dragMoved = true;
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

            // 收集当前条目顺序 → 移动 → 重写字典（列标题行不参与计数）
            var entries = new List<DictionaryEntry>();
            foreach (DictionaryEntry e in _dict)
                entries.Add(e);
            from = Mathf.Clamp(from, 0, entries.Count - 1);
            target = Mathf.Clamp(target, 0, entries.Count - 1);
            var moved = entries[from];
            entries.RemoveAt(from);
            entries.Insert(target, moved);

            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, $"Reorder {Data.Name}");
            _dict.Clear();
            foreach (var e in entries)
                _dict.Add(e.Key, e.Value);
            Data.SetValue(_dict);
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);
            RebuildRows();
        }

        int FindInsertIndex(float localY)
        {
            int idx = 0;
            float acc = 0;
            foreach (var c in _items.Children())
            {
                if (c == _dragSourceRow || c == _dragGhost) continue;
                if (c.ClassListContains("eui-dict-columns")) continue; // 列标题行不参与落点计算
                float h = c.resolvedStyle.height;
                if (localY < acc + h * 0.5f) return idx;
                idx++;
                acc += h;
            }
            return idx;
        }

        static object DefaultValue(Type t)
        {
            if (t == typeof(string)) return string.Empty;
            if (t.IsValueType) return Activator.CreateInstance(t);
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return null;
            try { return Activator.CreateInstance(t); }
            catch { return null; }
        }

        /// <summary>新增键/值的临时取值节点：不记 Undo、不回写真实对象；改值通知用于即时校验键。</summary>
        sealed class TempValueData : FieldData
        {
            readonly Type _type;
            object _value;

            public Action OnChanged;

            public TempValueData(string name, Type type) : base(name)
            {
                _type = type;
                _value = DefaultValue(type);
            }

            public override Type BaseType => _type;
            public object Value => _value;

            /// <summary>提交后回到默认值（Odin 添加完会把输入清空）。</summary>
            public void Reset()
            {
                _value = DefaultValue(_type);
                OnChanged?.Invoke();
            }

            public override object GetValue() => _value;

            public override void SetValue(object value)
            {
                _value = value;
                OnChanged?.Invoke();
            }
        }
    }
}
