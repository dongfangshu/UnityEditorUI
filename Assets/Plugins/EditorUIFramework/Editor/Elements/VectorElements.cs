
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>Vector2 → Vector2Field（UnityEngine.UIElements 运行时版，2022.3）</summary>
    public class Vector2Element : FieldElement
    {
        public Vector2Element(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new Vector2Field(DisplayName) { value = Data.GetValue() is Vector2 v ? v : default };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }

    /// <summary>Vector3 → Vector3Field（UnityEngine.UIElements 运行时版，2022.3）</summary>
    public class Vector3Element : FieldElement
    {
        public Vector3Element(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            var field = new Vector3Field(DisplayName) { value = Data.GetValue() is Vector3 v ? v : default };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            MainControl = field;
            Add(field);
        }
    }
}
