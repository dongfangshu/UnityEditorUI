namespace EditorUIFramework
{
    /// <summary>Lua 模式根元素：按条目袋经工厂构造子元素（数据源是 lua 注解，非反射）。</summary>
    public class LuaSchemaElement : FieldElement
    {
        public LuaSchemaElement(FieldData data) : base(data) { }

        protected override void OnSetup()
        {
            MainControl = this;
            var root = (LuaRootData)Data;
            foreach (var entry in root.Host.fields)
            {
                var element = FieldElementFactory.Create(new LuaFieldData(entry));
                element.ParentFieldElement = this;
                element.UndoTarget = UndoTarget;
                Add(element);
                element.Setup();
            }
        }
    }
}
