using System;
using System.Reflection;

namespace EditorUIFramework
{
    /// <summary>
    /// 按全名反射读取特性（Sirenix Odin + Unity 内建），不产生编译期依赖：
    /// 项目无 Odin 时所有读取落空，框架行为与无特性一致。
    /// 注意：Odin 设计上直接复用 Unity 内建特性（Range/Tooltip/TextArea/Header），
    /// 所以候选名同时覆盖 Sirenix.OdinInspector.* 与 UnityEngine.*。
    /// </summary>
    internal static class OdinCompat
    {
        public static readonly string[] LabelText = { "Sirenix.OdinInspector.LabelTextAttribute" };
        public static readonly string[] Tooltip = { "Sirenix.OdinInspector.TooltipAttribute", "UnityEngine.TooltipAttribute" };
        public static readonly string[] HideLabel = { "Sirenix.OdinInspector.HideLabelAttribute" };
        public static readonly string[] ReadOnly = { "Sirenix.OdinInspector.ReadOnlyAttribute" };
        public static readonly string[] Range = { "Sirenix.OdinInspector.RangeAttribute", "UnityEngine.RangeAttribute" };
        public static readonly string[] MinValue = { "Sirenix.OdinInspector.MinValueAttribute" };
        public static readonly string[] MaxValue = { "Sirenix.OdinInspector.MaxValueAttribute" };
        public static readonly string[] ShowIf = { "Sirenix.OdinInspector.ShowIfAttribute" };
        public static readonly string[] HideIf = { "Sirenix.OdinInspector.HideIfAttribute" };
        public static readonly string[] Title = { "Sirenix.OdinInspector.TitleAttribute" };
        public static readonly string[] Header = { "UnityEngine.HeaderAttribute" };
        public static readonly string[] TextArea = { "Sirenix.OdinInspector.TextAreaAttribute", "UnityEngine.TextAreaAttribute", "UnityEngine.MultilineAttribute" };
        public static readonly string[] Button = { "Sirenix.OdinInspector.ButtonAttribute" };

        /// <summary>取成员上任一候选全名的特性实例，无则 null。</summary>
        public static object Get(MemberInfo member, params string[] attrFullNames)
        {
            if (member == null) return null;
            foreach (var attr in member.GetCustomAttributes(true))
            {
                var fullName = attr.GetType().FullName;
                foreach (var name in attrFullNames)
                    if (fullName == name)
                        return attr;
            }
            return null;
        }

        public static bool Has(MemberInfo member, params string[] attrFullNames) => Get(member, attrFullNames) != null;

        /// <summary>读特性实例的公共属性或字段，按候选名依次尝试（Odin 与 Unity 命名风格不同）。</summary>
        public static T Read<T>(object attr, params string[] names)
        {
            if (attr == null) return default;
            var t = attr.GetType();
            foreach (var name in names)
            {
                var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanRead) return (T)prop.GetValue(attr);
                var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (field != null) return (T)field.GetValue(attr);
            }
            return default;
        }
    }
}
