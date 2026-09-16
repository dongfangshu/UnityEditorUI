using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 所有字段元素的基类（继承 VisualElement）。两阶段契约：
    /// 构造仅存储 FieldData；Setup() 模板方法 = OnSetup() 建控件 → ApplyAttributes() 统一应用 Odin 特性修饰。
    /// 整树构造成功后由 CustomEditorBase 调用根的 Setup() 级联组装。
    /// </summary>
    public abstract class FieldElement : VisualElement
    {
        /// <summary>本元素对应的数据节点。</summary>
        public FieldData Data { get; }

        /// <summary>Undo/SetDirty 的目标对象，由上层（CustomEditorBase 或复合元素）注入。</summary>
        public Object UndoTarget { get; set; }

        /// <summary>父元素，struct 回写与条件重估的冒泡链；由复合元素在构造子元素后设置。</summary>
        public FieldElement ParentFieldElement { get; set; }

        /// <summary>元素的主控件（OnSetup 中赋值），特性修饰（tooltip/禁用等）作用于它。</summary>
        protected VisualElement MainControl { get; set; }

        System.Func<bool> _visibilityCondition;
        List<FieldElement> _conditionals; // 仅根节点使用

        protected FieldElement(FieldData data)
        {
            Data = data;
            AddToClassList("eui-field");
        }

        /// <summary>第二阶段（模板方法）：先建控件，再应用特性。由根级联调用。</summary>
        public void Setup()
        {
            OnSetup();
            ApplyAttributes();
        }

        /// <summary>构建视觉、读取初值、挂值变更回调；复合元素在此经工厂构造子元素并调用子 Setup()。</summary>
        protected abstract void OnSetup();

        // ---------- Odin 特性管道 ----------

        /// <summary>底层反射字段（仅 ReflectionFieldData 有，list/dict 元素为 null）。</summary>
        protected FieldInfo FieldInfo => (Data as ReflectionFieldData)?.Info;

        /// <summary>字段所属实例（条件求值用）。</summary>
        protected object OwnerInstance => (Data as ReflectionFieldData)?.Owner;

        /// <summary>美化显示名；被 [LabelText] 覆盖、被 [HideLabel] 置空。</summary>
        protected string DisplayName
        {
            get
            {
                var f = FieldInfo;
                if (f != null)
                {
                    if (OdinCompat.Has(f, OdinCompat.HideLabel))
                        return string.Empty;
                    var labelText = OdinCompat.Get(f, OdinCompat.LabelText);
                    if (labelText != null)
                    {
                        var s = OdinCompat.Read<string>(labelText, "Text");
                        if (!string.IsNullOrEmpty(s)) return s;
                    }
                }
                return NameUtil.Nicify(Data.Name);
            }
        }

        void ApplyAttributes()
        {
            var f = FieldInfo;
            if (f == null || MainControl == null) return;

            var tooltip = OdinCompat.Get(f, OdinCompat.Tooltip);
            if (tooltip != null)
            {
                var s = OdinCompat.Read<string>(tooltip, "TooltipText", "tooltip");
                if (!string.IsNullOrEmpty(s)) MainControl.tooltip = s;
            }

            if (OdinCompat.Has(f, OdinCompat.ReadOnly))
                MainControl.SetEnabled(false);

            var title = OdinCompat.Get(f, OdinCompat.Title) ?? OdinCompat.Get(f, OdinCompat.Header);
            if (title != null)
            {
                var s = OdinCompat.Read<string>(title, "Title", "header");
                if (!string.IsNullOrEmpty(s))
                {
                    var header = new Label(s);
                    header.AddToClassList("eui-header");
                    Insert(0, header);
                }
            }

            var showIf = OdinCompat.Get(f, OdinCompat.ShowIf);
            var hideIf = OdinCompat.Get(f, OdinCompat.HideIf);
            var showCond = showIf != null ? OdinCompat.Read<string>(showIf, "Condition") : null;
            var hideCond = hideIf != null ? OdinCompat.Read<string>(hideIf, "Condition") : null;
            if (!string.IsNullOrEmpty(showCond) || !string.IsNullOrEmpty(hideCond))
            {
                var owner = OwnerInstance;
                _visibilityCondition = () =>
                {
                    bool visible = string.IsNullOrEmpty(showCond) || EvalCondition(owner, showCond);
                    if (visible && !string.IsNullOrEmpty(hideCond))
                        visible = !EvalCondition(owner, hideCond);
                    return visible;
                };
                Root().RegisterConditional(this);
                RefreshVisibility();
            }
        }

        /// <summary>求值条件：同对象上的 bool 字段/属性/无参方法，支持 ! 前缀。成员不存在时保持显示。</summary>
        static bool EvalCondition(object owner, string condition)
        {
            if (owner == null || string.IsNullOrEmpty(condition)) return true;
            bool negate = condition.StartsWith("!");
            var name = negate ? condition.Substring(1) : condition;
            var t = owner.GetType();
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var field = t.GetField(name, Flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                var v = (bool)field.GetValue(owner);
                return negate ? !v : v;
            }
            var prop = t.GetProperty(name, Flags);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
            {
                var v = (bool)prop.GetValue(owner);
                return negate ? !v : v;
            }
            var method = t.GetMethod(name, Flags, null, System.Type.EmptyTypes, null);
            if (method != null && method.ReturnType == typeof(bool))
            {
                var v = (bool)method.Invoke(owner, null);
                return negate ? !v : v;
            }
            return true;
        }

        FieldElement Root()
        {
            var e = this;
            while (e.ParentFieldElement != null) e = e.ParentFieldElement;
            return e;
        }

        internal void RegisterConditional(FieldElement element)
        {
            (_conditionals ??= new List<FieldElement>()).Add(element);
        }

        void RefreshVisibility()
        {
            style.display = _visibilityCondition == null || _visibilityCondition()
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>数值夹取（[MinValue]/[MaxValue]），无特性时原样返回。</summary>
        protected object ClampNumeric(object value)
        {
            var f = FieldInfo;
            if (f == null || value == null) return value;
            try
            {
                dynamic v = value;
                var minAttr = OdinCompat.Get(f, OdinCompat.MinValue);
                if (minAttr != null)
                {
                    dynamic min = System.Convert.ChangeType(OdinCompat.Read<object>(minAttr, "MinValue"), Data.BaseType);
                    if (v < min) v = min;
                }
                var maxAttr = OdinCompat.Get(f, OdinCompat.MaxValue);
                if (maxAttr != null)
                {
                    dynamic max = System.Convert.ChangeType(OdinCompat.Read<object>(maxAttr, "MaxValue"), Data.BaseType);
                    if (v > max) v = max;
                }
                return v;
            }
            catch { return value; }
        }

        // ---------- 值写回与冒泡 ----------

        /// <summary>控件值变更统一入口：Undo.RecordObject → SetValue → SetDirty → 冒泡。</summary>
        protected void CommitValue(object value)
        {
            if (UndoTarget != null)
                Undo.RecordObject(UndoTarget, $"Change {Data.Name}");
            Data.SetValue(value);
            if (UndoTarget != null)
                EditorUtility.SetDirty(UndoTarget);
            OnValueChanged();
        }

        /// <summary>值变化冒泡：到根时重估全部条件可见性；否则向上传递；持有 struct 实例的复合元素覆写以回写。</summary>
        public virtual void OnValueChanged()
        {
            if (ParentFieldElement == null)
            {
                if (_conditionals != null)
                    foreach (var c in _conditionals) c.RefreshVisibility();
            }
            else
            {
                ParentFieldElement.OnValueChanged();
            }
        }

        /// <summary>构造「不支持」提示标签。</summary>
        protected Label UnsupportedLabel(string reason)
        {
            var label = new Label($"{DisplayName}: {reason}");
            label.AddToClassList("eui-unsupported");
            return label;
        }
    }

    /// <summary>字段名美化：去 m_/_ 前缀、驼峰拆词、首字母大写。</summary>
    internal static class NameUtil
    {
        public static string Nicify(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var s = name;
            if (s.StartsWith("m_")) s = s.Substring(2);
            else if (s.StartsWith("_")) s = s.Substring(1);
            if (s.Length == 0) return name;

            var sb = new StringBuilder(s.Length + 4);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && (char.IsLower(s[i - 1]) || char.IsDigit(s[i - 1])))
                    sb.Append(' ');
                sb.Append(i == 0 ? char.ToUpperInvariant(c) : c);
            }
            return sb.ToString();
        }
    }
}
