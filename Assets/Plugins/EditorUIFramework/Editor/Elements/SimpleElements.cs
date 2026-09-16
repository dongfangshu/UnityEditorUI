using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>bool → Toggle</summary>
    public class BoolElement : FieldElement
    {
        public BoolElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new Toggle(DisplayName) { value = Data.GetValue() is bool v && v };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>int → IntegerField；[Range] 时 → SliderInt</summary>
    public class IntElement : FieldElement
    {
        public IntElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var range = OdinCompat.Get(FieldInfo, OdinCompat.Range);
            if (range != null)
            {
                int min = (int)OdinCompat.Read<float>(range, "Min", "min");
                int max = (int)OdinCompat.Read<float>(range, "Max", "max");
                var slider = new SliderInt(DisplayName, min, max) { value = Convert.ToInt32(Data.GetValue() ?? 0) };
                slider.RegisterValueChangedCallback(e => CommitValue(e.newValue));
                MainControl = slider;
            }
            else
            {
                var field = new IntegerField(DisplayName) { value = Convert.ToInt32(Data.GetValue() ?? 0) };
                field.RegisterValueChangedCallback(e => CommitValue(ClampNumeric(e.newValue)));
                MainControl = field;
            }
            Add(MainControl);
        }
    }

    /// <summary>float → FloatField；[Range] 时 → Slider</summary>
    public class FloatElement : FieldElement
    {
        public FloatElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var range = OdinCompat.Get(FieldInfo, OdinCompat.Range);
            if (range != null)
            {
                var slider = new Slider(DisplayName, OdinCompat.Read<float>(range, "Min", "min"), OdinCompat.Read<float>(range, "Max", "max"))
                { value = Convert.ToSingle(Data.GetValue() ?? 0f) };
                slider.RegisterValueChangedCallback(e => CommitValue(e.newValue));
                MainControl = slider;
            }
            else
            {
                var field = new FloatField(DisplayName) { value = Convert.ToSingle(Data.GetValue() ?? 0f) };
                field.RegisterValueChangedCallback(e => CommitValue(ClampNumeric(e.newValue)));
                MainControl = field;
            }
            Add(MainControl);
        }
    }

    /// <summary>double → DoubleField</summary>
    public class DoubleElement : FieldElement
    {
        public DoubleElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new DoubleField(DisplayName) { value = Convert.ToDouble(Data.GetValue() ?? 0d) };
            field.RegisterValueChangedCallback(e => CommitValue(ClampNumeric(e.newValue)));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>long → LongField</summary>
    public class LongElement : FieldElement
    {
        public LongElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new LongField(DisplayName) { value = Convert.ToInt64(Data.GetValue() ?? 0L) };
            field.RegisterValueChangedCallback(e => CommitValue(ClampNumeric(e.newValue)));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>string → TextField；[TextArea] 时多行</summary>
    public class StringElement : FieldElement
    {
        public StringElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new TextField(DisplayName) { value = Data.GetValue() as string ?? string.Empty };
            if (OdinCompat.Has(FieldInfo, OdinCompat.TextArea))
                field.multiline = true;
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>enum → EnumField（自动识别 [Flags]）</summary>
    public class EnumElement : FieldElement
    {
        public EnumElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var type = Data.BaseType;
            var value = Data.GetValue() as Enum ?? (Enum)Activator.CreateInstance(type);
            var flags = type.IsDefined(typeof(FlagsAttribute), false);
            var field = new EnumField(DisplayName);
            field.Init(value, flags);
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>UnityEngine.Object 引用 → ObjectField</summary>
    public class ObjectFieldElement : FieldElement
    {
        public ObjectFieldElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new ObjectField(DisplayName)
            {
                objectType = Data.BaseType,
                value = Data.GetValue() as UnityEngine.Object
            };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>Color → ColorField</summary>
    public class ColorElement : FieldElement
    {
        public ColorElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new ColorField(DisplayName) { value = Data.GetValue() is Color c ? c : Color.white };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>兜底：不支持的类型。</summary>
    public class UnsupportedElement : FieldElement
    {
        readonly string _reason;

        public UnsupportedElement(FieldData data, string reason = "不支持的类型") : base(data)
        {
            _reason = reason;
        }

        protected override void OnSetup()
        {
            MainControl = UnsupportedLabel(_reason);
            Add(MainControl);
        }
    }
}
