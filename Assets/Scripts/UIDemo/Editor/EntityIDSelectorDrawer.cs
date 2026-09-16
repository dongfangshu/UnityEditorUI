using EditorUIFramework;
using UnityEngine.UIElements;

namespace UIDemo
{
    /// <summary>[EntityIDSelector] 的自定义绘制：按钮显示「实体名 : id」，点击打开实体搜索分页弹窗。</summary>
    public class EntityIDSelectorDrawer : CustomDraw<EntityIDSelectorAttribute>
    {
        Button _button;

        public EntityIDSelectorDrawer(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            if (Data.BaseType != typeof(int))
            {
                MainControl = UnsupportedLabel("[EntityIDSelector] 仅支持 int 字段");
                Add(MainControl);
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("eui-row");
            if (DisplayName.Length > 0)
            {
                var label = new Label(DisplayName);
                label.AddToClassList("eui-field-label");
                row.Add(label);
            }

            _button = new Button(ShowPopup) { text = Describe(CurrentId) };
            _button.AddToClassList("eui-enum-button");
            _button.style.flexGrow = 1;
            row.Add(_button);

            MainControl = row;
            Add(row);
        }

        int CurrentId => Data.GetValue() is int v ? v : 0;

        void ShowPopup()
        {
            EntityPickerPopup.Open(_button.worldBound, CurrentId, id =>
            {
                CommitValue(id);
                _button.text = Describe(id);
            });
        }

        static string Describe(int id)
        {
            if (DemoEntityDatabase.TryFind(id, out var e))
                return $"{e.Name} : {e.Id}";
            return id == 0 ? "未设置" : $"未找到 : {id}";
        }
    }
}
