using System;
using System.Collections;
using System.Collections.Generic;

namespace EditorUIFramework
{
    /// <summary>
    /// 类型工厂：接受 FieldData，按 BaseType 派生不同 FieldElement。
    /// 分派优先级：自定义绘制特性（CustomDraw）&gt; 注册表精确匹配 &gt; 外部谓词链 &gt; 内建判定（enum/Obj引用/List/Dict/嵌套对象）&gt; Unsupported。
    /// </summary>
    public static class FieldElementFactory
    {
        static readonly Dictionary<Type, Func<FieldData, FieldElement>> Exact =
            new Dictionary<Type, Func<FieldData, FieldElement>>();

        static readonly List<Func<FieldData, FieldElement>> Custom =
            new List<Func<FieldData, FieldElement>>();

        static FieldElementFactory()
        {
            Register(typeof(bool), d => new BoolElement(d));
            Register(typeof(int), d => new IntElement(d));
            Register(typeof(float), d => new FloatElement(d));
            Register(typeof(double), d => new DoubleElement(d));
            Register(typeof(long), d => new LongElement(d));
            Register(typeof(string), d => new StringElement(d));
            Register(typeof(UnityEngine.Vector2), d => new Vector2Element(d));
            Register(typeof(UnityEngine.Vector3), d => new Vector3Element(d));
            Register(typeof(UnityEngine.Color), d => new ColorElement(d));
        }

        /// <summary>注册精确类型映射（可覆盖内建映射）。</summary>
        public static void Register(Type type, Func<FieldData, FieldElement> ctor)
        {
            Exact[type] = ctor;
        }

        /// <summary>注册谓词构造器，返回 null 表示不匹配；后注册的先匹配。</summary>
        public static void RegisterProvider(Func<FieldData, FieldElement> provider)
        {
            Custom.Insert(0, provider);
        }

        /// <summary>按 FieldData.BaseType 构造对应元素；未知类型落 UnsupportedElement。</summary>
        public static FieldElement Create(FieldData data)
        {
            // 根节点始终展开自身字段，不按 UnityEngine.Object 引用处理
            if (data is RootFieldData)
                return new ObjectElement(data);

            // 自定义绘制重定向：字段特性注册了 CustomDraw 时优先级最高
            if (CustomDrawerRegistry.TryCreate(data, out var custom))
                return custom;

            var t = data.BaseType;
            if (t != null)
            {
                if (Exact.TryGetValue(t, out var ctor))
                    return ctor(data);

                for (int i = 0; i < Custom.Count; i++)
                {
                    var e = Custom[i](data);
                    if (e != null) return e;
                }

                var builtin = CreateBuiltin(data, t);
                if (builtin != null) return builtin;
            }
            return new UnsupportedElement(data, $"不支持的类型 {(t != null ? t.Name : "null")}");
        }

        static FieldElement CreateBuiltin(FieldData data, Type t)
        {
            if (t.IsEnum) return new EnumElement(data);
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return new ObjectFieldElement(data);
            if (t.IsArray) return null; // T[] 留待后续（见 map 雾区）
            if (typeof(IDictionary).IsAssignableFrom(t)) return new DictElement(data);
            if (typeof(IList).IsAssignableFrom(t)) return new ListElement(data);
            if (typeof(Delegate).IsAssignableFrom(t)) return null;
            if (t.IsClass || (t.IsValueType && !t.IsPrimitive)) return new ObjectElement(data);
            return null;
        }
    }
}
