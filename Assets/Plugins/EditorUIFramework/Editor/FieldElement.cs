using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 所有字段元素的基类（继承 VisualElement）。两阶段契约：
    /// 构造仅存储 FieldData；Setup() 构建视觉、读初值、复合元素递归构造并 Setup 子元素。
    /// 整树构造成功后由 CustomEditorBase 调用根的 Setup() 级联组装。
    /// </summary>
    public abstract class FieldElement : VisualElement
    {
        /// <summary>本元素对应的数据节点。</summary>
        public FieldData Data { get; }

        /// <summary>Undo/SetDirty 的目标对象，由上层（CustomEditorBase 或复合元素）注入。</summary>
        public Object UndoTarget { get; set; }

        /// <summary>父元素，struct 回写冒泡链；由复合元素在构造子元素后设置。</summary>
        public FieldElement ParentFieldElement { get; set; }

        protected FieldElement(FieldData data)
        {
            Data = data;
            AddToClassList("eui-field");
        }

        /// <summary>第二阶段：构建视觉、读取初值、挂值变更回调；复合元素在此经工厂构造子元素并调用子 Setup()。</summary>
        public abstract void Setup();

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

        /// <summary>值变化冒泡：默认向上传递；持有 struct 实例的复合元素覆写以回写装箱实例。</summary>
        public virtual void OnValueChanged()
        {
            ParentFieldElement?.OnValueChanged();
        }

        /// <summary>美化后的显示名。</summary>
        protected string DisplayName => NameUtil.Nicify(Data.Name);

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
