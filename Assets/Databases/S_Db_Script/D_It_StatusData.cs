using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;


namespace App.BaseSystem.DataStores.ScriptableObjects.Status
{
    [CreateAssetMenu(menuName = "ScriptableObject/Data/ItemStatus")]
    public class D_It_StatusData : BaseData
    {
        [TextArea, Header("効果説明")]
        public string EfficacyItemDescription;

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

        public enum ItemRange
        {
            Himself,//自身
            Single_ally,//味方単体
            Alla_llies,//味方全体
            Single_enemy,//敵単体
            All_enemies//敵全体
        }
        public ItemRange SeeItemRange//参照時はこれを呼ぶ
        {
            get => itemRange;
            set => itemRange = value;
        }
        [SerializeField, Header("アイテム範囲")]
        private ItemRange itemRange;

        public enum Kinds
        {
            //上から武器、防具、アクセサリー、バフ、デバフ、攻撃、魔法、回復、貴重品
            Weapon,
            Armor,
            Accessories,
            Buff,
            DeBuff,
            Attack,
            Magic,
            HP_Recovery,
            MP_Recovery,
            Valuables
        }
        public Kinds SeeKinds//参照時はこれを呼ぶ
        {
            get => kinds;
            set => kinds = value;
        }
        [SerializeField, Header("種類")]
        private Kinds kinds;

        public enum Buff_DeBuff_Kinds//バフ、デバフ、の種類
        {
            None,
            Attack,
            Magic,
            Defense,
            MagicDefense,
            Speed,
            Critical
        }
        public Buff_DeBuff_Kinds SeeBuff_DeBuff_Kinds//参照時はこれを呼ぶ
        {
            get => buff_DeBuff_kind;
            set => buff_DeBuff_kind = value;
        }
        [SerializeField, Header("バフ種類")]
        private Buff_DeBuff_Kinds buff_DeBuff_kind;

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

        [Header("装備ステータス(武器、防具、アクセサリー用)")]
        public EquipmentStatus Equipment;
        //クラスをインスペクターに表示
        [System.Serializable]
        // 内部クラス
        public class EquipmentStatus
        {
            public int MaxHpup;
            public int MaxMpup;
            public int Attackup;
            public int Magicup;
            public int Defenceup;
            public int MagicDefenseup;
            public int Speedup;
            public int Criticalup;
        }


        public float Efficacy1
        {
            get => efficacy1;
            set => efficacy1 = value;
        }
        [SerializeField, Header("威力、効力1")]
        private float efficacy1;
        public float Efficacy2
        {
            get => efficacy2;
            set => efficacy2 = value;
        }
        [SerializeField, Header("威力、効力2")]//効果が複数ある場合
        private float efficacy2;

        public float CriticalRate
        {
            get => criticalRate;
            set => criticalRate = value;
        }
        [SerializeField, Header("クリティカル率")]
        private float criticalRate;

        public int Duration
        {
            get => duration;
            set => duration = value;
        }
        [SerializeField, Header("効果ターン")]//自身の攻撃が終了すると1ターン経過
        private int duration;

        public int Number
        {
            get => number;
            set => number = value;
        }
        [SerializeField, Header("所持数")]
        private int number;

        public int Price
        {
            get => price;
            set => price = value;
        }
        [SerializeField, Header("値段")]
        private int price;

        [SerializeField, Header("アイコン")]
        public GameObject Icon;

        [SerializeField, Header("データアイコン画像")]
        public Sprite DataIcon;

        [SerializeField, Header("バトルアイコン")]
        public GameObject B_Icon;

        [SerializeField, Header("エフェクト")]
        public GameObject Effect;

        [SerializeField, Header("効果音")]
        public AudioClip SoundEffects;

        [SerializeField, Header("イベント")]
        public D_Ev_StatusData Event;
    }
}