using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;


namespace App.BaseSystem.DataStores.ScriptableObjects.Status
{
    [CreateAssetMenu(menuName = "ScriptableObject/Data/SkillStatus")]
    public class D_Sk_StatusData : BaseData
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

        public enum SkillRange
        {
            Himself,      // 自分
            Single_ally,  // 味方単体
            Alla_llies,   // 味方全体
            Single_enemy, // 敵単体
            All_enemies   // 敵全体 
        }
        public SkillRange SeeSkillRange//参照時はこれを呼ぶ
        {
            get => skillRange;
            set => skillRange = value;
        }
        [SerializeField, Header("スキル範囲")]
        private SkillRange skillRange;

        public enum Attack_or_Magic
        {
            Attack,
            Magic
        }
        public Attack_or_Magic SeeAttack_or_Magic//参照時はこれを呼ぶ
        {
            get => attack_or_Magic;
            set => attack_or_Magic = value;
        }
        [SerializeField, Header("攻撃/魔法区分")]
        private Attack_or_Magic attack_or_Magic;

        public enum Kinds
        {
            //アタック、ディフェンス、ファスト、スロー、クイック、回復、バフ、デバフ、アビリティ
            Attack,
            Defense,
            Fast,
            slow,
            Quick,
            Recovery,
            Buff,
            DeBuff,
            Abilities
        }
        public Kinds SeeKinds//参照時はこれを呼ぶ
        {
            get => kind;
            set => kind = value;
        }
        [SerializeField, Header("種類")]
        private Kinds kind;

        public enum Buff_DeBuff_Kinds// バフ・デバフ対象ステータス
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
        [SerializeField, Header("バフ対象ステータス")]
        private Buff_DeBuff_Kinds buff_DeBuff_kind;

        public enum Abnormalstatus
        {
            None
        }
        [SerializeField, Header("状態異常")]
        public Abnormalstatus SeeAbnormalstatus//蜿ら・譎ゅ・縺薙ｌ繧貞他縺ｶ
        {
            get => abnormalstatus;
            set => abnormalstatus = value;
        }
        private Abnormalstatus abnormalstatus;

        public float Efficacy1
        {
            get => efficacy1;
            set => efficacy1 = value;
        }
        [SerializeField, Header("威力、効力１")]
        private float efficacy1;
        public float Efficacy2
        {
            get => efficacy2;
            set => efficacy2 = value;
        }
        [SerializeField, Header("威力、効力２")]//効果が複数ある場合
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
        [SerializeField, Header("効果ターン")]//自身の行動終了時1ターン経過する、バフ付与時にも減少する
        private int duration;

        public int MpConsumption
        {
            get => mpConsumption;
            set => mpConsumption = value;
        }
        [SerializeField, Header("MP消費量")]
        private int mpConsumption;

        public int NeedSp
        {
            get => needSp;
            set => needSp = value;
        }
        [SerializeField, Header("必要SP")]
        private int needSp;

        [SerializeField, Header("解放されているか")]
        public bool Unlock;

        [SerializeField, Header("アイコン")]
        public GameObject Icon;

        [SerializeField, Header("バトル用アイコン")]
        public GameObject B_Icon;

        [SerializeField, Header("エフェクト")]
        public GameObject Effect;

        [SerializeField, Header("効果音")]
        public AudioClip SoundEffects;
    }
}
