using System;
using System.Collections;
using System.Reflection;

namespace EditorUIFramework
{
    /// <summary>
    /// 统一的「成员取值」抽象：字段 / 列表元素 / 字典项 / 根对象。
    /// 构造时捕获宿主实例；GetValue/SetValue 对宿主直接读写。
    /// </summary>
    public abstract class FieldData
    {
        /// <summary>显示名（字段名美化前的原名）。</summary>
        public string Name { get; }

        /// <summary>值类型，工厂分派依据。</summary>
        public abstract Type BaseType { get; }

        protected FieldData(string name)
        {
            Name = name;
        }

        public abstract object GetValue();
        public abstract void SetValue(object value);
    }

    /// <summary>反射字段（owner 为字段所属实例，可为装箱 struct）。</summary>
    public class ReflectionFieldData : FieldData
    {
        readonly FieldInfo _field;
        readonly object _owner;

        public ReflectionFieldData(FieldInfo field, object owner) : base(field.Name)
        {
            _field = field;
            _owner = owner;
        }

        public override Type BaseType => _field.FieldType;
        public override object GetValue() => _field.GetValue(_owner);
        public override void SetValue(object value) => _field.SetValue(_owner, value);
    }

    /// <summary>IList 索引元素（list[index] 读写）。</summary>
    public class ListItemData : FieldData
    {
        readonly IList _list;
        readonly int _index;
        readonly Type _elementType;

        public ListItemData(IList list, int index, Type elementType) : base($"Element {index}")
        {
            _list = list;
            _index = index;
            _elementType = elementType;
        }

        public override Type BaseType => _elementType;
        public override object GetValue() => _list[_index];
        public override void SetValue(object value) => _list[_index] = value;
    }

    /// <summary>IDictionary 键值项的值（dict[key] 读写）。Name 置空，键由 DictElement 单独展示。</summary>
    public class DictEntryData : FieldData
    {
        readonly IDictionary _dict;
        readonly object _key;
        readonly Type _valueType;

        public DictEntryData(IDictionary dict, object key, Type valueType) : base(string.Empty)
        {
            _dict = dict;
            _key = key;
            _valueType = valueType;
        }

        public override Type BaseType => _valueType;
        public override object GetValue() => _dict[_key];
        public override void SetValue(object value) => _dict[_key] = value;
    }

    /// <summary>根对象（被检视的 target 本身）。</summary>
    public class RootFieldData : FieldData
    {
        readonly object _target;

        public RootFieldData(object target) : base(target != null ? target.GetType().Name : "Root")
        {
            _target = target;
        }

        public override Type BaseType => _target?.GetType();
        public override object GetValue() => _target;
        public override void SetValue(object value) => throw new NotSupportedException("根对象不支持 SetValue");
    }
}
