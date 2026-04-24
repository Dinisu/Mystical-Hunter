using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;


namespace App.BaseSystem.DataStores.ScriptableObjects.Status
{
    /// <summary>
    /// ステータスを持つオブジェクトのデータ群 (対象: プレイヤー、敵、破壊可能オブジェクトなど)
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObject/Data/CharacterStatus")]
    public class D_Ch_StatusData : BaseData
    {
        public int Level
        {
            get => level;
            set => level = value;
        }
        [SerializeField, Header("レベル")]
        private int level;

        public int Exp
        {
            get => exp;
            set => exp = value;
        }
        [SerializeField, Header("経験値")]
        private int exp;

        // リストを宣言し、内部の経験値クラス(PlayerExpTable)を入れる
        public List<ExpTable> LevelExp = new List<ExpTable>();
        //クラスをインスペクターに表示
        [System.Serializable]
        // 各レベルに達するまでの必要経験値が入っている内部クラス
        public class ExpTable//現在のLevelが参照する変数位置になる、LevelExp[Level]
        {
            public int Levelup;
            public int NeedExp;
            public int AcquisitionSp;
            public int MaxHpup;
            public int MaxMpup;
            public int Attackup;
            public int Magicup;
            public int Defenceup;
            public int MagicDefenseup;
            public int Speedup;
        }
        public int Sp
        {
            get => sp;
            set => sp = value;
        }
        [SerializeField, Header("スキルポイント")]
        private int sp;

        public enum Attribute
        {
            //上から無、火、水、木、金、土、神秘、呪い
            None,
            Fire,
            Water,
            Tree,
            Metal,
            Soil,
            Mystery,
            Curse
        }
        public Attribute SeeAttribute//参照時はこれを呼ぶ
        {
            get => attribute;
            set => attribute = value;
        }
        [SerializeField, Header("属性")]
        private Attribute attribute;
        public int MaxHp
        {
            get => maxhp;
            set => maxhp = value;
        }
        [SerializeField, Header("最大HP")]
        private int maxhp;

        public int Hp
        {
            get => hp;
            set => hp = Mathf.Clamp(value, 0, maxhp); // 0 <= hp <= maxhp
        }
        //[HideInInspector]
        [SerializeField]
        private int hp;
        public int MaxMp
        {
            get => maxmp;
            set => maxmp = value;
        }
        [SerializeField, Header("最大MP")]
        private int maxmp;

        public int Mp
        {
            get => mp;
            set => mp = Mathf.Clamp(value, 0, maxmp); // 0 <= hp <= maxmp
        }
        [SerializeField]
        private int mp;

        public int Attack
        {
            get => attack;
            set => attack = value;
        }
        [SerializeField, Header("攻撃力")]
        private int attack;

        public int Magic
        {
            get => magic;
            set => magic = value;
        }
        [SerializeField, Header("魔力")]
        private int magic;

        public int Defense
        {
            get => defense;
            set => defense = value;
        }
        [SerializeField, Header("防御力")]
        private int defense;

        public int MagicDefense
        {
            get => magicDefense;
            set => magicDefense = value;
        }
        [SerializeField, Header("魔法防御力")]
        private int magicDefense;

        public int Speed
        {
            get => speed;
            set => speed = value;
        }
        [SerializeField, Header("速度")]
        private int speed;

        public float CriticalRate
        {
            get => criticalRate;
            set => criticalRate = value;
        }
        [SerializeField, Header("クリティカル率")]
        private float criticalRate;

        public int Money
        {
            get => money;
            set => money = value;
        }
        [SerializeField, Header("所持金")]
        private int money;

        [SerializeField, Header("武器")]
        public D_It_StatusData Weapon;

        [SerializeField, Header("防具")]
        public D_It_StatusData Armor;

        [SerializeField, Header("アクセサリー1")]
        public D_It_StatusData Accessories1;
        [SerializeField, Header("アクセサリー2")]
        public D_It_StatusData Accessories2;


        [SerializeField, Header("覚えるスキルリスト")]//覚えてるスキルは向こうのフラグで管理
        public Db_Sk_StatusDataBase SkillList;

        [SerializeField, Header("受けているスキルバフ")]
        public List<ActiveBuff<D_Sk_StatusData>> ActiveBuffs = new();//スキルバフのデータ管理
        //[HideInInspector]
        [SerializeField, Header("受けているアイテムバフ")]
        public List<ActiveBuff<D_It_StatusData>> ActiveBuffs_It = new();//アイテムバフのデータ管理
        public enum Abnormalstatus
        {
            None
        }
        public Abnormalstatus SeeAbnormalstatus//参照時はこれを呼ぶ
        {
            get => abnormalstatus;
            set => abnormalstatus = value;
        }
        [SerializeField, Header("状態異常")]
        private Abnormalstatus abnormalstatus;

        [SerializeField, Header("キャラクター")]
        public GameObject Character;
        public GameObject B_Character;
        public GameObject B_Icon;
        public GameObject B_Icon_Ch;
        public GameObject Icon_Character1;
        public GameObject Icon_Character2;

        [SerializeField, Header("ドロップ用アイテムリスト")]
        public List<Droppedable> DroppedItems = new List<Droppedable>();

        [System.Serializable]
        // 各レベルに達するまでの必要経験値が入っている内部クラス
        public class Droppedable//現在のLevelが参照する変数位置になる、LevelExp[Level]
        {
            public D_It_StatusData DroppedItem;
            public int Droprate;
        }

        [SerializeField, Header("インベントリのステータスオブジェクト")]
        public GameObject Status;//消すかも

        [SerializeField, Header("イベント")]
        public D_Ev_StatusData Event;
    }
}