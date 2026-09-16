using System;
using System.Collections;
using System.Globalization;

namespace EditorUIFramework
{
    /// <summary>
    /// Lua 条目字段数据：BaseType 取 lua 类型映射（LuaTypeMap，Runtime）。
    /// 基元：字符串↔类型双向 Convert；集合：物化缓存实例，SetValue 序列化回 lua 字面量。
    /// </summary>
    public class LuaFieldData : FieldData
    {
        readonly LuaFieldEntry _entry;
        object _collectionCache; // 集合类型的物化实例（物化一次，编辑共享同一实例）

        public LuaFieldData(LuaFieldEntry entry) : base(entry.name)
        {
            _entry = entry;
        }

        public override Type BaseType => LuaTypeMap.Map(_entry.type);

        bool IsCollection
        {
            get
            {
                var t = BaseType;
                return t != null && t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t);
            }
        }

        public override object GetValue()
        {
            var t = BaseType;
            if (t == null) return null;
            if (IsCollection)
                return _collectionCache ?? (_collectionCache = LuaValue.ParseTo(t, _entry.value));
            if (string.IsNullOrEmpty(_entry.value)) return Default(t);
            try { return Convert.ChangeType(_entry.value, t, CultureInfo.InvariantCulture); }
            catch { return Default(t); }
        }

        public override void SetValue(object value)
        {
            if (IsCollection)
            {
                _collectionCache = value;
                _entry.value = LuaValue.ToLua(value);
                return;
            }
            _entry.value = value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        internal static object Default(Type t)
        {
            if (t == typeof(string)) return string.Empty;
            if (t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t))
                return LuaValue.ParseTo(t, "{}"); // 集合默认空表
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }

    /// <summary>Lua 实例根节点数据：工厂谓词拦截后由 LuaSchemaElement 处理。</summary>
    public class LuaRootData : FieldData
    {
        readonly LuaHost _host;

        public LuaRootData(LuaHost host, string className) : base(className ?? "LuaObject")
        {
            _host = host;
        }

        public LuaHost Host => _host;

        public override Type BaseType => typeof(object);
        public override object GetValue() => _host;
        public override void SetValue(object value) => throw new NotSupportedException("Lua 根节点不支持 SetValue");
    }
}
