using UnityEngine;

namespace EditorUIFramework
{
    /// <summary>
    /// 通用数据宿主：把纯 C# 数据对象（[DataObject] 标记的 class）挂到场景对象上，
    /// 由 DataHostEditor 在 Inspector 中选择类型并反射建树编辑。
    /// [SerializeReference] 是持久化的关键：缺它域重载后 target 变 null。
    /// </summary>
    public class DataHost : MonoBehaviour
    {
        [SerializeReference]
        public object target;
    }
}
