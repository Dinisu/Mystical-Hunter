using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;
using App.BaseSystem.DataStores.ScriptableObjects.Status;

public class Result : MonoBehaviour
{
    // --- 外部参照（ステータス数値処理） ---
    public NumericalProcessing numericalProcessing;
    public static Result Instance { get; private set; }

    [SerializeField] private GameObject GaugeUI;
    [SerializeField] private GameObject Statusarea;

    [Header("勝利リザルト")]
    [SerializeField] private GameObject WinningResult;
    [SerializeField] private TextMeshProUGUI VictoryDeclaration;
    [SerializeField] private GameObject AllyEXP;
    [SerializeField] private TextMeshProUGUI ObtainedItems;
    [SerializeField] private TextMeshProUGUI Amount;
    [SerializeField] private TextMeshProUGUI AcquiredItems;
    [SerializeField] private TextMeshProUGUI Enter;

    [Header("敗北リザルト")]
    [SerializeField] private GameObject DefeatResult;
    [SerializeField] private TextMeshProUGUI MissionFailure;

    public bool isVictory = false;
    private bool isDefeat = false;
    private bool enterEnabled = false;

    private int acquisitionMoney;

    //データストア
    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;
    private Db_Ch_StatusDataBase db_allyDataBase;

    private void Awake()
    {
        // 数値処理スクリプト取得
        numericalProcessing = GameObject.Find("Numerical Processing").GetComponent<NumericalProcessing>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();

        // FindDatabaseWithName を使用してデータベースとAlly Listを取得
        db_allyDataBase = dss_Ch_StatusDataStores.FindDatabaseWithName("Ally List");
    }
    void Start()
    {

        // 初期状態でリザルトUIを非表示に設定
        WinningResult.SetActive(false);
        DefeatResult.SetActive(false);

        AllyEXP.SetActive(false);
    }

    // Update is called once per frame
    public void Victory_LossJudgment()
    {
        // 勝利条件のチェック
        if (!isVictory && CheckVictoryCondition())
        {
            //勝利処理開始
            StartCoroutine(ShowVictoryResult());
        }
        // 敗北条件のチェック
        else if (!isDefeat && CheckDefeatCondition())
        {
            //敗北処理開始
            StartCoroutine(ShowDefeatResult());
        }
    }

    private bool CheckVictoryCondition() // 勝利条件の判定
    {
        // BattleManager が存在しない or 敵リストが無い場合は勝利しない
        if (BattleManager.Instance == null ||
            BattleManager.Instance.EnemyParticipationList == null)
        {
            return false;
        }

        var enemyList = BattleManager.Instance.EnemyParticipationList;

        if (enemyList.Count == 0) return false;

        // 敵が1体でも生きていたら勝利ではない
        foreach (var enemy in enemyList)
        {
            if (enemy.Hp > 0)
            {
                return false;
            }
        }

        // 全員 HP 0 以下 → 勝利
        return true;
    }


    private bool CheckDefeatCondition()// 敗北条件の判定
    {
        if (db_allyDataBase == null) return false;

        var allies = db_allyDataBase.ItemList;

        foreach (var ally in allies)
        {
            // 1人でも生きていたら敗北ではない
            if (ally.Hp > 0) return false;
        }

        // 全員HP0以下 → 敗北
        return true;
    }

    private IEnumerator ShowVictoryResult()// 勝利リザルトの表示処理
    {
        isVictory = true;
        WinningResult.SetActive(true);
        GaugeUI.SetActive(false);
        Statusarea.SetActive(false);

        BattleManager.Instance.ActionName.text = ("");//行動名リセット

        // VictoryDeclarationの表示
        VictoryDeclaration.DOFade(1f, 1f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);


        yield return StartCoroutine(LootDisposal());

        // Enterの表示と点滅アニメーション
        Enter.DOFade(1f, 1f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);
        Enter.DOFade(0f, 1f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        enterEnabled = true;

        //エンカウントした敵の情報をクリア
        GameManager.Instance.ClearEncounteredEnemies();
        // 3秒待機後にシーン遷移
        yield return new WaitForSecondsRealtime(3f);
        // シーン遷移前に時間を戻す
        Time.timeScale = 1;
        SceneManager.LoadScene($"{GameManager.Instance.SceneName}");
    }

    private IEnumerator ShowDefeatResult()// 敗北リザルトの表示処理
    {
        isDefeat = true;
        DefeatResult.SetActive(true);
        GaugeUI.SetActive(false);
        Statusarea.SetActive(false);

        BattleManager.Instance.ActionName.text = ("");//行動名リセット

        // MissionFailureのテキスト表示
        MissionFailure.DOFade(1f, 2f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(2f);

        GameManager.Instance.ShouldRestorePlayerPosition = false;
        //エンカウントした敵の情報をクリア
        GameManager.Instance.ClearEncounteredEnemies();
        // 3秒待機後にシーン遷移
        yield return new WaitForSecondsRealtime(3f);
        // シーン遷移前に時間を戻す
        Time.timeScale = 1;
        SceneManager.LoadScene("Title");
    }

    private IEnumerator LootDisposal()
    {
        //BattleManager.Instance.AllyParticipationListにあるD_Ch_StatusDataをAllyEXPの子オブジェクトのB_Ch_EXPIcon1～4にあるCharacterIconStatus.statusに
        //上から順にある分入れていく入れられなかったB_Ch_EXPIconは非表示　
        //例：AllyParticipationListの中に2つD_Ch_StatusDataがあればD_Ch_StatusData1つ目→B_Ch_EXPIcon1のCharacterIconStatus.status、D_Ch_StatusData2つ目→B_Ch_EXPIcon2のCharacterIconStatus.status
        //B_Ch_EXPIcon3，4は非表示
        AllyEXP.SetActive(true);

        //表示中のB_Ch_EXPIconのCharacterIconStatus.StatusUpdateEXP(acquisitionEXP)を実行していく、複数でも同時に
        //acquisitionEXPはGameManager.Instance.EncounteredEnemysにあるD_Ch_StatusData.Expの合計
        List<D_Ch_StatusData> allyList = BattleManager.Instance != null
            ? BattleManager.Instance.AllyParticipationList
            : null;

        var shownIcons = new List<CharacterIconStatus>();

        for (int i = 0; i < 4; i++)
        {
            Transform iconTransform = AllyEXP.transform.Find($"B_Ch_EXPIcon{i + 1}");
            if (iconTransform == null) continue;

            bool shouldShow = allyList != null && i < allyList.Count && allyList[i] != null;
            iconTransform.gameObject.SetActive(shouldShow);

            if (!shouldShow) continue;

            CharacterIconStatus iconStatus = iconTransform.GetComponent<CharacterIconStatus>();
            if (iconStatus == null) continue;

            iconStatus.status = allyList[i];
            shownIcons.Add(iconStatus);
        }

        //獲得EXPの計算
        int acquisitionEXP = 0;
        if (GameManager.Instance != null && GameManager.Instance.EncounteredEnemys != null)
        {
            foreach (var enemy in GameManager.Instance.EncounteredEnemys)
            {
                if (enemy == null) continue;
                acquisitionEXP += enemy.Exp;
            }
        }
        //獲得したEXPの表示処理と獲得処理の呼び出し
        foreach (var icon in shownIcons)
        {
            icon.StatusUpdateEXP(acquisitionEXP);
            numericalProcessing.EXP_earnedprocess(icon.status, acquisitionEXP);
        }

        //獲得したお金の計算とアイテムドロップの判定
        acquisitionMoney = 0;
        var acquiredItemMap = new Dictionary<string, int>();
        if (GameManager.Instance != null && GameManager.Instance.EncounteredEnemys != null)
        {
            foreach (var enemy in GameManager.Instance.EncounteredEnemys)
            {
                if (enemy == null) continue;
                acquisitionMoney += enemy.Money;

                if (enemy.DroppedItems == null) continue;
                foreach (var item in enemy.DroppedItems)
                {
                    if (item == null || item.DroppedItem == null) continue;

                    //ドロップ判定
                    bool dropped = numericalProcessing.Random_drop(item.DroppedItem, item.Droprate);
                    if (!dropped) continue;

                    string itemName = string.IsNullOrEmpty(item.DroppedItem.Name)? item.DroppedItem.name : item.DroppedItem.Name;

                    if (!acquiredItemMap.ContainsKey(itemName))
                    {
                        acquiredItemMap[itemName] = 0;
                    }
                    acquiredItemMap[itemName] += 1;
                }
            }
        }

        // ▼ 所持金に加算
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerMoney += acquisitionMoney;
        }

        // ObtainedItemsの表示
        ObtainedItems.DOFade(1f, 1f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);

        //ドロップアイテムの表示
        if (acquiredItemMap.Count == 0)
        {
            AcquiredItems.text = "なし";
        }
        else
        {
            string resultText = "";
            foreach (var kv in acquiredItemMap)
            {
                resultText += $"{kv.Key} x{kv.Value}\n";
            }
            AcquiredItems.text = resultText.TrimEnd('\n');
        }
        AcquiredItems.DOFade(1f, 1f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);

        // Amountの表示
        Amount.text = $"${acquisitionMoney}";
        Amount.DOFade(1f, 1f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);
    }
}
