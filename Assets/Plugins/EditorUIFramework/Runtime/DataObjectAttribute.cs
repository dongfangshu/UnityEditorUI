using System;

namespace EditorUIFramework
{
    /// <summary>
    /// 标记可作为 DataHost 托管数据对象的纯 C# 类。
    /// 限制：仅 class、非抽象、非泛型定义、有无参构造、不继承 UnityEngine.Object（SerializeReference 硬限制）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DataObjectAttribute : Attribute
    {
    }
}
