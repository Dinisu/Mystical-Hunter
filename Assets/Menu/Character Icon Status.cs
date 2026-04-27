using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterIconStatus : MonoBehaviour
{
    [SerializeField, Header("表示するキャラクターステータスデータ")]
    public D_Ch_StatusData status;

    [SerializeField, Header("表示するテキスト")]
    private TextMeshProUGUI StatusText1;
    [SerializeField]
    private TextMeshProUGUI StatusText2;
    [SerializeField, Header("表示するオブジェクト")]
    private Transform Displayfields;
    [SerializeField] private GameObject buffIconPrefab;

    // ActiveBuff と UI の紐付け
    private Dictionary<object, GameObject> buffIconMap = new();

    /// <summary>
    /// 装備などでステータスが変わった際呼ぶ,アイコンステータス
    /// </summary>
    public void StatusUpdates() 
    {
        StatusText1.text = ($"{status.Name}   Lv{status.Level}   EXP{status.Exp}/{status.LevelExp[status.Level].NeedExp}");
        StatusText2.text = ($"HP{status.Hp}/{status.MaxHp}   MP{status.Mp}/{status.MaxMp}");
    }

    /// <summary>
    /// 小さい方のアイコン用
    /// </summary>
    public void StatusUpdateMini()
    {
        StatusText1.text = ($"{status.Name}   Lv{status.Level}\nHP{status.Hp}/{status.MaxHp}\nMP{status.Mp}/{status.MaxMp}");
    }

    /// <summary>
    /// 戦闘用,HP,MPステータス表示
    /// バフ表示
    /// </summary>
    public void StatusUpdateBattle()
    {
        StatusText1.text = ($"{status.Name}　\nHP{status.Hp}/{status.MaxHp}\nMP{status.Mp}/{status.MaxMp}");

        // ▼ 現在の全Buffをまとめる
        // ▼ 現在存在するバフ一覧を統合
        List<object> currentBuffs = new();

        foreach (var b in status.ActiveBuffs)
            currentBuffs.Add(b);

        foreach (var b in status.ActiveBuffs_It)
            currentBuffs.Add(b);

        // =========================
        // 既存バフの更新 or 新規生成
        // =========================
        foreach (var buffObj in currentBuffs)
        {
            int remainingTurns = 0;
            Sprite iconSprite = null;

            // ▼ 型ごとに取得
            if (buffObj is ActiveBuff<D_Sk_StatusData> skBuff)
            {
                remainingTurns = skBuff.remainingTurns;
                iconSprite = skBuff.baseData.DataIcon;
            }
            else if (buffObj is ActiveBuff<D_It_StatusData> itBuff)
            {
                remainingTurns = itBuff.remainingTurns;
                iconSprite = itBuff.baseData.DataIcon;
            }

            // ▼ 既に存在する場合 → 更新
            if (buffIconMap.ContainsKey(buffObj))
            {
                var iconObj = buffIconMap[buffObj];

                // ターン更新
                var text = iconObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                text.text = remainingTurns.ToString();
            }
            else
            {
                // ▼ 新規生成
                GameObject iconObj = Instantiate(buffIconPrefab, Displayfields);

                // 画像
                var image = iconObj.GetComponent<UnityEngine.UI.Image>();
                image.sprite = iconSprite;

                // ターン表示
                var text = iconObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

                text.text = remainingTurns.ToString();

                // 登録
                buffIconMap.Add(buffObj, iconObj);
            }
        }

        // =========================
        // 消えたバフの削除
        // =========================
        var removeList = new List<object>();

        foreach (var kvp in buffIconMap)
        {
            var buffObj = kvp.Key;
            bool exists = currentBuffs.Contains(buffObj);

            int remainingTurns = 0;

            if (buffObj is ActiveBuff<D_Sk_StatusData> skBuff)
                remainingTurns = skBuff.remainingTurns;
            else if (buffObj is ActiveBuff<D_It_StatusData> itBuff)
                remainingTurns = itBuff.remainingTurns;

            // ▼ 条件：リストに無い or ターン0
            if (!exists || remainingTurns <= 0)
            {
                Destroy(kvp.Value);
                removeList.Add(buffObj);
            }
        }

        // 辞書から削除
        foreach (var key in removeList)
            buffIconMap.Remove(key);
    }

    public void StatusUpdateEXP(int acquisitionEXP)//獲得EXP表示
    {
        //currentExpにacquisitionEXPを1ずつ足して表示、を繰り返して視覚的にEXPが溜まっているのを表現
        //currentExpがstatus.LevelExp[status.Level - 1].NeedExp以上になった時currentExpを0にしてcurrentLevelを1+
        StopAllCoroutines();
        StartCoroutine(AnimateExpGain(acquisitionEXP));
    }


    /// <summary>
    /// 所持金表示
    /// </summary>
    public void DisplayOfMoneyHeld()
    {
        StatusText1.text = ($"所持金:{GameManager.Instance.PlayerMoney}$");
    }

    /// <summary>
    /// 獲得EXPを1ずつ足して表示、を繰り返して視覚的にEXPが溜まっているのを表現
    /// </summary>
    private IEnumerator AnimateExpGain(int acquisitionEXP)
    {
        if (status == null || StatusText1 == null) yield break;
        if (status.LevelExp == null || status.LevelExp.Count == 0) yield break;

        int currentExp = Mathf.Max(0, status.Exp);
        int currentLevel = Mathf.Max(1, status.Level);

        for (int i = 0; i < acquisitionEXP; i++)
        {
            // テーブル参照範囲外にならないように調整
            int tableIndex = Mathf.Clamp(currentLevel - 1, 0, status.LevelExp.Count - 1);
            int needExp = Mathf.Max(1, status.LevelExp[tableIndex].NeedExp);

            currentExp += 1;

            //獲得Expがレベルアップに必要な量に達したら
            if (currentExp >= needExp)
            {
                currentExp = 0;
                currentLevel += 1;
            }

            StatusText1.text = $"{status.Name} Lv{currentLevel} \n{currentExp}/{needExp}";
            yield return new WaitForSecondsRealtime(0.01f);
        }

        int finalIndex = Mathf.Clamp(currentLevel - 1, 0, status.LevelExp.Count - 1);
        int finalNeedExp = Mathf.Max(1, status.LevelExp[finalIndex].NeedExp);
        StatusText1.text = $"{status.Name} Lv{currentLevel} \n{currentExp}/{finalNeedExp}";
    }
}
