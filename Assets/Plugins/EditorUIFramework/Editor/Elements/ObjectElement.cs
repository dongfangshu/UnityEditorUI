using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUIFramework
{
    /// <summary>
    /// 兜底元素：递归展开可序列化 class/struct 的实例字段，也是对象树的根。
    /// struct 时子值变更会把装箱实例经自身 FieldData 回写父级，保证嵌套编辑可持久化。
    /// </summary>
    public class ObjectElement : FieldElement
    {
        object _instance;
        VisualElement _body;

        public ObjectElement(FieldData data) : base(data) { }

        public override void Setup()
        {
            _instance = Data.GetValue();

            if (Data is RootFieldData)
            {
                _body = this; // 根节点不包 Foldout
            }
            else
            {
                var foldout = new Foldout { text = DisplayName, value = true };
                foldout.AddToClassList("eui-foldout");
                Add(foldout);
                _body = new VisualElement();
                _body.AddToClassList("eui-indent");
                foldout.Add(_body);
            }

            if (_instance == null)
            {
                var label = new Label("null（暂不支持创建实例）");
                label.AddToClassList("eui-unsupported");
                _body.Add(label);
                return;
            }

            foreach (var field in CollectFields(Data.BaseType))
            {
                var data = new ReflectionFieldData(field, _instance);
                var element = FieldElementFactory.Create(data);
                element.ParentFieldElement = this;
                element.UndoTarget = UndoTarget;
                _body.Add(element);
                element.Setup();
            }
        }

        /// <summary>子值变化冒泡：struct 装箱实例写回宿主后继续上冒。</summary>
        public override void OnValueChanged()
        {
            if (_instance != null && !(Data is RootFieldData) && Data.BaseType != null && Data.BaseType.IsValueType)
                Data.SetValue(_instance);
            base.OnValueChanged();
        }

        /// <summary>
        /// 收集可序列化实例字段：public（排除 [NonSerialized]）或带 [SerializeField] 的非 public；
        /// 沿继承链收集（基类在前）；排除 static/const/[HideInInspector]/编译器生成。
        /// </summary>
        internal static List<FieldInfo> CollectFields(Type type)
        {
            var stack = new Stack<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                stack.Push(t);

            var result = new List<FieldInfo>();
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                         BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var f in fields)
                {
                    if (f.IsStatic || f.IsLiteral) continue;
                    if (f.IsDefined(typeof(CompilerGeneratedAttribute), true)) continue;
                    if (f.IsDefined(typeof(HideInInspector), true)) continue;
                    if (f.IsPublic)
                    {
                        if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                        result.Add(f);
                    }
                    else if (f.IsDefined(typeof(SerializeField), false))
                    {
                        result.Add(f);
                    }
                }
            }
            return result;
        }
    }
}
