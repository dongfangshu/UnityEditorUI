using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// Dictionary&lt;K,V&gt; / IDictionary 完整编辑：key 限可比较基元（string/int/float/double/long/bool/enum），
    /// key 创建后不可改；支持增删键值对、改 value（value 经工厂 + DictEntryData 递归构造）。
    /// </summary>
    public class DictElement : FieldElement
    {
        static readonly HashSet<Type> SupportedKeyTypes = new HashSet<Type>
        {
            typeof(string), typeof(int), typeof(float),
            typeof(double), typeof(long), typeof(bool)
        };

        IDictionary _dict;
        Type _keyType;
        Type _valueType;
        Foldout _foldout;
        VisualElement _rows;
        TempValueData _newKeyData;
        Label _errorLabel;

        public DictElement(FieldData data) : base(data) { }

        static bool IsKeySupported(Type t) => SupportedKeyTypes.Contains(t) || t.IsEnum;

        public override void Setup()
        {
            _dict = Data.GetValue() as IDictionary;
            if (_dict == null)
            {
                Add(UnsupportedLabel("null 字典，暂不支持创建"));
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

            _foldout = new Foldout { text = Title(), value = true };
            _foldout.AddToClassList("eui-foldout");
            Add(_foldout);

            _rows = new VisualElement();
            _rows.AddToClassList("eui-indent");
            _foldout.Add(_rows);

            BuildAddRow();
            RebuildRows();
        }

        string Title() => $"{DisplayName} ({_dict.Count})";

        void BuildAddRow()
        {
            var row = new VisualElement();
            row.AddToClassList("eui-dict-row");

            if (!IsKeySupported(_keyType))
            {
                var hint = new Label($"键类型 {_keyType.Name} 不支持（仅限 string/int/float/double/long/bool/enum）");
                hint.AddToClassList("eui-unsupported");
                row.Add(hint);
            }
            else
            {
                _newKeyData = new TempValueData("新 Key", _keyType);
                var keyElement = FieldElementFactory.Create(_newKeyData);
                keyElement.UndoTarget = null; // 未提交前不记 Undo
                keyElement.style.flexGrow = 1;
                row.Add(keyElement);
                keyElement.Setup();

                var addButton = new Button(TryAdd) { text = "+ 添加" };
                addButton.AddToClassList("eui-item-button");
                row.Add(addButton);

                _errorLabel = new Label(string.Empty);
                _errorLabel.AddToClassList("eui-error");
                row.Add(_errorLabel);
            }

            _foldout.Add(row);
        }

        void TryAdd()
        {
            var key = _newKeyData.Value;
            if (key == null)
            {
                if (_errorLabel != null) _errorLabel.text = "键为空";
                return;
            }
            if (_dict.Contains(key))
            {
                if (_errorLabel != null) _errorLabel.text = "键已存在";
                return;
            }

            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, $"Add {Data.Name}");
            _dict.Add(key, DefaultValue(_valueType));
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);
            if (_errorLabel != null) _errorLabel.text = string.Empty;
            RebuildRows();
        }

        void RemoveKey(object key)
        {
            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, $"Remove {Data.Name}");
            _dict.Remove(key);
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);
            RebuildRows();
        }

        void RebuildRows()
        {
            _foldout.text = Title();
            _rows.Clear();
            foreach (DictionaryEntry entry in _dict)
            {
                var key = entry.Key;
                var row = new VisualElement();
                row.AddToClassList("eui-dict-row");

                var keyLabel = new Label(key != null ? key.ToString() : "null");
                keyLabel.AddToClassList("eui-dict-key");
                row.Add(keyLabel);

                var data = new DictEntryData(_dict, key, _valueType);
                var element = FieldElementFactory.Create(data);
                element.ParentFieldElement = this;
                element.UndoTarget = UndoTarget;
                element.style.flexGrow = 1;
                row.Add(element);
                element.Setup();

                var remove = new Button(() => RemoveKey(key)) { text = "-" };
                remove.AddToClassList("eui-item-button");
                row.Add(remove);

                _rows.Add(row);
            }
        }

        static object DefaultValue(Type t)
        {
            if (t == typeof(string)) return string.Empty;
            if (t.IsValueType) return Activator.CreateInstance(t);
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return null;
            try { return Activator.CreateInstance(t); }
            catch { return null; }
        }

        /// <summary>新增键的临时取值节点：不记 Undo、不回写真实对象。</summary>
        sealed class TempValueData : FieldData
        {
            readonly Type _type;
            object _value;

            public TempValueData(string name, Type type) : base(name)
            {
                _type = type;
                if (type == typeof(string)) _value = string.Empty;
                else if (type.IsValueType) _value = Activator.CreateInstance(type);
            }

            public override Type BaseType => _type;
            public object Value => _value;
            public override object GetValue() => _value;
            public override void SetValue(object value) => _value = value;
        }
    }
}
