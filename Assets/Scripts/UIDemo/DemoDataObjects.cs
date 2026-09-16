using System;
using System.Collections.Generic;
using EditorUIFramework;
using UnityEngine;

namespace UIDemo
{
    /// <summary>示例数据对象：玩家数据。</summary>
    [DataObject]
    public class DemoPlayerData
    {
        public string playerName = "玩家";
        public int level = 1;
        public float hp = 100f;
        public Vector3 spawnPoint = new Vector3(0f, 1f, 0f);
        public List<string> buffs = new List<string> { "加速" };
        public DemoNested detail = new DemoNested();
    }

    /// <summary>示例数据对象：商店配置。</summary>
    [DataObject]
    public class DemoShopData
    {
        public string shopName = "商店";
        public Color themeColor = Color.yellow;
        public Dictionary<string, int> prices = new Dictionary<string, int>();
    }
}
