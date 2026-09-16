
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>Vector2 → Vector2Field</summary>
    public class Vector2Element : FieldElement
    {
        public Vector2Element(FieldData data) : base(data) { }

        public override void Setup()
        {
            var field = new UnityEngine.UIElements.Vector2Field(DisplayName) { value = Data.GetValue() is Vector2 v ? v : default };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            Add(field);
        }
    }

    /// <summary>Vector3 → Vector3Field</summary>
    public class Vector3Element : FieldElement
    {
        public Vector3Element(FieldData data) : base(data) { }

        public override void Setup()
        {
            var field = new UnityEngine.UIElements.Vector3Field(DisplayName) { value = Data.GetValue() is Vector3 v ? v : default };
            field.RegisterValueChangedCallback(e => CommitValue(e.newValue));
            Add(field);
        }
    }
}
