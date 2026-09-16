using System;
using System.Collections.Generic;

namespace UIDemo
{
    /// <summary>标记 int 字段为「实体选择器」：绘制重定向到 EntityIDSelectorDrawer（按钮 + 搜索分页弹窗）。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EntityIDSelectorAttribute : Attribute
    {
    }

    /// <summary>演示实体条目。</summary>
    public struct DemoEntity
    {
        public int Id;
        public string Name;
    }

    /// <summary>演示用静态实体库（真实项目可替换为配置表/运行时数据）。</summary>
    public static class DemoEntityDatabase
    {
        static readonly string[] Species =
            { "哥布林", "史莱姆", "骷髅兵", "兽人", "巨魔", "石像鬼", "亡灵法师", "精灵弓手", "人类骑士", "幼龙", "狼人", "小恶魔" };
        static readonly string[] Zones =
            { "新手村", "幽暗森林", "熔岩洞窟", "冰霜峡谷", "沙漠遗迹", "天空之城" };

        public static IReadOnlyList<DemoEntity> All { get; }

        static DemoEntityDatabase()
        {
            var list = new List<DemoEntity>();
            int id = 1001;
            foreach (var zone in Zones)
                foreach (var sp in Species)
                    list.Add(new DemoEntity { Id = id++, Name = $"{zone}·{sp}" });
            All = list;
        }

        public static bool TryFind(int id, out DemoEntity entity)
        {
            foreach (var e in All)
            {
                if (e.Id == id)
                {
                    entity = e;
                    return true;
                }
            }
            entity = default;
            return false;
        }
    }
}
