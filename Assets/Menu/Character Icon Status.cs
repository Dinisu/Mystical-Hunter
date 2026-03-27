using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using TMPro;
using UnityEngine;

public class CharacterIconStatus : MonoBehaviour
{
    [SerializeField, Header("表示するキャラクターステータスデータ")]
    public D_Ch_StatusData status;

    [SerializeField, Header("表示するテキスト")]
    public TextMeshProUGUI StatusText1;
    public TextMeshProUGUI StatusText2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void StatusUpdates() //装備などでステータスが変わった際呼ぶ,アイコンステータス
    {
        StatusText1.text = ($"{status.Name}   Lv{status.Level}   EXP{status.Exp}/{status.LevelExp[status.Level].NeedExp}");
        StatusText2.text = ($"HP{status.Hp}/{status.MaxHp}   MP{status.Mp}/{status.MaxMp}");
    }

    public void StatusUpdateMini()//小さい方のアイコン用
    {
        StatusText1.text = ($"{status.Name}   Lv{status.Level}\nHP{status.Hp}/{status.MaxHp}\nMP{status.Mp}/{status.MaxMp}");
    }

    public void StatusUpdateBattle()//戦闘用
    {
        StatusText1.text = ($"{status.Name}　\nHP{status.Hp}/{status.MaxHp}\nMP{status.Mp}/{status.MaxMp}");
    }

    public void StatusUpdateEXP(int acquisitionEXP)//獲得EXP表示
    {
        //currentExpにacquisitionEXPを1ずつ足して表示、を繰り返して視覚的にEXPが溜まっているのを表現
        //currentExpがstatus.LevelExp[status.Level - 1].NeedExp以上になった時currentExpを0にしてcurrentLevelを1+
        StopAllCoroutines();
        StartCoroutine(AnimateExpGain(acquisitionEXP));
    }

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
