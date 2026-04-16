using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System;
using static App.BaseSystem.DataStores.ScriptableObjects.Status.D_Ch_StatusData;
using System.Collections.Generic;
using GameConstants;

[Serializable]
public class SaveData
{
    //TODO :ここに保存したいパラメータを追加する

    public SceneName CurrentScene;
    //プレイヤーのいる位置
    public float PlayerPosX;
    public float PlayerPosY;
    public float PlayerPosZ;
    public bool ShouldRestorePlayerPosition;

    public int PlayerMoney;

    public float PlayTime;



    // リストを宣言し味方のステータス管理
    public List<AlliesData> AlliesStatus = new List<AlliesData>();
    [Serializable]
    public class AlliesData
    {
        public int Id;
        public int Level;
        public int Exp;
        public int Sp;
        public int MaxHp;
        public int Hp;
        public int MaxMp;
        public int Mp;
        public int Attack;
        public int Magic;
        public int Defense;
        public int MagicDefense;
        public int Speed;
        public float CriticalRate;

        //このキャラクターが現在装備しているアイテムのID
        public int Weapon;
        public int Armor;
        public int Accessories1;
        public int Accessories2;
    }

    public List<SkillData> SkillStatus = new List<SkillData>();
    [Serializable]
    public class SkillData
    {
        public int Id;
        public bool UnlockSkills;
    }

    public List<ItemData> ItemStatus = new List<ItemData>();
    [Serializable]
    public class ItemData
    {
        public int Id;
        public int Number;
    }

    public List<EventData> EventStatus = new List<EventData>();
    [Serializable]
    public class EventData
    {
        public int Id;
        public bool EventFlag1;
        public bool EventFlag2;
        public bool EventFlag3;
    }
}
