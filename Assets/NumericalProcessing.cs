using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using static UnityEditor.Progress;

public class NumericalProcessing : MonoBehaviour
{
    [SerializeField]
    public InventManager InventManager;
    [SerializeField, Header("使用データ")]
    public D_Ch_StatusData Use_ChData;//使用するキャラクターデータ
    public D_Ch_StatusData Use_subject_ChData;//使用される対象データ
    public GameObject Use_characterObject;//使用される対象オブジェクト
    public D_It_StatusData ItemUse_ItData;//使用するアイテムデータ
    public D_Sk_StatusData SkillUse_SkData;

    //キャラクターデータストア
    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;
    private Dss_It_StatusDataStores dss_It_StatusDataStores;
    private Ds_Sk_StatusDataStore ds_Sk_StatusDataStore;
    //private D_Sk_StatusData ReferenceSkills;

    private Db_It_StatusDataBase db_PlayerItem;//プレイヤーの所持アイテムデータ

    // アイテムの種類（Kinds）と、それに対応する処理（Action）を紐づける辞書
    private Dictionary<D_It_StatusData.Kinds, Action> itemActions;

    /// <summary>被ダメージ点滅用。同じキャラに連続ヒットしたとき前の Sequence を Kill する。</summary>
    private readonly Dictionary<int, Sequence> _hitBlinkSequences = new Dictionary<int, Sequence>();

    private void Awake()
    {
        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();
        dss_It_StatusDataStores = FindObjectOfType<Dss_It_StatusDataStores>();
        ds_Sk_StatusDataStore = FindObjectOfType<Ds_Sk_StatusDataStore>();


        // Dictionaryを生成
        // 「キー：アイテム種類」「値：実行するメソッド」
        itemActions = new Dictionary<D_It_StatusData.Kinds, Action>()
        {
            // ===== 装備系 =====
            { D_It_StatusData.Kinds.Weapon, WeaponEquipment },       // 武器装備処理
            { D_It_StatusData.Kinds.Armor, ArmorEquipment },         // 防具装備処理
            { D_It_StatusData.Kinds.Accessories, AccessoriesEquipment }, // アクセサリー装備処理

            // ===== ステータス変化系 =====
            { D_It_StatusData.Kinds.Buff, It_Buff },                 // バフ処理
            { D_It_StatusData.Kinds.DeBuff, It_DeBuff },             // デバフ処理
 
            // ===== 攻撃・魔法系 =====
            { D_It_StatusData.Kinds.Attack, It_Attack },             // 攻撃アイテム処理
            { D_It_StatusData.Kinds.Magic, It_Magic },               // 魔法アイテム処理
  
            // ===== 回復系 =====
            { D_It_StatusData.Kinds.HP_Recovery, It_HP_Recovery },   // HP回復処理
            { D_It_StatusData.Kinds.MP_Recovery, It_MP_Recovery }    // MP回復処理

            //{ D_It_StatusData.Kinds.Valuables, 実装予定 }//貴重品
        };
    }
    void Start()
    {
        // FindDatabaseWithName を使用して Player_Item データベースを取得
        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Player_Item");
    }
    //---スキル計算---//

    /// <summary>
    /// バフ・デバフの効果を一時的に適用して、元の数値を記録する構造体
    /// </summary>
    public class TempBuffRestoreData
    {
        public D_Ch_StatusData character;
        public float originalAttack;
        public float originalMagic;
        public float originalDefense;
        public float originalMagicDefense;
        public float originalSpeed;
    }

    public void ApplyBuff(D_Ch_StatusData target, D_Sk_StatusData buffData)//バフ、デバフ取得
    {
        // 既に同じ種類のバフがある場合は上書き
        var existing = target.ActiveBuffs.Find(b => b.baseData == buffData);
        if (existing != null)
        {
            existing.remainingTurns = buffData.Duration;
        }
        else
        {
            target.ActiveBuffs.Add(new ActiveBuff(buffData));
        }
    }

    /// <summary>
    /// キャラクターのバフ・デバフを実際の数値に一時反映する
    /// </summary>
    public TempBuffRestoreData ApplyCurrentBuffDeBuffEffects(D_Ch_StatusData characterData)
    {
        if (characterData == null) return null;

        // 元の値を保存
        var restoreData = new TempBuffRestoreData
        {
            character = characterData,
            originalAttack = characterData.Attack,
            originalMagic = characterData.Magic,
            originalDefense = characterData.Defense,
            originalMagicDefense = characterData.MagicDefense,
            originalSpeed = characterData.Speed
        };

        // スキル由来バフの適用
        ApplyBuffList(characterData, characterData.ActiveBuffs);

        // アイテム由来バフの適用
        ApplyBuffList_It(characterData, characterData.ActiveBuffs_It);

        return restoreData;
    }

    /// <summary>
    /// バフ・デバフを解除し、元の数値に戻す
    /// </summary>
    public void RestoreOriginalStats(TempBuffRestoreData restoreData)
    {
        if (restoreData == null || restoreData.character == null) return;

        restoreData.character.Attack = Mathf.RoundToInt(restoreData.originalAttack);
        restoreData.character.Magic = Mathf.RoundToInt(restoreData.originalMagic);
        restoreData.character.Defense = Mathf.RoundToInt(restoreData.originalDefense);
        restoreData.character.MagicDefense = Mathf.RoundToInt(restoreData.originalMagicDefense);
        restoreData.character.Speed = Mathf.RoundToInt(restoreData.originalSpeed);
    }

    /// <summary>
    /// スキル由来のバフ・デバフを反映
    /// </summary>
    private void ApplyBuffList(D_Ch_StatusData character, List<ActiveBuff> activeBuffs)
    {
        if (activeBuffs == null || activeBuffs.Count == 0) return;

        foreach (var buff in activeBuffs)
        {
            if (buff.baseData == null) continue;

            var kind = buff.baseData.SeeKinds;
            var buffKind = buff.baseData.SeeBuff_DeBuff_Kinds;
            float power = buff.baseData.Efficacy1; // ← スキル側にバフ倍率 or 加算値を設定しておく

            switch (kind)
            {
                case D_Sk_StatusData.Kinds.Buff:
                    ApplyBuffEffect(character, buffKind, power, true);
                    break;
                case D_Sk_StatusData.Kinds.DeBuff:
                    ApplyBuffEffect(character, buffKind, power, false);
                    break;
            }
        }
    }

    /// <summary>
    /// アイテム由来のバフ・デバフを反映
    /// </summary>
    private void ApplyBuffList_It(D_Ch_StatusData character, List<ActiveBuff_It> activeBuffs)
    {
        if (activeBuffs == null || activeBuffs.Count == 0) return;

        foreach (var buff in activeBuffs)
        {
            if (buff.baseData == null) continue;

            var kind = buff.baseData.SeeKinds;
            var buffKind = buff.baseData.SeeBuff_DeBuff_Kinds;
            float power = buff.baseData.Efficacy1; // ← アイテム側の効果量

            switch (kind)
            {
                case D_It_StatusData.Kinds.Buff:
                    ApplyBuffEffect(character, buffKind, power, true);
                    break;
                case D_It_StatusData.Kinds.DeBuff:
                    ApplyBuffEffect(character, buffKind, power, false);
                    break;
            }
        }
    }

    /// <summary>
    /// 個々の能力値に対してバフまたはデバフを反映
    /// </summary>
    private void ApplyBuffEffect(D_Ch_StatusData character, Enum buffKind, float value, bool isBuff)
    {
        // valueをパーセンテージ（例：20 → 0.2f）に変換
        float multiplier = isBuff ? 1f + (value * 0.01f) : 1f - (value * 0.01f);

        switch (buffKind)//Dictionaryに変更するかも
        {
            case D_Sk_StatusData.Buff_DeBuff_Kinds.Attack:
                character.Attack = Mathf.RoundToInt(character.Attack * multiplier);
                break;
            case D_Sk_StatusData.Buff_DeBuff_Kinds.Magic:
                character.Magic = Mathf.RoundToInt(character.Magic * multiplier);
                break;
            case D_Sk_StatusData.Buff_DeBuff_Kinds.Defense:
                character.Defense = Mathf.RoundToInt(character.Defense * multiplier);
                break;
            case D_Sk_StatusData.Buff_DeBuff_Kinds.MagicDefense:
                character.MagicDefense = Mathf.RoundToInt(character.MagicDefense * multiplier);
                break;
            case D_Sk_StatusData.Buff_DeBuff_Kinds.Speed:
                character.Speed = Mathf.RoundToInt(character.Speed * multiplier);
                break;
            case D_Sk_StatusData.Buff_DeBuff_Kinds.Critical:
                character.CriticalRate = Mathf.RoundToInt(character.CriticalRate * multiplier);
                break;
        }
        Debug.Log($"{D_Sk_StatusData.Buff_DeBuff_Kinds.Attack}に{multiplier}を付与しました。");
    }

    /// <summary>
    /// UIを動かしているTimelineIconControllerの現在の素早さ計算よう
    /// </summary>
    public float GetEffectiveSpeed(D_Ch_StatusData characterData)//素早さバフ計算
    {
        float baseSpeed = characterData.Speed;
        float speedBuffRate = 0f; // 上昇率（例：+0.2f = +20%）
        float speedDebuffRate = 0f; // 減少率（例：-0.1f = -10%）

        // スキルバフをチェック
        foreach (var activeBuff in characterData.ActiveBuffs)
        {
            var buff = activeBuff.baseData;

            if ((buff.SeeKinds == D_Sk_StatusData.Kinds.Buff || buff.SeeKinds == D_Sk_StatusData.Kinds.DeBuff)
           && buff.SeeBuff_DeBuff_Kinds == D_Sk_StatusData.Buff_DeBuff_Kinds.Speed)
            {
                // 例：buff.Efficacy1 が 20 なら 20%アップとみなす
                float value = buff.Efficacy1 * 0.01f;
                if (buff.SeeKinds == D_Sk_StatusData.Kinds.Buff)
                    speedBuffRate += value;
                else
                    speedDebuffRate += value;
            }
        }

        // アイテムバフもチェック
        foreach (var activeBuff in characterData.ActiveBuffs_It)
        {
            var itemBuff = activeBuff.baseData;

            if ((itemBuff.SeeKinds == D_It_StatusData.Kinds.Buff || itemBuff.SeeKinds == D_It_StatusData.Kinds.DeBuff)
                && itemBuff.SeeBuff_DeBuff_Kinds == D_It_StatusData.Buff_DeBuff_Kinds.Speed)
            {
                float value = itemBuff.Efficacy1 * 0.01f;
                if (itemBuff.SeeKinds == D_It_StatusData.Kinds.Buff)
                    speedBuffRate += value;
                else
                    speedDebuffRate += value;
            }
        }

        // 最終計算
        float finalSpeed = baseSpeed * (1f + speedBuffRate - speedDebuffRate);

        // 速度がマイナスにならないように下限保証
        return Mathf.Max(0.1f, finalSpeed);
    }

    /// <summary>
    /// 行動結果の処理
    /// </summary>
    public void DamageCalculation() // ダメージ計算
    {
        // ▼ 攻撃者と防御者の元ステータスを保存（バフ・デバフ反映後の一時上書き用）
        TempBuffRestoreData atkBuffData = null;
        TempBuffRestoreData defBuffData = null;

        // ★ 同一キャラが対象の場合
        if (Use_ChData == Use_subject_ChData)
        {
            // → 自分にだけ適用する
            defBuffData = ApplyCurrentBuffDeBuffEffects(Use_subject_ChData);
        }
        else
        {
            // 通常処理（別キャラなので両方適用）
            atkBuffData = ApplyCurrentBuffDeBuffEffects(Use_ChData);
            defBuffData = ApplyCurrentBuffDeBuffEffects(Use_subject_ChData);
        }

        D_Ch_StatusData attacker = Use_ChData;
        D_Ch_StatusData defender = Use_subject_ChData;
        D_Sk_StatusData skill = SkillUse_SkData;

        // ▼▼▼ 種類による処理分岐 ▼▼▼
        switch (skill.SeeKinds)
        {
            case D_Sk_StatusData.Kinds.Buff:
            case D_Sk_StatusData.Kinds.DeBuff:
            case D_Sk_StatusData.Kinds.Defense:
            case D_Sk_StatusData.Kinds.Abilities:
                // ActiveBuff を作成して付与 → その前に同一バフがあるか確認
                var existingBuff = defender.ActiveBuffs.Find(b => b.baseData == skill);

                if (existingBuff != null)
                {
                    // 既に同じバフがある → ターンだけ更新
                    existingBuff.remainingTurns = skill.Duration;

                    Debug.Log($"【Buff更新】{defender.name} の {skill.name} のターンを再設定 → {existingBuff.remainingTurns}ターン");
                }
                else
                {
                    // 新しく追加
                    var newBuff = new ActiveBuff(skill);
                    defender.ActiveBuffs.Add(newBuff);

                    Debug.Log($"【Buff追加】 {attacker.name} → {defender.name} / {skill.SeeBuff_DeBuff_Kinds}");
                    Debug.Log($"→ ActiveBuffs に {skill.name} を追加（残り {newBuff.remainingTurns} ターン）");
                }

                // ▼ MP消費
                attacker.Mp -= skill.MpConsumption;
                Debug.Log($"{skill.MpConsumption}MPを消費しました。");

                // ステータス更新処理
                if (attacker == defender)
                {
                    RestoreOriginalStats(defBuffData);
                }
                else
                {
                    RestoreOriginalStats(atkBuffData);
                    RestoreOriginalStats(defBuffData);
                }

                // ▼ エフェクトを再生
                StartCoroutine(PlayTheEffect_Skill(defender, skill));
                // ▼ 効果音を再生
                if (GameManager.Instance.audioSource != null && skill.SoundEffects != null)
                {
                    GameManager.Instance.audioSource.PlayOneShot(skill.SoundEffects); // 決定音
                }

                // ▼ UI更新（ステータスアイコンをすべて更新）
                UpdateAllStatusIcons();


                return;

            case D_Sk_StatusData.Kinds.Recovery:
                //回復
                var RecoveryAmount = Mathf.RoundToInt(skill.Efficacy1 * attacker.Magic);
                defender.Hp += RecoveryAmount;

                // ▼ エフェクトを再生
                StartCoroutine(PlayTheEffect_Skill(defender, skill));
                // ▼ 効果音を再生
                if (GameManager.Instance.audioSource != null && skill.SoundEffects != null)
                {
                    GameManager.Instance.audioSource.PlayOneShot(skill.SoundEffects); // 決定音
                }

                DamageText(defender, Mathf.RoundToInt(RecoveryAmount), false);
                return;
            case D_Sk_StatusData.Kinds.Quick:
                //疲労のデバフをattackerに付与してダメージ計算へ
                var FatigueDebuff = attacker.ActiveBuffs.Find(buff => buff.baseData != null && buff.baseData.name == "Fatigue");

                var FatigueData = ds_Sk_StatusDataStore.FindWithName("Fatigue");

                //疲労デバフがあればターン更新なければ付与
                if (FatigueDebuff != null)
                {
                    // 既に同じバフがある → ターンだけ更新
                    FatigueDebuff.remainingTurns = FatigueData.Duration;
                    Debug.LogWarning("疲労デバフを更新");
                }
                else
                {
                    // 新しく追加
                    var newBuff = new ActiveBuff(FatigueData);
                    attacker.ActiveBuffs.Add(newBuff);
                    Debug.LogWarning("疲労デバフを付与");
                }
                break;
            case D_Sk_StatusData.Kinds.Attack:
            case D_Sk_StatusData.Kinds.Fast:
            case D_Sk_StatusData.Kinds.slow:
                // ↓↓↓ このままダメージ計算へ ↓↓↓
                break;

            default:
                Debug.LogWarning("未対応のスキル種類");
                if (attacker == defender)
                {
                    RestoreOriginalStats(defBuffData);
                }
                else
                {
                    RestoreOriginalStats(atkBuffData);
                    RestoreOriginalStats(defBuffData);
                }
                return;
        }


        // ▼▼▼ 攻撃ダメージ計算 ▼▼▼

        // ▼ 物理か魔法かで使うステータスを切り替え
        int attackStat = (skill.SeeAttack_or_Magic == D_Sk_StatusData.Attack_or_Magic.Attack)
            ? attacker.Attack
            : attacker.Magic;

        int defenseStat = (skill.SeeAttack_or_Magic == D_Sk_StatusData.Attack_or_Magic.Attack)
            ? defender.Defense
            : defender.MagicDefense;

        // ▼ 基本ダメージ計算（スキル倍率 × 攻撃力 − 防御値）
        int baseDamage = Mathf.RoundToInt(skill.Efficacy1 * attackStat) - defenseStat;
        baseDamage = Mathf.Max(0, baseDamage); // 0未満は切り捨て

        // ▼ 属性補正（弱点 1.5倍、耐性 0.5倍）
        float attributeMultiplier = GetAttributeMultiplier(skill.SeeAttribute, defender.SeeAttribute);
        baseDamage = Mathf.RoundToInt(baseDamage * attributeMultiplier);

        // ▼ クリティカル判定（攻撃者のクリ率 + スキルのクリ率）
        float totalCriticalRate =
            Mathf.Clamp01(attacker.CriticalRate + skill.CriticalRate);

        bool isCritical = UnityEngine.Random.value < totalCriticalRate;

        if (isCritical)
            baseDamage = Mathf.RoundToInt(baseDamage * 2f);

        // ▼ 乱数幅（80% ～ 120%）
        int minDamage = Mathf.FloorToInt(baseDamage * 0.8f);
        int maxDamage = Mathf.CeilToInt(baseDamage * 1.2f);
        int finalDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        // ▼ 防御していればダメージ半減
        bool hasDefense = defender.ActiveBuffs.Any(buff => buff.baseData != null && buff.baseData.name == "Defense");

        if (hasDefense)
        {
            finalDamage /= 2;
            Debug.Log("Defenseバフによりダメージ半減");
        }

        // ▼ エフェクトを再生
        StartCoroutine(PlayTheEffect_Skill(defender, skill));

        // ▼ 効果音を再生
        if (GameManager.Instance.audioSource != null && skill.SoundEffects != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(skill.SoundEffects); // 決定音
        }

        // ▼ ダメージ適用
        ApplyDamage(defender, finalDamage, isCritical);

        // ▼ MP消費
        attacker.Mp -= skill.MpConsumption;
        Debug.Log($"{skill.MpConsumption}MPを消費しました。");

        Debug.Log(
            $"攻撃者:{attacker.name}, 対象:{defender.name}, スキル:{skill.name}\n" +
            $"属性補正:{attributeMultiplier}, クリティカル:{isCritical}\n" +
            $"最終ダメージ:{finalDamage}"
        );

        // ▼ 元のステータスに戻す
        if (attacker == defender)
        {
            RestoreOriginalStats(defBuffData);
        }
        else
        {
            RestoreOriginalStats(atkBuffData);
            RestoreOriginalStats(defBuffData);
        }

        // ▼ UI更新（ステータスアイコンをすべて更新）
        UpdateAllStatusIcons();
    }

    private float GetAttributeMultiplier(
    D_Sk_StatusData.Attribute attackAttr,
    D_Ch_StatusData.Attribute defendAttr)//属性計算
    {
        // 無属性：弱点・耐性なし
        if (attackAttr == D_Sk_StatusData.Attribute.None ||
            defendAttr == D_Ch_StatusData.Attribute.None)
            return 1f;

        // 神秘 → 全属性に弱点 (呪いのみ耐性なし)
        if (attackAttr == D_Sk_StatusData.Attribute.Mystery)
        {
            if (defendAttr == D_Ch_StatusData.Attribute.Curse) return 1f; // 等倍
            return 1.5f; // それ以外は弱点
        }

        // 呪い → 神秘にのみ弱点、その他全て等倍
        if (attackAttr == D_Sk_StatusData.Attribute.Curse)
        {
            if (defendAttr == D_Ch_StatusData.Attribute.Mystery) return 1.5f;
            return 1f;
        }

        // --- 五行相克（火 → 金 → 木 → 土 → 水 → 火）---
        bool isWeak =
            (attackAttr == D_Sk_StatusData.Attribute.Fire && defendAttr == D_Ch_StatusData.Attribute.Metal) ||
            (attackAttr == D_Sk_StatusData.Attribute.Metal && defendAttr == D_Ch_StatusData.Attribute.Tree) ||
            (attackAttr == D_Sk_StatusData.Attribute.Tree && defendAttr == D_Ch_StatusData.Attribute.Soil) ||
            (attackAttr == D_Sk_StatusData.Attribute.Soil && defendAttr == D_Ch_StatusData.Attribute.Water) ||
            (attackAttr == D_Sk_StatusData.Attribute.Water && defendAttr == D_Ch_StatusData.Attribute.Fire);

        bool isResist =
            (defendAttr == D_Ch_StatusData.Attribute.Fire && attackAttr == D_Sk_StatusData.Attribute.Metal) ||
            (defendAttr == D_Ch_StatusData.Attribute.Metal && attackAttr == D_Sk_StatusData.Attribute.Tree) ||
            (defendAttr == D_Ch_StatusData.Attribute.Tree && attackAttr == D_Sk_StatusData.Attribute.Soil) ||
            (defendAttr == D_Ch_StatusData.Attribute.Soil && attackAttr == D_Sk_StatusData.Attribute.Water) ||
            (defendAttr == D_Ch_StatusData.Attribute.Water && attackAttr == D_Sk_StatusData.Attribute.Fire);

        if (isWeak) return 1.5f;
        if (isResist) return 0.5f;

        return 1f;
    }

    /// <summary>
    /// アイテム用の属性補正計算（アイテムの属性とキャラクターの属性）
    /// </summary>
    private float GetAttributeMultiplierForItem(
        D_It_StatusData.Attribute attackAttr,
        D_Ch_StatusData.Attribute defendAttr)
    {
        // enum値は同じなので、スキル用のメソッドを呼び出す
        // キャストして使用
        return GetAttributeMultiplier(
            (D_Sk_StatusData.Attribute)attackAttr,
            defendAttr
        );
    }

    /// <summary>
    /// DamageCalculation() の非同期版。エフェクト再生完了まで待つ。
    /// </summary>
    public IEnumerator DamageCalculationAsync(D_Sk_StatusData targetSkills)
    {
        DamageCalculation();

        float waitTime = 1f; // デフォルト待機時間（エフェクトがない場合）

        // ▼ アイコン停止
        BattleManager.Instance.Stopallicons();

        GameObject effectInstance = null;

        // ▼ エフェクトが設定されている場合
        if (targetSkills != null && targetSkills.Effect != null)
        {
            effectInstance = Instantiate(targetSkills.Effect);
            // ▼ ParticleSystemがある場合は再生時間取得
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                waitTime = ps.main.duration;
            }
        }

        // エフェクト再生時間（+余裕）待つ
        yield return new WaitForSeconds(waitTime + 0.5f);
    }

    private void ApplyDamage(D_Ch_StatusData target, int damage, bool isCritical = false)//ダメージ処理
    {
        target.Hp -= damage;
        target.Hp = Mathf.Max(0, target.Hp);

        Debug.Log($"{damage}ダメージを与えました");

        BattleManager battleManager = BattleManager.Instance;

        // HPが0になった場合、キャラクターとアイコンを削除
        if (target.Hp <= 0)
        {
            if (battleManager != null)
            {
                // 点滅してから削除
                Sequence blinkSeq = BlinkHitObject(target);

                if (blinkSeq != null)
                {
                    blinkSeq.OnComplete(() =>
                    {
                        battleManager.RemoveCharacterOnDeath(target);
                        DamageText(target, damage, true, isCritical);
                    });

                    //メモ　blinkSeq.OnCompleteはこのアニメーション（Sequence）が全部終わったら、この中の処理を実行してくださいという意味
                }
                else
                {
                    // フェイルセーフ
                    battleManager.RemoveCharacterOnDeath(target);
                    DamageText(target, damage, true, isCritical);
                }
            }
            else
            {
                Debug.LogWarning("BattleManager.Instance が見つかりません。削除処理をスキップします。");
            }
        }
        else
        {
            // ダメージを受けたキャラクターのオブジェクトを点滅させる
            BlinkHitObject(target);
            DamageText(target, damage, true, isCritical);
            // ダメージを受けたキャラクターのTimelineIconControllerを探して、currentProgressを下げる
            ReduceTimelineProgress(target);
        }

        // ここで勝敗判定を呼ぶ
        if (Result.Instance != null)
        {
            Result.Instance.Victory_LossJudgment();
        }
        else
        {
            Debug.LogWarning("Result.Instance が見つかりません");
        }
    }

    /// <summary>
    /// 被ダメージキャラの表示用ルートを取得。
    /// </summary>
    private bool TryGetDamagedCharacterRoot(D_Ch_StatusData data, out GameObject root)
    {
        root = null;
        if (data == null) return false;

        BattleManager bm = BattleManager.Instance;
        if (bm != null)
        {
            foreach (var icon in bm.allyIcons)
            {
                if (icon != null && icon.characterData == data && icon.characterObject != null)
                {
                    root = icon.characterObject;
                    return true;
                }
            }
            foreach (var icon in bm.enemyIcons)
            {
                if (icon != null && icon.characterData == data && icon.characterObject != null)
                {
                    root = icon.characterObject;
                    return true;
                }
            }
        }

        if (Use_subject_ChData == data && Use_characterObject != null)
        {
            root = Use_characterObject;
            return true;
        }

        return false;
    }

    private void KillHitBlinkOnRoot(GameObject root)
    {
        if (root == null) return;
        int id = root.GetInstanceID();
        if (_hitBlinkSequences.TryGetValue(id, out Sequence seq) && seq != null && seq.IsActive())
            seq.Kill();
        _hitBlinkSequences.Remove(id);
    }

    /// <summary>
    /// ダメージを受けたキャラクターオブジェクトにダメージ数を表示。
    /// ダメージか回復かで色を変える、クリティカル時黄色
    /// true: ダメージ, false: 回復
    /// </summary>
    private void DamageText(D_Ch_StatusData targetCheck, int damage, bool Damage_or_healing, bool isCritical = false)
    {
        if (targetCheck == null) return;
        if (!TryGetDamagedCharacterRoot(targetCheck, out GameObject root) || root == null) return;

        // Status display を取得
        Transform StatusDisplay = root.transform.Find("Status display");
        if (StatusDisplay == null) return;

        Transform damageTextTransform = StatusDisplay.Find("DamageText");
        if (damageTextTransform == null) return;

        TMP_Text tmp = damageTextTransform.GetComponent<TMP_Text>();
        if (tmp == null) return;

        // テキスト設定
        tmp.text = damage.ToString();

        // 色設定
        if (isCritical)
        {
            tmp.color = Color.yellow;//黄
        }
        else if (Damage_or_healing)
        {
            tmp.color = Color.red;//赤
        }
        else
        {
            tmp.color = new Color(0.5f, 1f, 0f); // 黄緑
        }

        // 表示ON
        StatusDisplay.gameObject.SetActive(true);

        // スケール初期化
        damageTextTransform.localScale = Vector3.zero;

        // 既存Tween停止（連続被弾対策）
        damageTextTransform.DOKill();



        // アニメーション
        Sequence seq = DOTween.Sequence();

        seq.Append(damageTextTransform.DOScale(1.4f, 0.2f).SetEase(Ease.OutBack)) // ぽん
           .Append(damageTextTransform.DOScale(1f, 0.15f))                         // 戻る
           .AppendInterval(0.65f)                                                   // 合計約1秒
           .OnComplete(() =>
           {
               StatusDisplay.gameObject.SetActive(false);
           });
    }

    /// <summary>
    /// ダメージを受けたキャラクターオブジェクトを点滅させる（DOTween）。
    /// エフェクトと同期する場合は <paramref name="durationSeconds"/> に再生時間を渡す。
    /// </summary>
    private Sequence BlinkHitObject(D_Ch_StatusData targetCheck, float durationSeconds = 1f)
    {
        if (targetCheck == null) return null;
        if (!TryGetDamagedCharacterRoot(targetCheck, out GameObject root) || root == null) return null;


        KillHitBlinkOnRoot(root);

        durationSeconds = Mathf.Max(0.05f, durationSeconds);

        const int pulseCount = 3; // 点滅回数
        float onePulseDuration = durationSeconds / pulseCount;
        float half = onePulseDuration / 2f;

        // SpriteRendererを取得
        SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return null;
        // マテリアルを取得
        Material material = spriteRenderer.material;
        if (material == null) return null;

        // URPでは_BaseColorを使用する可能性があるため、両方試す
        string colorPropertyName = "_BaseColor";
        if (!material.HasProperty(colorPropertyName))
        {
            colorPropertyName = "_Color";
        }

        // デフォルトの白色を設定
        Color defaultColor = Color.white;
        Debug.Log($"デフォルト色: {defaultColor}");

        // 既存のTweenを完全に停止して色を白に戻す
        material.DOKill(true); // trueを指定して完全に停止
        material.SetColor(colorPropertyName, defaultColor);

        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < pulseCount; i++)
        {
            seq.Append(material.DOColor(Color.red, colorPropertyName, half));
            seq.Append(material.DOColor(defaultColor, colorPropertyName, half));
        }

        seq.OnComplete(() => material.SetColor(colorPropertyName, defaultColor));

        return seq;
    }

    /// <summary>
    /// エフェクトを1ループ再生する
    /// エフェクトを再生中タイムライン上の全アイコンを停止
    /// 再生が終わるまでまつ
    /// </summary>
    private IEnumerator PlayTheEffect_Skill(D_Ch_StatusData targetCheck, D_Sk_StatusData targetSkills)
    {
        if (targetCheck == null) yield break;
        if (!TryGetDamagedCharacterRoot(targetCheck, out GameObject root) || root == null) yield break;

        Debug.Log($"エフェクトを再生します。");

        float waitTime = 1f; // デフォルト待機時間（エフェクトがない場合）

        // ▼ アイコン停止
        BattleManager.Instance.Stopallicons();

        GameObject effectInstance = null;

        // ▼ エフェクトが設定されている場合
        if (targetSkills != null && targetSkills.Effect != null)
        {
            effectInstance = Instantiate(targetSkills.Effect, root.transform);
            effectInstance.transform.localPosition = Vector3.zero;

            // ▼ ParticleSystemがある場合は再生時間取得
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                waitTime = ps.main.duration;
            }
        }

        Debug.Log($"{waitTime}秒程エフェクトを再生します。");

        // ▼ 再生終了まで待つ
        yield return new WaitForSeconds(waitTime);

        // ▼ エフェクト削除
        if (effectInstance != null)
        {
            Destroy(effectInstance);
        }

        // ▼ アイコン再開
        BattleManager.Instance.Moveallicons();

        // ▼ 行動名リセット
        BattleManager.Instance.ActionName.text = "";
    }

    private IEnumerator PlayTheEffect_Item(D_Ch_StatusData targetCheck, D_It_StatusData targetItems)
    {
        if (targetCheck == null) yield break;
        if (!TryGetDamagedCharacterRoot(targetCheck, out GameObject root) || root == null) yield break;

        Debug.Log($"エフェクトを再生します。");

        float waitTime = 1f; // デフォルト待機時間（エフェクトがない場合）

        // ▼ アイコン停止
        BattleManager.Instance.Stopallicons();

        GameObject effectInstance = null;

        // ▼ エフェクトが設定されている場合
        if (targetItems != null && targetItems.Effect != null)
        {
            effectInstance = Instantiate(targetItems.Effect, root.transform);
            effectInstance.transform.localPosition = Vector3.zero;

            // ▼ ParticleSystemがある場合は再生時間取得
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                waitTime = ps.main.duration;
            }
        }

        Debug.Log($"{waitTime}秒程エフェクトを再生します。");

        // ▼ 再生終了まで待つ
        yield return new WaitForSeconds(waitTime);

        // ▼ エフェクト削除
        if (effectInstance != null)
        {
            Destroy(effectInstance);
        }

        // ▼ アイコン再開
        BattleManager.Instance.Moveallicons();

        // ▼ 行動名リセット
        BattleManager.Instance.ActionName.text = "";
    }



    /// <summary>エフェクト再生時間に合わせて点滅時間だけ変えたいときに呼ぶ。</summary>
    public void BlinkHitObjectForDuration(D_Ch_StatusData targetCheck, float durationSeconds)
    {
        BlinkHitObject(targetCheck, durationSeconds);
    }

    /// <summary>ダメージを受けたキャラクターのTimelineIconControllerのcurrentProgressを下げる </summary>
    private void ReduceTimelineProgress(D_Ch_StatusData targetCharacter)
    {
        if (targetCharacter == null) return;

        // BattleManagerからTimelineIconControllerを取得
        BattleManager battleManager = BattleManager.Instance;
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager.Instance が見つかりません");
            return;
        }

        // allyIconsとenemyIconsから対象のTimelineIconControllerを探す
        TimelineIconController targetIcon = null;
        
        foreach (var icon in battleManager.allyIcons)
        {
            if (icon != null && icon.characterData == targetCharacter)
            {
                targetIcon = icon;
                break;
            }
        }

        if (targetIcon == null)
        {
            foreach (var icon in battleManager.enemyIcons)
            {
                if (icon != null && icon.characterData == targetCharacter)
                {
                    targetIcon = icon;
                    break;
                }
            }
        }

        if (targetIcon == null)
        {
            Debug.LogWarning($"TimelineIconController が見つかりません: {targetCharacter.name}");
            return;
        }

        //行動リセット
        targetIcon.ActionReset();

        // stateがActing_upならcurrentProgressを3下げる（滑らかに）
        if (targetIcon.state == TimelineIconController.TimelineState.Acting_up || targetIcon.state == TimelineIconController.TimelineState.Interrupted)
        {
            float targetProgress = Mathf.Max(0f, targetIcon.currentProgress - 0.3f);
            
            // DOTweenで滑らかに下げる
            DOTween.To(
                () => targetIcon.currentProgress,
                x => targetIcon.currentProgress = x,
                targetProgress,
                0.3f // 0.3秒かけて滑らかに下げる
            ).SetEase(Ease.OutQuad) // イージングで滑らかに
            .OnComplete(() =>
            {
                // アニメーション完了後、行動ゾーンから出たことを伝える処理
                // isActionTriggeredはGaugeRoutine()で自動的にリセットされる
                if (targetIcon.state == TimelineIconController.TimelineState.Acting_up)
                {
                    targetIcon.state = TimelineIconController.TimelineState.Moving;
                }
                else
                {
                    targetIcon.state = TimelineIconController.TimelineState.WaitingForCommand;
                }
            });

            Debug.Log($"{targetCharacter.name} の currentProgress を {targetIcon.currentProgress} → {targetProgress} に下げました");
        }
    }

    /// <summary>
    /// StatusIcons の子にある CharacterIconStatus を全て更新
    /// </summary>
    private void UpdateAllStatusIcons()
    {
        if (BattleManager.Instance == null)
        {
            Debug.Log("BattleManager がシーンに存在しません！");
            return;
        }

        Transform statusIcons = BattleManager.Instance.StatusIcons?.transform;

        if (statusIcons == null)
        {
            Debug.Log("StatusIcons が BattleManager に設定されていません！");
            return;
        }

        // 子オブジェクトすべての CharacterIconStatus を更新
        foreach (Transform child in statusIcons)
        {
            var icon = child.GetComponent<CharacterIconStatus>();
            if (icon != null)
            {
                icon.StatusUpdateBattle();
            }
        }
    }


    /// <summary>
    /// アイテムの種類に応じて処理を実行する
    /// switch文の代わりにDictionaryを使用
    /// </summary>
    public void Itemtypedetermination()//アイテムの種類に合わせて処理を変える
    {
        if (ItemUse_ItData == null) return;

        // Dictionaryから対応する処理を取得
        if (itemActions.TryGetValue(ItemUse_ItData.SeeKinds, out var action))
        {
            // 対応する処理が見つかった場合は実行
            action?.Invoke();
        }
        else
        {
            // Dictionaryに登録されていない種類の場合
            Debug.Log("未対応の Kinds: " + ItemUse_ItData.SeeKinds);
        }
    }

    private void WeaponEquipment()//武器装備
    {
        if (ItemUse_ItData == null)
        {
            Debug.LogWarning("データが見つかりません。");
            return;
        }

        //装備を外す
        if (Use_subject_ChData.Weapon != null && Use_subject_ChData.Weapon.Name == ItemUse_ItData.Name)
        {
            Use_subject_ChData.Weapon = null;
            Use_subject_ChData.MaxHp -= ItemUse_ItData.Equipment.MaxHpup;
            Use_subject_ChData.Hp -= ItemUse_ItData.Equipment.MaxHpup;
            Use_subject_ChData.MaxMp -= ItemUse_ItData.Equipment.MaxMpup;
            Use_subject_ChData.Mp -= ItemUse_ItData.Equipment.MaxMpup;
            Use_subject_ChData.Attack -= ItemUse_ItData.Equipment.Attackup;
            Use_subject_ChData.Magic -= ItemUse_ItData.Equipment.Magicup;
            Use_subject_ChData.Defense -= ItemUse_ItData.Equipment.Defenceup;
            Use_subject_ChData.MagicDefense -= ItemUse_ItData.Equipment.MagicDefenseup;
            Use_subject_ChData.Speed -= ItemUse_ItData.Equipment.Speedup;
            Use_subject_ChData.CriticalRate -= ItemUse_ItData.Equipment.Criticalup;

            if (Use_subject_ChData.Hp <= 0)
            {
                Use_subject_ChData.Hp = 1;
            }

            Debug.Log($"{ItemUse_ItData.Name} を外しました。");
        }
        else
        {
            //装備を入れ替える
            if (Use_subject_ChData.Weapon != null)
            {
                //装備上書き
                Use_subject_ChData.MaxHp -= Use_subject_ChData.Weapon.Equipment.MaxHpup;
                Use_subject_ChData.Hp -= Use_subject_ChData.Weapon.Equipment.MaxHpup;
                Use_subject_ChData.MaxMp -= Use_subject_ChData.Weapon.Equipment.MaxMpup;
                Use_subject_ChData.Mp -= Use_subject_ChData.Weapon.Equipment.MaxMpup;
                Use_subject_ChData.Attack -= Use_subject_ChData.Weapon.Equipment.Attackup;
                Use_subject_ChData.Magic -= Use_subject_ChData.Weapon.Equipment.Magicup;
                Use_subject_ChData.Defense -= Use_subject_ChData.Weapon.Equipment.Defenceup;
                Use_subject_ChData.MagicDefense -= Use_subject_ChData.Weapon.Equipment.MagicDefenseup;
                Use_subject_ChData.Speed -= Use_subject_ChData.Weapon.Equipment.Speedup;
                Use_subject_ChData.CriticalRate -= Use_subject_ChData.Weapon.Equipment.Criticalup;

                Use_subject_ChData.Weapon = ItemUse_ItData;
                ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

                Debug.Log($"{ItemUse_ItData.Name} に変更しました。");
            }
            else
            {
                //装備をする
                Use_subject_ChData.Weapon = ItemUse_ItData;
                ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

                Debug.Log($"{ItemUse_ItData.Name} を装備しました。");
            }
        }
    }
    private void ArmorEquipment()//防具装備
    {
        if (ItemUse_ItData == null)
        {
            Debug.LogWarning("データが見つかりません。");
            return;
        }

        //装備を外す
        if (Use_subject_ChData.Armor != null && Use_subject_ChData.Armor.Name == ItemUse_ItData.Name)
        {
            Use_subject_ChData.Armor = null;
            Use_subject_ChData.MaxHp -= ItemUse_ItData.Equipment.MaxHpup;
            Use_subject_ChData.Hp -= ItemUse_ItData.Equipment.MaxHpup;
            Use_subject_ChData.MaxMp -= ItemUse_ItData.Equipment.MaxMpup;
            Use_subject_ChData.Mp -= ItemUse_ItData.Equipment.MaxMpup;
            Use_subject_ChData.Attack -= ItemUse_ItData.Equipment.Attackup;
            Use_subject_ChData.Magic -= ItemUse_ItData.Equipment.Magicup;
            Use_subject_ChData.Defense -= ItemUse_ItData.Equipment.Defenceup;
            Use_subject_ChData.MagicDefense -= ItemUse_ItData.Equipment.MagicDefenseup;
            Use_subject_ChData.Speed -= ItemUse_ItData.Equipment.Speedup;
            Use_subject_ChData.CriticalRate -= ItemUse_ItData.Equipment.Criticalup;

            if (Use_subject_ChData.Hp <= 0)
            {
                Use_subject_ChData.Hp = 1;
            }

            Debug.Log($"{ItemUse_ItData.Name} を外しました。");
        }
        else
        {
            //装備を入れ替える
            if (Use_subject_ChData.Armor != null)
            {
                //装備上書き
                Use_subject_ChData.MaxHp -= Use_subject_ChData.Armor.Equipment.MaxHpup;
                Use_subject_ChData.Hp -= Use_subject_ChData.Armor.Equipment.MaxHpup;
                Use_subject_ChData.MaxMp -= Use_subject_ChData.Armor.Equipment.MaxMpup;
                Use_subject_ChData.Mp -= Use_subject_ChData.Armor.Equipment.MaxMpup;
                Use_subject_ChData.Attack -= Use_subject_ChData.Armor.Equipment.Attackup;
                Use_subject_ChData.Magic -= Use_subject_ChData.Armor.Equipment.Magicup;
                Use_subject_ChData.Defense -= Use_subject_ChData.Armor.Equipment.Defenceup;
                Use_subject_ChData.MagicDefense -= Use_subject_ChData.Armor.Equipment.MagicDefenseup;
                Use_subject_ChData.Speed -= Use_subject_ChData.Armor.Equipment.Speedup;
                Use_subject_ChData.CriticalRate -= Use_subject_ChData.Armor.Equipment.Criticalup;

                Use_subject_ChData.Armor = ItemUse_ItData;
                ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

                if (Use_subject_ChData.Hp <= 0)
                {
                    Use_subject_ChData.Hp = 1;
                }

                Debug.Log($"{ItemUse_ItData.Name} に変更しました。");
            }
            else
            {
                //装備をする
                Use_subject_ChData.Armor = ItemUse_ItData;
                ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

                if (Use_subject_ChData.Hp <= 0)
                {
                    Use_subject_ChData.Hp = 1;
                }

                Debug.Log($"{ItemUse_ItData.Name} を装備しました。");
            }
        }
    }
    private void AccessoriesEquipment()//アクセサリー装備(両方対応)
    {
        if (ItemUse_ItData == null)
        {
            Debug.LogWarning("データが見つかりません。");
            return;
        }

        // 前のメニューで選択されていたUIのSelectionSettingsを取得
        bool useSpecificSlot = false;
        bool useAccessories1 = false;
        
        if (InventManager != null)
        {
            GameObject previousSelectedElement = InventManager.GetPreviousMenuSelectedElement();
            if (previousSelectedElement != null)
            {
                SelectionSettings selectionSettings = previousSelectedElement.GetComponent<SelectionSettings>();
                if (selectionSettings != null)
                {
                    if (selectionSettings.choose == SelectionSettings.Choose.Accessories1Choice)
                    {
                        useSpecificSlot = true;
                        useAccessories1 = true;
                    }
                    else if (selectionSettings.choose == SelectionSettings.Choose.Accessories2Choice)
                    {
                        useSpecificSlot = true;
                        useAccessories1 = false;
                    }
                }
            }
        }

        // 装備解除判定（既に装備されているか）
        // まず Accessories1 をチェック
        if (Use_subject_ChData.Accessories1 != null && string.Equals(Use_subject_ChData.Accessories1.Name, ItemUse_ItData.Name, System.StringComparison.Ordinal))
        {
            // Accessories1 を外す
            var eq = Use_subject_ChData.Accessories1.Equipment;
            if (eq != null)
            {
                Use_subject_ChData.MaxHp -= eq.MaxHpup;
                Use_subject_ChData.Hp -= eq.MaxHpup;
                Use_subject_ChData.MaxMp -= eq.MaxMpup;
                Use_subject_ChData.Mp -= eq.MaxMpup;
                Use_subject_ChData.Attack -= eq.Attackup;
                Use_subject_ChData.Magic -= eq.Magicup;
                Use_subject_ChData.Defense -= eq.Defenceup;
                Use_subject_ChData.MagicDefense -= eq.MagicDefenseup;
                Use_subject_ChData.Speed -= eq.Speedup;
                Use_subject_ChData.CriticalRate -= eq.Criticalup;
            }
            Use_subject_ChData.Accessories1 = null;

            // 安全ガード
            if (Use_subject_ChData.MaxHp < 1) Use_subject_ChData.MaxHp = 1;
            if (Use_subject_ChData.Hp < 1) Use_subject_ChData.Hp = 1;

            Debug.Log($"{ItemUse_ItData.Name} を Accessories1 から外しました。");
            return;
        }

        // 次に Accessories2 をチェック
        if (Use_subject_ChData.Accessories2 != null && string.Equals(Use_subject_ChData.Accessories2.Name, ItemUse_ItData.Name, System.StringComparison.Ordinal))
        {
            var eq = Use_subject_ChData.Accessories2.Equipment;
            if (eq != null)
            {
                Use_subject_ChData.MaxHp -= eq.MaxHpup;
                Use_subject_ChData.Hp -= eq.MaxHpup;
                Use_subject_ChData.MaxMp -= eq.MaxMpup;
                Use_subject_ChData.Mp -= eq.MaxMpup;
                Use_subject_ChData.Attack -= eq.Attackup;
                Use_subject_ChData.Magic -= eq.Magicup;
                Use_subject_ChData.Defense -= eq.Defenceup;
                Use_subject_ChData.MagicDefense -= eq.MagicDefenseup;
                Use_subject_ChData.Speed -= eq.Speedup;
                Use_subject_ChData.CriticalRate -= eq.Criticalup;
            }
            Use_subject_ChData.Accessories2 = null;

            if (Use_subject_ChData.MaxHp < 1) Use_subject_ChData.MaxHp = 1;
            if (Use_subject_ChData.Hp < 1) Use_subject_ChData.Hp = 1;

            Debug.Log($"{ItemUse_ItData.Name} を Accessories2 から外しました。");
            return;
        }

        // 特定のスロットが指定されている場合
        if (useSpecificSlot)
        {
            if (useAccessories1)
            {
                // Accessories1に装備
                // 既に装備されている場合は外す
                if (Use_subject_ChData.Accessories1 != null && Use_subject_ChData.Accessories1.Equipment != null)
                {
                    var oldEq = Use_subject_ChData.Accessories1.Equipment;
                    Use_subject_ChData.MaxHp -= oldEq.MaxHpup;
                    Use_subject_ChData.Hp -= oldEq.MaxHpup;
                    Use_subject_ChData.MaxMp -= oldEq.MaxMpup;
                    Use_subject_ChData.Mp -= oldEq.MaxMpup;
                    Use_subject_ChData.Attack -= oldEq.Attackup;
                    Use_subject_ChData.Magic -= oldEq.Magicup;
                    Use_subject_ChData.Defense -= oldEq.Defenceup;
                    Use_subject_ChData.MagicDefense -= oldEq.MagicDefenseup;
                    Use_subject_ChData.Speed -= oldEq.Speedup;
                    Use_subject_ChData.CriticalRate -= oldEq.Criticalup;
                }

                Use_subject_ChData.Accessories1 = ItemUse_ItData;
                ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

                if (Use_subject_ChData.MaxHp < 1) Use_subject_ChData.MaxHp = 1;
                if (Use_subject_ChData.Hp < 1) Use_subject_ChData.Hp = 1;

                Debug.Log($"{ItemUse_ItData.Name} を Accessories1 に装備しました。");
                return;
            }
            else
            {
                // Accessories2に装備
                // 既に装備されている場合は外す
                if (Use_subject_ChData.Accessories2 != null && Use_subject_ChData.Accessories2.Equipment != null)
                {
                    var oldEq = Use_subject_ChData.Accessories2.Equipment;
                    Use_subject_ChData.MaxHp -= oldEq.MaxHpup;
                    Use_subject_ChData.Hp -= oldEq.MaxHpup;
                    Use_subject_ChData.MaxMp -= oldEq.MaxMpup;
                    Use_subject_ChData.Mp -= oldEq.MaxMpup;
                    Use_subject_ChData.Attack -= oldEq.Attackup;
                    Use_subject_ChData.Magic -= oldEq.Magicup;
                    Use_subject_ChData.Defense -= oldEq.Defenceup;
                    Use_subject_ChData.MagicDefense -= oldEq.MagicDefenseup;
                    Use_subject_ChData.Speed -= oldEq.Speedup;
                    Use_subject_ChData.CriticalRate -= oldEq.Criticalup;
                }

                Use_subject_ChData.Accessories2 = ItemUse_ItData;
                ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

                if (Use_subject_ChData.MaxHp < 1) Use_subject_ChData.MaxHp = 1;
                if (Use_subject_ChData.Hp < 1) Use_subject_ChData.Hp = 1;

                Debug.Log($"{ItemUse_ItData.Name} を Accessories2 に装備しました。");
                return;
            }
        }

        // 特定のスロットが指定されていない場合は現状の処理
        // 空きスロットがあればそこに格納
        if (Use_subject_ChData.Accessories1 == null)
        {
            Use_subject_ChData.Accessories1 = ItemUse_ItData;
            ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);
            Debug.Log($"{ItemUse_ItData.Name} を Accessories1 に装備しました。");
            return;
        }

        if (Use_subject_ChData.Accessories2 == null)
        {
            Use_subject_ChData.Accessories2 = ItemUse_ItData;
            ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);
            Debug.Log($"{ItemUse_ItData.Name} を Accessories2 に装備しました。");
            return;
        }

        // 両方埋まっている場合は既定で Accessories1 を上書き（必要ならUIで選ばせる）
        {
            // 旧 Accessories1 のステータスを引く
            if (Use_subject_ChData.Accessories1 != null && Use_subject_ChData.Accessories1.Equipment != null)
            {
                var oldEq = Use_subject_ChData.Accessories1.Equipment;
                Use_subject_ChData.MaxHp -= oldEq.MaxHpup;
                Use_subject_ChData.Hp -= oldEq.MaxHpup;
                Use_subject_ChData.MaxMp -= oldEq.MaxMpup;
                Use_subject_ChData.Mp -= oldEq.MaxMpup;
                Use_subject_ChData.Attack -= oldEq.Attackup;
                Use_subject_ChData.Magic -= oldEq.Magicup;
                Use_subject_ChData.Defense -= oldEq.Defenceup;
                Use_subject_ChData.MagicDefense -= oldEq.MagicDefenseup;
                Use_subject_ChData.Speed -= oldEq.Speedup;
                Use_subject_ChData.CriticalRate -= oldEq.Criticalup;
            }

            // 新しくセットして加算
            Use_subject_ChData.Accessories1 = ItemUse_ItData;
            ApplyEquipmentStats(Use_subject_ChData, ItemUse_ItData);

            if (Use_subject_ChData.MaxHp < 1) Use_subject_ChData.MaxHp = 1;
            if (Use_subject_ChData.Hp < 1) Use_subject_ChData.Hp = 1;

            Debug.Log($"{ItemUse_ItData.Name} を Accessories1 に上書きしました。");
        }
    }

    //装備のステータスを加算する共通処理
    private void ApplyEquipmentStats(D_Ch_StatusData targetCh, D_It_StatusData eq)
    {
        if (eq == null) return;

        targetCh.MaxHp += eq.Equipment.MaxHpup;
        targetCh.Hp += eq.Equipment.MaxHpup;
        targetCh.MaxMp += eq.Equipment.MaxMpup;
        targetCh.Mp += eq.Equipment.MaxMpup;
        targetCh.Attack += eq.Equipment.Attackup;
        targetCh.Magic += eq.Equipment.Magicup;
        targetCh.Defense += eq.Equipment.Defenceup;
        targetCh.MagicDefense += eq.Equipment.MagicDefenseup;
        targetCh.Speed += eq.Equipment.Speedup;
        targetCh.CriticalRate += eq.Equipment.Criticalup;

        // Hp下限などガード
        if (targetCh.MaxHp < 1) targetCh.MaxHp = 1;
        if (targetCh.Hp < 1) targetCh.Hp = 1;
        if (targetCh.MaxMp < 0) targetCh.MaxMp = 0;
        if (targetCh.Mp < 0) targetCh.Mp = 0;
    }
    private void It_Buff()
    {
        var existingBuff = Use_subject_ChData.ActiveBuffs.Find(b => b.baseData == ItemUse_ItData);

        if (existingBuff != null)
        {
            // 既に同じバフがある → ターンだけ更新
            existingBuff.remainingTurns = ItemUse_ItData.Duration;

            Debug.Log($"【Buff更新】{Use_subject_ChData.name} の {ItemUse_ItData.name} のターンを再設定 → {existingBuff.remainingTurns}ターン");
        }
        else
        {
            // 新しく追加
            var newBuff_It = new ActiveBuff_It(ItemUse_ItData);
            Use_subject_ChData.ActiveBuffs_It.Add(newBuff_It);

            Debug.Log($"【Buff追加】 {Use_ChData.name} → {Use_subject_ChData.name} / {ItemUse_ItData.SeeBuff_DeBuff_Kinds}");
            Debug.Log($"→ ActiveBuffs に {ItemUse_ItData.name} を追加（残り {newBuff_It.remainingTurns} ターン）");
        }

        // ▼ UI更新（ステータスアイコンをすべて更新 戦闘用）
        UpdateAllStatusIcons();
    }   
    private void It_DeBuff()
    {
        var existingBuff = Use_subject_ChData.ActiveBuffs.Find(b => b.baseData == ItemUse_ItData);

        if (existingBuff != null)
        {
            // 既に同じバフがある → ターンだけ更新
            existingBuff.remainingTurns = ItemUse_ItData.Duration;

            Debug.Log($"【Buff更新】{Use_subject_ChData.name} の {ItemUse_ItData.name} のターンを再設定 → {existingBuff.remainingTurns}ターン");
        }
        else
        {
            // 新しく追加
            var newBuff_It = new ActiveBuff_It(ItemUse_ItData);
            Use_subject_ChData.ActiveBuffs_It.Add(newBuff_It);

            Debug.Log($"【Buff追加】 {Use_ChData.name} → {Use_subject_ChData.name} / {ItemUse_ItData.SeeBuff_DeBuff_Kinds}");
            Debug.Log($"→ ActiveBuffs に {ItemUse_ItData.name} を追加（残り {newBuff_It.remainingTurns} ターン）");
        }

        // ▼ UI更新（ステータスアイコンをすべて更新 戦闘用）
        UpdateAllStatusIcons();
    }
    private void It_Attack() 
    {
        if (ItemUse_ItData == null || ItemUse_ItData.Number <= 0)
        {
            Debug.LogWarning("アイテムデータが見つからないか、個数が0です。");
            UpdateAllStatusIcons();
            return;
        }

        // ▼ 攻撃者と防御者の元ステータスを保存（バフ・デバフ反映後の一時上書き用）
        TempBuffRestoreData atkBuffData = null;
        TempBuffRestoreData defBuffData = null;

        // ★ 同一キャラが対象の場合
        if (Use_ChData == Use_subject_ChData)
        {
            // → 自分にだけ適用する
            defBuffData = ApplyCurrentBuffDeBuffEffects(Use_subject_ChData);
        }
        else
        {
            // 通常処理（別キャラなので両方適用）
            atkBuffData = ApplyCurrentBuffDeBuffEffects(Use_ChData);
            defBuffData = ApplyCurrentBuffDeBuffEffects(Use_subject_ChData);
        }

        D_Ch_StatusData attacker = Use_ChData;
        D_Ch_StatusData defender = Use_subject_ChData;
        D_It_StatusData item = ItemUse_ItData;

        // ▼▼▼ 物理攻撃ダメージ計算 ▼▼▼

        // ▼ 物理攻撃なのでAttackとDefenseを使用
        int attackStat = attacker.Attack;
        int defenseStat = defender.Defense;

        // ▼ 基本ダメージ計算（アイテム倍率 × 攻撃力 − 防御値）
        int baseDamage = Mathf.RoundToInt(item.Efficacy1 * attackStat) - defenseStat;
        baseDamage = Mathf.Max(0, baseDamage); // 0未満は切り捨て

        // ▼ 属性補正（弱点 1.5倍、耐性 0.5倍）
        // アイテムの属性をスキル属性として扱う（enum値は同じ）
        float attributeMultiplier = GetAttributeMultiplierForItem(item.SeeAttribute, defender.SeeAttribute);
        baseDamage = Mathf.RoundToInt(baseDamage * attributeMultiplier);

        // ▼ クリティカル判定（攻撃者のクリ率のみ、アイテムにはクリ率がない）
        float totalCriticalRate = Mathf.Clamp01(attacker.CriticalRate);
        bool isCritical = UnityEngine.Random.value < totalCriticalRate;

        if (isCritical)
            baseDamage = Mathf.RoundToInt(baseDamage * 2f);

        // ▼ 乱数幅（80% ～ 120%）
        int minDamage = Mathf.FloorToInt(baseDamage * 0.8f);
        int maxDamage = Mathf.CeilToInt(baseDamage * 1.2f);
        int finalDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        PlayTheEffect_Item(defender, item);

        // ▼ ダメージ適用
        ApplyDamage(defender, finalDamage, isCritical);

        // ▼ アイテム個数を減らす
        item.Number -= 1;
        if (item.Number <= 0)
        {
            db_PlayerItem.ItemList.RemoveAll(i => i.Number <= 0);
        }

        Debug.Log(
            $"攻撃者:{attacker.name}, 対象:{defender.name}, アイテム:{item.name}\n" +
            $"属性補正:{attributeMultiplier}, クリティカル:{isCritical}\n" +
            $"最終ダメージ:{finalDamage}, 残り個数:{item.Number}"
        );

        // ▼ 元のステータスに戻す
        if (Use_ChData == Use_subject_ChData)
        {
            RestoreOriginalStats(defBuffData);
        }
        else
        {
            RestoreOriginalStats(atkBuffData);
            RestoreOriginalStats(defBuffData);
        }

        // ▼ UI更新（ステータスアイコンをすべて更新 戦闘用）
        UpdateAllStatusIcons();
    }
    private void It_Magic()
    {
        if (ItemUse_ItData == null || ItemUse_ItData.Number <= 0)
        {
            Debug.LogWarning("アイテムデータが見つからないか、個数が0です。");
            UpdateAllStatusIcons();
            return;
        }

        // ▼ 攻撃者と防御者の元ステータスを保存（バフ・デバフ反映後の一時上書き用）
        TempBuffRestoreData atkBuffData = null;
        TempBuffRestoreData defBuffData = null;

        // ★ 同一キャラが対象の場合
        if (Use_ChData == Use_subject_ChData)
        {
            // → 自分にだけ適用する
            defBuffData = ApplyCurrentBuffDeBuffEffects(Use_subject_ChData);
        }
        else
        {
            // 通常処理（別キャラなので両方適用）
            atkBuffData = ApplyCurrentBuffDeBuffEffects(Use_ChData);
            defBuffData = ApplyCurrentBuffDeBuffEffects(Use_subject_ChData);
        }

        D_Ch_StatusData attacker = Use_ChData;
        D_Ch_StatusData defender = Use_subject_ChData;
        D_It_StatusData item = ItemUse_ItData;

        // ▼▼▼ 魔法攻撃ダメージ計算 ▼▼▼

        // ▼ 魔法攻撃なのでMagicとMagicDefenseを使用
        int attackStat = attacker.Magic;
        int defenseStat = defender.MagicDefense;

        // ▼ 基本ダメージ計算（アイテム倍率 × 魔法力 − 魔法防御値）
        int baseDamage = Mathf.RoundToInt(item.Efficacy1 * attackStat) - defenseStat;
        baseDamage = Mathf.Max(0, baseDamage); // 0未満は切り捨て

        // ▼ 属性補正（弱点 1.5倍、耐性 0.5倍）
        // アイテムの属性をスキル属性として扱う（enum値は同じ）
        float attributeMultiplier = GetAttributeMultiplierForItem(item.SeeAttribute, defender.SeeAttribute);
        baseDamage = Mathf.RoundToInt(baseDamage * attributeMultiplier);

        // ▼ クリティカル判定（攻撃者のクリ率のみ、アイテムにはクリ率がない）
        float totalCriticalRate = Mathf.Clamp01(attacker.CriticalRate);
        bool isCritical = UnityEngine.Random.value < totalCriticalRate;

        if (isCritical)
            baseDamage = Mathf.RoundToInt(baseDamage * 2f);

        // ▼ 乱数幅（80% ～ 120%）
        int minDamage = Mathf.FloorToInt(baseDamage * 0.8f);
        int maxDamage = Mathf.CeilToInt(baseDamage * 1.2f);
        int finalDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        PlayTheEffect_Item(defender, item);

        // ▼ ダメージ適用
        ApplyDamage(defender, finalDamage, isCritical);

        // ▼ アイテム個数を減らす
        item.Number -= 1;
        if (item.Number <= 0)
        {
            db_PlayerItem.ItemList.RemoveAll(i => i.Number <= 0);
        }

        Debug.Log(
            $"攻撃者:{attacker.name}, 対象:{defender.name}, アイテム:{item.name}\n" +
            $"属性補正:{attributeMultiplier}, クリティカル:{isCritical}\n" +
            $"最終ダメージ:{finalDamage}, 残り個数:{item.Number}"
        );

        // ▼ 元のステータスに戻す
        if (Use_ChData == Use_subject_ChData)
        {
            RestoreOriginalStats(defBuffData);
        }
        else
        {
            RestoreOriginalStats(atkBuffData);
            RestoreOriginalStats(defBuffData);
        }

        // ▼ UI更新（ステータスアイコンをすべて更新 戦闘用）
        UpdateAllStatusIcons();
    }
    private void It_HP_Recovery()
    {
        if(ItemUse_ItData.Number > 0)
        {
            Use_subject_ChData.Hp += Mathf.RoundToInt(ItemUse_ItData.Efficacy1);//Mathf.RoundToInt,floatをintに入れるやつ
            ItemUse_ItData.Number -= 1;

            DamageText(Use_subject_ChData, Mathf.RoundToInt(ItemUse_ItData.Efficacy1), false);

            if (ItemUse_ItData.Number <= 0)
            {
                db_PlayerItem.ItemList.RemoveAll(item => item.Number <= 0);
            }

            PlayTheEffect_Item(Use_subject_ChData, ItemUse_ItData);
        }
        // ▼ UI更新（ステータスアイコンをすべて更新 戦闘用）
        UpdateAllStatusIcons();
    }
    private void It_MP_Recovery()
    {
        if (ItemUse_ItData.Number > 0)
        {
            Use_subject_ChData.Mp += Mathf.RoundToInt(ItemUse_ItData.Efficacy1);//Mathf.RoundToInt,floatをintに入れるやつ
            ItemUse_ItData.Number -= 1;

            DamageText(Use_subject_ChData, Mathf.RoundToInt(ItemUse_ItData.Efficacy1), false);

            if (ItemUse_ItData.Number <= 0)
            {
                db_PlayerItem.ItemList.RemoveAll(item => item.Number <= 0);
            }

            PlayTheEffect_Item(Use_subject_ChData, ItemUse_ItData);
        }

        // ▼ UI更新（ステータスアイコンをすべて更新 戦闘用）
        UpdateAllStatusIcons();
    }

    /// <summary>
    /// 敵を倒した際などのランダムアイテムドロップ
    /// </summary>
    public bool Random_drop(D_It_StatusData dropItem, int rate)
    {
        if (dropItem == null) return false;
        if (db_PlayerItem == null) return false;

        // 0以下は絶対落ちない、100以上は確定ドロップ
        int clampedRate = Mathf.Clamp(rate, 0, 100);
        bool isDropped = UnityEngine.Random.Range(0, 100) < clampedRate;
        if (!isDropped) return false;

        // 既に所持リストに同一アイテムがあるなら個数加算
        D_It_StatusData existingItem = null;
        foreach (var owned in db_PlayerItem.ItemList)
        {
            if (owned == null) continue;
            if (owned == dropItem || owned.Id == dropItem.Id || owned.Name == dropItem.Name)
            {
                existingItem = owned;
                break;
            }
        }

        if (existingItem != null)
        {
            existingItem.Number += 1;
        }
        else
        {
            // 新規入手（ScriptableObjectアセット参照を追加）
            if (dropItem.Number <= 0)
            {
                dropItem.Number = 1;
            }
            else
            {
                dropItem.Number += 1;
            }
            db_PlayerItem.ItemList.Add(dropItem);
        }

        return true;
    }

    /// <summary>
    /// 獲得したEXPを実際にデータに入れる
    /// レベルアップ処理
    /// </summary>
    public void EXP_earnedprocess(D_Ch_StatusData subject, int acquisitionEXP)
    {
        //subject.ExpにacquisitionEXPを足していきsubject.Expがsubject.LevelExp[Level-1].NeedExp以上になったら
        //subject.LevelExp[Level-1]のLevelupとNeedExp以外のupを各対応するsubjectのステータスに足すしてsubject.Levelに1+してsubject.Expを0にする
        //acquisitionEXPを足し終わったら処理を終了
        if (subject == null)
        {
            Debug.LogWarning("EXP_earnedprocess: subject が null です。");
            return;
        }

        if (acquisitionEXP <= 0)
        {
            return;
        }

        if (subject.LevelExp == null || subject.LevelExp.Count == 0)
        {
            Debug.LogWarning($"EXP_earnedprocess: {subject.name} の LevelExp テーブルがありません。");
            subject.Exp += acquisitionEXP;
            return;
        }

        for (int i = 0; i < acquisitionEXP; i++)
        {
            subject.Exp += 1;

            int levelIndex = subject.Level - 1;
            if (levelIndex < 0 || levelIndex >= subject.LevelExp.Count)
            {
                // テーブル範囲外はそのままEXPのみ加算して終了
                break;
            }

            var levelData = subject.LevelExp[levelIndex];

            if (subject.Exp >= levelData.NeedExp)
            {
                // レベルアップ時、LevelupとNeedExp以外の項目を反映
                subject.Sp += levelData.AcquisitionSp;
                subject.MaxHp += levelData.MaxHpup;
                subject.Hp = subject.MaxHp;
                subject.MaxMp += levelData.MaxMpup;
                subject.Mp = subject.MaxHp;
                subject.Attack += levelData.Attackup;
                subject.Magic += levelData.Magicup;
                subject.Defense += levelData.Defenceup;
                subject.MagicDefense += levelData.MagicDefenseup;
                subject.Speed += levelData.Speedup;

                subject.Level += 1;
                subject.Exp = 0;
            }
        }
    }
}
