using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIDemo
{
    public enum DemoQuality
    {
        Low,
        Medium,
        High
    }

    [Flags]
    public enum DemoFlags
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4
    }

    [Serializable]
    public class DemoNested
    {
        public string title = "嵌套类";
        public int count = 3;
        public Color tint = Color.cyan;
    }

    [Serializable]
    public struct DemoStruct
    {
        public float weight;
        public Vector2 pivot;
    }

    /// <summary>框架示例：覆盖 v1 全部已支持类型。</summary>
    public class DemoModel : MonoBehaviour
    {
        public bool enabledFlag = true;
        public int intValue = 42;
        public float floatValue = 1.5f;
        public double doubleValue = 3.14;
        public long longValue = 1234567890123L;
        public string text = "hello";
        public DemoQuality quality = DemoQuality.Medium;
        public DemoFlags flags = DemoFlags.A | DemoFlags.B;
        public Transform targetTransform;
        public Texture2D icon;
        public Vector2 vec2 = new Vector2(1f, 2f);
        public Vector3 vec3 = new Vector3(1f, 2f, 3f);
        public Color color = Color.green;
        public List<int> scores = new List<int> { 1, 2, 3 };
        public List<string> tags = new List<string> { "a", "b" };
        public List<DemoStruct> structList = new List<DemoStruct>();
        public Dictionary<string, int> inventory = new Dictionary<string, int>();
        public DemoNested nested = new DemoNested();
        public DemoStruct structField;

        [SerializeField]
        int privateBacked = 7;
    }
}
