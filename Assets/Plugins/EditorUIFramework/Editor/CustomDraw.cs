using System;
using System.Collections.Generic;
using UnityEditor;

namespace EditorUIFramework
{
    /// <summary>
    /// 自定义绘制基类（非泛型）。字段上打了某个特性时，工厂将绘制重定向到该特性注册的 CustomDraw 子类。
    /// 特性实例由 CustomDrawerRegistry 在构造后注入（BoundAttribute）。
    /// </summary>
    public abstract class CustomDraw : FieldElement
    {
        /// <summary>触发重定向的特性实例（构造后、Setup 前注入）。</summary>
        internal Attribute BoundAttribute;

        protected CustomDraw(FieldData data) : base(data) { }
    }

    /// <summary>
    /// 自定义绘制基类（泛型）：TAttribute 即触发重定向的特性类型。
    /// 子类需公开构造函数 (FieldData)。通过 Attr 访问字段上的特性实例。
    /// </summary>
    public abstract class CustomDraw<TAttribute> : CustomDraw where TAttribute : Attribute
    {
        /// <summary>字段上的特性实例。</summary>
        protected TAttribute Attr => (TAttribute)BoundAttribute;

        protected CustomDraw(FieldData data) : base(data) { }
    }

    /// <summary>
    /// 特性 → 自定义绘制 注册表。惰性扫描所有 CustomDraw 子类（TypeCache），按 CustomDraw&lt;TAttribute&gt; 的泛型实参建映射。
    /// 匹配时沿特性继承链向上查找（特性基类注册的绘制器对派生特性同样生效）。
    /// </summary>
    public static class CustomDrawerRegistry
    {
        static Dictionary<Type, Type> _map; // attributeType -> drawerType

        static void EnsureScanned()
        {
            if (_map != null) return;
            _map = new Dictionary<Type, Type>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<CustomDraw>())
            {
                if (t.IsAbstract) continue;
                for (var b = t.BaseType; b != null && b != typeof(object); b = b.BaseType)
                {
                    if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(CustomDraw<>))
                    {
                        _map[b.GetGenericArguments()[0]] = t;
                        break;
                    }
                }
            }
        }

        /// <summary>若字段上存在已注册绘制器的特性，构造对应 CustomDraw；否则返回 false。</summary>
        public static bool TryCreate(FieldData data, out FieldElement element)
        {
            element = null;
            if (!(data is ReflectionFieldData rfd)) return false;
            EnsureScanned();

            foreach (var attrObj in rfd.Info.GetCustomAttributes(true))
            {
                var attr = (Attribute)attrObj;
                for (var t = attr.GetType(); t != null && t != typeof(object) && t != typeof(Attribute); t = t.BaseType)
                {
                    if (!_map.TryGetValue(t, out var drawerType)) continue;
                    var drawer = (CustomDraw)Activator.CreateInstance(drawerType, data);
                    drawer.BoundAttribute = attr;
                    element = drawer;
                    return true;
                }
            }
            return false;
        }
    }
}
