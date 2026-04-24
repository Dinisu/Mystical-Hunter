using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using static TimelineIconController;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class BattleManager : MonoBehaviour
{
    // --- 外部参照（ステータス数値処理） ---
    public NumericalProcessing numericalProcessing;

    public static BattleManager Instance;

    //エンカウントスクリプトから参戦リストに
    [SerializeField, Header("味方参戦リスト")]
    public List<D_Ch_StatusData> AllyParticipationList = new List<D_Ch_StatusData>();
    [SerializeField]private List<GameObject> Spawn_points = new List<GameObject>();
    [SerializeField] private List<GameObject> character = new List<GameObject>();
    [SerializeField, Header("敵参戦リスト")]
    public List<D_Ch_StatusData> EnemyParticipationList = new List<D_Ch_StatusData>();
    [SerializeField] private List<GameObject> E_Spawn_points = new List<GameObject>();
    [SerializeField] private List<GameObject> E_character = new List<GameObject>();

    [Header("TimelineIconController")]
    [SerializeField] private TimelineIconController timelineIconController;

    [SerializeField, Header("行動UI")] private GameObject ActionSelectionUI;
    private ObjectMarker marker;

    [SerializeField, Header("行動する物の名前表示")]
    public TextMeshProUGUI ActionName;

    //データストア
    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;
    private Dss_It_StatusDataStores dss_It_StatusDataStores;

    private List<GameObject> spawnedAllies = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    [Header("Gauge Parent")]
    [SerializeField] private Transform gaugeParent;


    [Header("タイムライン関係")]
    public List<TimelineIconController> allyIcons = new();
    public List<TimelineIconController> enemyIcons = new();

    [Header("行動選択UI")]
    [SerializeField, Header("行動選択")]public List<GameObject> ActionSelection = new List<GameObject>();
    [SerializeField, Header("スキル選択")]public List<GameObject> SkillSelection = new List<GameObject>();
    [SerializeField, Header("アイテム選択")]public List<GameObject> ItemSelection = new List<GameObject>();
    [SerializeField, Header("味方交代")]public List<GameObject> Switching_sides = new List<GameObject>();
    [SerializeField, Header("範囲選択")]public List<GameObject> Area_of_Effect = new List<GameObject>();

    [SerializeField]
    private bool QuickAction = false;//行動選択外でクイックを発動しようとしているか

    //UI生成する場合
    private GameObject actionField;
    private GameObject skillField;
    private GameObject itemField;
    private GameObject allyField;
    [HideInInspector] public GameObject StatusIcons;

    // --- UI操作関連 ---
    private GameObject[] uiElements; // UI格納配列（切り替え用）
    private int currentIndex = 0;    // 選択中インデックス
    public GameObject SelectUI;      // 現在選択中のUI

    // 履歴管理システム
    private List<MenuHistory> menuHistory = new List<MenuHistory>();

    [System.Serializable]
    private class MenuHistory
    {
        public GameObject[] elements;//その時に操作可能だった UI 要素の配列
        public int index;//選択されていた要素の位置
        public string menuType; //メニューの種類　例:"ActionSelection", "SkillSelection", "ItemSelection", "Switching_sides", "Area_of_Effect"

        public MenuHistory(GameObject[] elements, int index, string menuType)
        {
            this.elements = elements;
            this.index = index;
            this.menuType = menuType;
        }
    }

    public GameObject Frame;                 // 枠：点滅対象
    private Tween blinkTween;
    private CanvasGroup frameCg;

    private PlayerInput playerInput;
    private bool NormalinputEnabled = false; // 入力処理の有効/無効フラグ

    // 現在選択中のキャラクターデータ（スキル生成用）
    [SerializeField] private D_Ch_StatusData currentCharacterData;
    private Db_It_StatusDataBase db_PlayerItem;
    private Db_Ch_StatusDataBase db_allyDataBase;

    [SerializeField, Header("説明テキスト")]
    private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    private void Awake()
    {
        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();
        dss_It_StatusDataStores = FindObjectOfType<Dss_It_StatusDataStores>();
        // 数値処理スクリプト取得
        numericalProcessing = GameObject.Find("Numerical Processing").GetComponent<NumericalProcessing>();

        // FindDatabaseWithName を使用して Player_Item データベースとAlly Listを取得
        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Player_Item");
        db_allyDataBase = dss_Ch_StatusDataStores.FindDatabaseWithName("Ally List");

        marker = ActionSelectionUI.GetComponent<ObjectMarker>();

        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupBattleParticipants();//参戦キャラ設定

        // --- バフ初期化 ---
        ClearAllBuffs();

        // もしInspectorで未設定なら、シーンから自動検索
        if (gaugeParent == null)
        {
            GameObject gaugeObj = GameObject.Find("Gauge");
            if (gaugeObj != null)
                gaugeParent = gaugeObj.transform;
            else
                Debug.LogError("Gauge オブジェクトがシーンにありません！");
        }

        actionField = FindUIObject("ActionField");
        skillField = FindUIObject("SkillField");
        itemField = FindUIObject("ItemField");
        allyField = FindUIObject("AllyField");
        StatusIcons = FindUIObject("Status Icons");

        //キャラクター生成
        SpawnAllies();
        SpawnEnemies();

        marker.Hide();

        // 他のバトル初期化処理（例）
        // InitializeTimeline();
        // StartCoroutine(BattleLoop());
        //特性があればここで発動出来るように
        foreach (var icon in allyIcons)
        {
            icon.OnEnterActionZone += HandleActionSelection;
            icon.OnActionExecute += HandleActionExecution;

            AbilityGrant(icon);
        }

        foreach (var icon in enemyIcons)
        {
            icon.OnEnterActionZone += HandleEnemyAction;
            icon.OnActionExecute += HandleActionExecution;

            AbilityGrant(icon);
        }

        // UI操作の初期化
        InitializeUI();
    }

    /// <summary>
    /// アビリティの付与
    /// 複数のキャラクター付与対策をする
    /// </summary>
    private IEnumerator AbilityGrant(TimelineIconController icon)
    {
        if (icon == null) yield break;
        var skillDB = icon.characterData.SkillList.ItemList;

        foreach (var skill in skillDB)
        {
            switch (skill.SeeKinds)
            {
                case D_Sk_StatusData.Kinds.Abilities:
                    numericalProcessing.SkillUse_SkData = skill;//発動するスキル
                    numericalProcessing.Use_ChData = icon.characterData;//発動するキャラクター
                    numericalProcessing.Use_subject_ChData = icon.characterData;//発動される対象
                    numericalProcessing.Use_characterObject = icon.characterObject;//発動される対象オブジェクト
                    ActionName.text = ($"{skill.Name}");//発動スキル名表示
                    
                    // エフェクト再生完了まで待つ
                    yield return StartCoroutine(numericalProcessing.DamageCalculationAsync(skill));

                    break;
            }
        }
    }

    private void SetupBattleParticipants()//参戦キャラ設定
    {
        AllyParticipationList.Clear();
        EnemyParticipationList.Clear();

        // =====================
        // 味方の設定（上から3体）
        // =====================
        if (db_allyDataBase != null)
        {
            int allyCount = Mathf.Min(3, db_allyDataBase.ItemList.Count);

            for (int i = 0; i < allyCount; i++)
            {
                var data = db_allyDataBase.ItemList[i];
                if (data != null)
                {
                    AllyParticipationList.Add(data);
                }
            }
        }
        else
        {
            Debug.LogError("味方用 Db_Ch_StatusDataBase が設定されていません");
        }

        // =====================
        // 敵の設定（エンカウント時決定済み）
        // =====================
        if (GameManager.Instance != null &&
            GameManager.Instance.EncounteredEnemys != null &&
            GameManager.Instance.EncounteredEnemys.Count > 0)
        {
            foreach (var enemy in GameManager.Instance.EncounteredEnemys)
            {
                if (enemy != null)
                {
                    EnemyParticipationList.Add(enemy);
                }
            }
        }
        else
        {
            Debug.LogWarning("エンカウント敵データがありません");
        }

        // デバッグ表示
        Debug.Log($"【BattleManager】味方 {AllyParticipationList.Count} 体 / 敵 {EnemyParticipationList.Count} 体 で戦闘開始");
    }

    private GameObject FindUIObject(string name)//UI取得
    {
        // シーン上の全てのUI(Imageを含む)から検索
        var all = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (var ui in all)
        {
            if (ui.gameObject.name == name && ui.gameObject.scene.IsValid())
            {
                return ui.gameObject;
            }
        }

        Debug.LogError($"{name} がシーン上に見つかりません！");
        return null;
    }

    private void InitializeUI()
    {
        // Frame に CanvasGroup がなければ追加
        if (Frame != null)
        {
            frameCg = Frame.GetComponent<CanvasGroup>();
            if (frameCg == null) frameCg = Frame.AddComponent<CanvasGroup>();
            frameCg.alpha = 0f; // 初期は非選択状態（透明）
        }

        // UI要素リスト設定（ActionSelectionから始まる）
        uiElements = ActionSelection.ToArray();

        // PlayerInput を探す
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput がシーン内に見つかりません！");
            }
        }

        // 最初のUIを選択状態に設定
        if (uiElements.Length > 0)
        {
            currentIndex = 0;
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI); // 枠を移動
            StartBlink();          // 枠を点滅開始
        }

        // 最初は非表示にしておく
        if (skillField != null) skillField.SetActive(false);
        if (itemField != null) itemField.SetActive(false);
        if (allyField != null) allyField.SetActive(false);

        // 入力イベントを初期化時に登録（以降、ずっと有効にしておく）
        RegisterInputEvents();
    }

    private void SpawnAllies()//味方の生成
    {
        for (int i = 0; i < AllyParticipationList.Count; i++)
        {
            if (i >= Spawn_points.Count)
            {
                Debug.LogWarning("スポーンポイント不足（味方）");
                break;
            }

            var data = AllyParticipationList[i];
            if (data == null || data.B_Character == null)
            {
                Debug.LogError("参戦キャラデータまたはPrefabが未設定（味方）");
                continue;
            }

            // 生成
            GameObject spawnParent = Spawn_points[i];
            GameObject spawnedCharacter = Instantiate(data.B_Character, spawnParent.transform);

            // 見やすいようにローカル位置をゼロ
            spawnedCharacter.transform.localPosition = Vector3.zero;

            spawnedAllies.Add(spawnedCharacter);
            character.Add(spawnedCharacter); // characterリストにも追加

            // Timeline にキャラを自動バインド
            if (timelineIconController != null)
            {
                timelineIconController.BindCharacter(spawnedCharacter);
            }

            //  B_Icon の生成（Gauge の子へ）
            TimelineIconController iconController = null;
            if (data.B_Icon != null && gaugeParent != null)
            {
                GameObject icon = Instantiate(data.B_Icon, gaugeParent);
                icon.transform.localPosition = Vector3.zero;

                // TimelineIconController を取得
                iconController = icon.GetComponent<TimelineIconController>();
                if (iconController == null)
                    Debug.LogError("B_Icon に TimelineIconController がついていません！");
            }

            // characterObject をキャラに設定
            if (iconController != null)
            {
                iconController.characterObject = spawnedCharacter;
                iconController.characterData = data; // ← 推奨（データセット）
                allyIcons.Add(iconController);//タイムラインにキャラアイコンを入れる
            }

            // B_Icon_Ch の生成 & 配置（追加）
            if (data.B_Icon_Ch != null && StatusIcons != null)
            {
                GameObject statusIcon = Instantiate(data.B_Icon_Ch, StatusIcons.transform);

                // Y座標を 320 / 160 / 0 にする
                float yPos = 320 - (i * 160);
                statusIcon.transform.localPosition = new Vector3(0, yPos, 0);

                // ★ CharacterIconStatus を取得
                CharacterIconStatus iconStatus = statusIcon.GetComponent<CharacterIconStatus>();
                if (iconStatus != null)
                {
                    // ★ ステータス更新を呼ぶ
                    iconStatus.StatusUpdateBattle();
                }
                else
                {
                    Debug.LogError("CharacterIconStatus が B_Icon_Ch にアタッチされていません");
                }

                Debug.Log($"StatusIcon 生成: {data.name} → Y={yPos}");
            }
            else
            {
                Debug.Log($"StatusIcon 生成できませんでした。");
            }
        }
    }

    private void SpawnEnemies()//敵の生成
    {
        for (int i = 0; i < EnemyParticipationList.Count; i++)
        {
            if (i >= E_Spawn_points.Count)
            {
                Debug.LogWarning("スポーンポイント不足（敵）");
                break;
            }

            var data = EnemyParticipationList[i];
            if (data == null || data.B_Character == null)
            {
                Debug.LogError("参戦キャラデータまたはPrefabが未設定（敵）");
                continue;
            }

            // 生成
            GameObject spawnParent = E_Spawn_points[i];
            GameObject spawnedEnemy = Instantiate(data.B_Character, spawnParent.transform);

            spawnedEnemy.transform.localPosition = Vector3.zero;

            spawnedEnemies.Add(spawnedEnemy);
            E_character.Add(spawnedEnemy); // E_characterリストにも追加

            // Timeline にキャラを自動バインド
            if (timelineIconController != null)
            {
                timelineIconController.BindCharacter(spawnedEnemy);
            }

            //  B_Icon の生成（Gauge の子へ）
            TimelineIconController iconController = null;
            if (data.B_Icon != null && gaugeParent != null)
            {
                GameObject icon = Instantiate(data.B_Icon, gaugeParent);
                icon.transform.localPosition = Vector3.zero;

                // TimelineIconController を取得
                iconController = icon.GetComponent<TimelineIconController>();
                if (iconController == null)
                    Debug.LogError("B_Icon に TimelineIconController がついていません！");
            }

            // characterObject をキャラに設定
            if (iconController != null)
            {
                iconController.characterObject = spawnedEnemy;
                iconController.characterData = data; // ← 推奨（データセット）
                enemyIcons.Add(iconController);//タイムラインにキャラアイコンを入れる
            }
        }
    }

    private void ClearAllBuffs()// 参戦中の全キャラクター（味方・敵）のアクティブバフを初期化する
    {
        // 味方側
        foreach (var ally in AllyParticipationList)
        {
            if (ally == null) continue;

            ally.ActiveBuffs?.Clear();
            ally.ActiveBuffs_It?.Clear();
        }

        // 敵側
        foreach (var enemy in EnemyParticipationList)
        {
            if (enemy == null) continue;

            enemy.ActiveBuffs?.Clear();
            enemy.ActiveBuffs_It?.Clear();
        }

        Debug.Log("全キャラクターのActiveBuff / ActiveBuff_Itを初期化しました。");
    }

    // 味方が行動ゾーンに入った
    private void HandleActionSelection(TimelineIconController icon)
    {
        // 行動選択UIを開く
        Debug.Log($"{icon.characterData.name} のコマンド選択開始");

        // 現在選択中のキャラクターデータを保存
        currentCharacterData = icon.characterData;

        // --- タイムライン上の全アイコンを停止 ---
        Stopallicons();

        //前の行動選択データをリセット
        numericalProcessing.Use_ChData = null;
        numericalProcessing.Use_subject_ChData = null;
        numericalProcessing.Use_characterObject = null;
        numericalProcessing.ItemUse_ItData = null;
        numericalProcessing.SkillUse_SkData = null;


        icon.ActionReset();

        // コマンド選択完了後、実際のスキルデータをセットして再開
        //skillUIを行動ゾーンに入った味方に子オブジェクトとして生成
        // marker がセットされていない場合、自動で探す

        //marker._target = 行動ゾーンに入った味方のキャラクターオブジェクト
        if (marker != null)
        {
            marker._target = icon.characterObject.transform;
            marker.Show();  //ここで表示
            marker.OnUpdatePosition();
        }

        // UI操作を有効化
        SetInputEnabled(true);

        // ActionFieldを表示
        if (actionField != null)
        {
            actionField.SetActive(true);
        }

        // クイック行動中でなければ履歴をクリア
        if (QuickAction)
        {
            menuHistory.Clear();
        }

        // ActionSelectionから開始
        uiElements = ActionSelection.ToArray();
        currentIndex = 0;
        if (uiElements.Length > 0)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
            StartBlink();
        }
    }


    // 敵が行動ゾーンに入った（自動で決定）
    private void HandleEnemyAction(TimelineIconController icon)
    {
        // SkillList（Db_Sk_StatusDataBase）取得
        var skillDB = icon.characterData.SkillList;

        // null または 0 件チェック
        if (skillDB == null || skillDB.ItemList == null || skillDB.ItemList.Count == 0)
        {
            Debug.LogWarning($"{icon.characterData.name} はスキルを持っていません！");
            icon.ActivatedSkills = null;
            return;
        }

        // ランダムスキル選択（UnityEngine.Random を明示）
        int randIndex = UnityEngine.Random.Range(0, skillDB.ItemList.Count);

        // スキル取得
        var selectedSkill = skillDB.ItemList[randIndex];

        // セット
        icon.ActivatedSkills = selectedSkill;

        HandleEnemyTargetSelection(icon);//対象選択

        Debug.Log($"{icon.characterData.name} がランダムに '{selectedSkill.name}' を選択しました。");
        //SkillUseProcess()を参考に対象選択、敵側なのでcharacterとE_characterは逆になる
        //対象はicon.Target_of_Actionに入れる
        /*
        いつか実装
        MPが足りないスキルを除外
        クールタイム中スキップ
        バフ系優先
        スキルの重みづけ（AI賢く）
         */

        icon.state = TimelineState.Acting_up;
    }

    private void HandleEnemyTargetSelection(TimelineIconController icon)//敵の自動対象選択
    {
        var skill = icon.ActivatedSkills;

        if (skill == null)
        {
            Debug.LogError("敵AI: ActivatedSkills が設定されていません！");
            return;
        }

        // ターゲット候補リスト（GameObject）
        List<GameObject> candidates = new List<GameObject>();

        switch (skill.SeeSkillRange)
        {
            case D_Sk_StatusData.SkillRange.Himself:
                // 敵自身
                candidates.Add(icon.characterObject);
                break;

            case D_Sk_StatusData.SkillRange.Single_ally:
                // 敵AIの味方（E_character）
                candidates.AddRange(E_character);
                break;

            case D_Sk_StatusData.SkillRange.Alla_llies:
                // 敵AIの味方全体
                candidates.AddRange(E_character);
                break;

            case D_Sk_StatusData.SkillRange.Single_enemy:
                // 敵AIにとっての「敵」= プレイヤーの味方（character）
                candidates.AddRange(character);
                break;

            case D_Sk_StatusData.SkillRange.All_enemies:
                // 敵AIにとっての「敵」全体
                candidates.AddRange(character);
                break;

            default:
                Debug.LogWarning("敵AI: 未対応のSkillRangeです → " + skill.SeeSkillRange);
                return;
        }

        if (candidates.Count == 0)
        {
            Debug.LogError("敵AI: ターゲット候補が 0 件です。");
            return;
        }

        // --- Single系はランダム選択 ---
        GameObject selectedObject;

        if (skill.SeeSkillRange == D_Sk_StatusData.SkillRange.Single_ally ||
            skill.SeeSkillRange == D_Sk_StatusData.SkillRange.Single_enemy)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            selectedObject = candidates[index];
        }
        else
        {
            // All系は任意で1つ代表を入れる（ここはバトルロジック次第で変更可能）
            selectedObject = candidates[0];
        }

        // --- selectedObject の名前からターゲット名を抽出 ---
        // 例: "B_Taro(Clone)" → "Taro"
        string rawName = selectedObject.name;
        string targetName = rawName;

        // 先頭の "B_" を除去
        if (targetName.StartsWith("B_"))
            targetName = targetName.Substring(2);

        // 後ろの "(Clone)" を除去
        targetName = targetName.Replace("(Clone)", "");

        // --- AllyParticipationList / EnemyParticipationList の中から一致する名前を探す ---
        D_Ch_StatusData foundTarget = null;

        // まず味方
        foreach (var data in AllyParticipationList)
        {
            if (data.name == targetName)
            {
                foundTarget = data;
                break;
            }
        }

        // 見つからなければ敵
        if (foundTarget == null)
        {
            foreach (var data in EnemyParticipationList)
            {
                if (data.name == targetName)
                {
                    foundTarget = data;
                    break;
                }
            }
        }

        // --- 見つかったらセット ---
        if (foundTarget != null)
        {
            // 最終ターゲットとして設定
            icon.Target_of_Action = foundTarget;
            Debug.Log($"ターゲット決定 → {foundTarget.name}");
        }
        else
        {
            Debug.LogError($"ターゲット名 '{targetName}' に一致するデータが見つかりませんでした。");
            return;
        }

        if (icon == null)
        {
            Debug.LogError("敵AI: ターゲットの D_Ch_StatusData を取得できません → " + selectedObject.name);
            return;
        }

        Debug.Log($"敵AI: '{icon.characterData.name}' の対象は '{icon.Target_of_Action.name}' です。");
    }

    /// <summary>
    /// 行動発動（味方・敵共通）
    /// </summary>
    private void HandleActionExecution(TimelineIconController icon)
    {
        Debug.Log($"{icon.characterData.name} の技発動！");
        numericalProcessing.Use_ChData = icon.characterData;

        //全アイコンを停止

        // 技発動例（ここでスキルデータを指定）
        //行動発動の味方のデータをNumericalProcessingに送る
        //スキル、アイテムの発動と必要なデータを送る
        if (icon.ActivatedSkills != null)
        {
            numericalProcessing.SkillUse_SkData = icon.ActivatedSkills;//発動するスキル
            numericalProcessing.Use_ChData = icon.characterData;//発動するキャラクター
            numericalProcessing.Use_subject_ChData = icon.Target_of_Action;//発動される対象
            numericalProcessing.Use_characterObject = icon.characterObject;//発動される対象オブジェクト
            ActionName.text = ($"{icon.ActivatedSkills.Name}");//発動スキル名表示
            numericalProcessing.DamageCalculation();
        }
        else
        {
            numericalProcessing.ItemUse_ItData = icon.ActivatedItem;//発動するアイテム
            numericalProcessing.Use_ChData = icon.characterData;//発動するキャラクター
            numericalProcessing.Use_subject_ChData = icon.Target_of_Action;//発動される対象
            numericalProcessing.Use_characterObject = icon.characterObject;//発動される対象オブジェクト
            ActionName.text = ($"{icon.ActivatedItem.Name}");//発動アイテム名表示
            numericalProcessing.Itemtypedetermination();
        }

        //行動選択データをリセット
        numericalProcessing.Use_ChData = null;
        numericalProcessing.Use_subject_ChData = null;
        numericalProcessing.Use_characterObject = null;
        numericalProcessing.ItemUse_ItData = null;
        numericalProcessing.SkillUse_SkData = null;

        icon.ActionReset();

        Area_of_Effect.Clear();

        // ターン経過処理
        ProcessBuffDurations(icon.characterData);

        //全アイコンを停止再始動

        // 発動後、タイムラインをリセットして再開
        icon.ResumeMovement();

        // UI操作を有効化
        SetInputEnabled(false);
    }

    /// <summary>
    /// バフのターン経過処理
    /// </summary>
    private void ProcessBuffDurations(D_Ch_StatusData character)
    {
        if (character == null) return;

        // --- スキル由来バフ ---
        if (character.ActiveBuffs != null && character.ActiveBuffs.Count > 0)
        {
            // ターンを減らして、切れたものを削除
            for (int i = character.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var activeBuff = character.ActiveBuffs[i];
                activeBuff.remainingTurns--;

                if (activeBuff.remainingTurns <= 0)
                {
                    Debug.Log($"{character.name} のスキルバフ「{activeBuff.baseData.name}」の効果が切れた！");
                    character.ActiveBuffs.RemoveAt(i);
                }
            }
        }

        // --- アイテム由来バフ ---
        if (character.ActiveBuffs_It != null && character.ActiveBuffs_It.Count > 0)
        {
            // ターンを減らして、切れたものを削除
            for (int i = character.ActiveBuffs_It.Count - 1; i >= 0; i--)
            {
                var activeBuffIt = character.ActiveBuffs_It[i];
                activeBuffIt.remainingTurns--;

                if (activeBuffIt.remainingTurns <= 0)
                {
                    Debug.Log($"{character.name} のアイテムバフ「{activeBuffIt.baseData.name}」の効果が切れた！");
                    character.ActiveBuffs_It.RemoveAt(i);
                }
            }
        }

        // ▼ UI更新（ステータスアイコンをすべて更新）
        numericalProcessing.UpdateAllStatusIcons();
    }

    // --- UI操作関連メソッド ---

    /// <summary>
    /// 入力イベントを登録する
    /// </summary>
    private void RegisterInputEvents()
    {
        if (playerInput == null) return;

        // 既存のイベントを解除
        UnregisterInputEvents();

        var move = playerInput.actions["Move"];
        move.performed += OnMove;
        move.canceled += OnMove;

        var attack = playerInput.actions["Attack"];
        attack.performed += OnAttack;

        var cancel = playerInput.actions["Cancel"];
        cancel.performed += OnCancel;
    }

    /// <summary>
    /// 入力イベントを解除する
    /// </summary>
    public void UnregisterInputEvents()
    {
        if (playerInput == null) return;

        var move = playerInput.actions["Move"];
        move.performed -= OnMove;
        move.canceled -= OnMove;
        
        var attack = playerInput.actions["Attack"];
        attack.performed -= OnAttack;

        var cancel = playerInput.actions["Cancel"];
        cancel.performed -= OnCancel;
    }

    /// <summary>
    /// Move入力（上下左右キー / スティック）を受け取る
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Vector2 input = context.ReadValue<Vector2>();
        string currentMenuType = GetCurrentMenuType();

        // 行動UI無効時は入力を処理しない
        if (!NormalinputEnabled) return;

        if (GameManager.Instance.audioSource != null && GameManager.Instance.choice != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.choice); // 選択音
        }

        if (input.y > 0.5f) // 上入力
        {
            MoveSelectionVertical(-1);
        }
        else if (input.y < -0.5f) // 下入力
        {
            MoveSelectionVertical(1);
        }

        //効果説明
        if (currentMenuType == "ItemSelection")
        {
            Itemiconexplanation();
        }
        else if (currentMenuType == "SkillSelection")
        {
            Skilliconexplanation();
        }
        else
        {
            skillDescriptionText.text = ("");
            itemDescriptionText.text = ("");
        }
    }

    /// <summary>
    /// Attack入力（決定ボタン）を受け取る
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        //通常入力有効がfalseならクイック行動に入る
        if (!NormalinputEnabled)
        {
            QuickActionSelection();
            return;
        }

        if (SelectUI != null)
        {
            Debug.Log("Attack pressed → " + SelectUI.name);

            if (GameManager.Instance.audioSource != null && GameManager.Instance.decision != null)
            {
                GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.decision); // 決定音
            }

            // 選択中UIにアタッチされた B_SelectionSettings を取得
            var settings = SelectUI.GetComponent<B_SelectionSettings>();
            if (settings != null)
            {
                // enum Choose を確認
                switch (settings.choose)
                {
                    case B_SelectionSettings.Choose.SkillSelection://スキル選択
                        SkillSelectionProcess();
                        break;
                    case B_SelectionSettings.Choose.ItemSelection://アイテム選択
                        ItemSelectionProcess();
                        break;
                    case B_SelectionSettings.Choose.Switchingsides://味方交代
                        Switching_sidesProcess();
                        break;
                    case B_SelectionSettings.Choose.StatusCheck://ステータス確認

                        break;
                    case B_SelectionSettings.Choose.Skill_Use://スキル使用
                        SkillUseProcess();
                        break;
                    case B_SelectionSettings.Choose.Item_Use://アイテム使用
                        ItemUseProcess();
                        break;
                    case B_SelectionSettings.Choose.Character_Switching://キャラクター交代
                        CharacterSwitchingProcess();
                        break;
                    case B_SelectionSettings.Choose.AreaofEffect://効果範囲
                        Targetdetermination();
                        break;
                    case B_SelectionSettings.Choose.run_away://逃げる
                        RunAwayProcess();
                        break;
                    case B_SelectionSettings.Choose.Quick_Action://クイック行動対象決定
                        QuickActionProcess();
                        break;

                    default:
                        Debug.Log("未対応の Choose: " + settings.choose);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("B_SelectionSettings がアタッチされていません → " + SelectUI.name);
            }
        }
    }

    /// <summary>
    /// Cancel入力（キャンセルボタン）を受け取る
    /// </summary>
    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // 行動UI無効時は入力を処理しない
        if (!NormalinputEnabled) return;

        if (GameManager.Instance.audioSource != null && GameManager.Instance.cancel != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.cancel); // キャンセル音
        }

        Debug.Log("Cancel pressed");
        PreviousList();
    }

    /// <summary>
    /// 現在の状態を履歴に保存する
    /// </summary>
    private void SaveCurrentStateToHistory()
    {
        string menuType = GetCurrentMenuType();
        MenuHistory history = new MenuHistory(uiElements, currentIndex, menuType);
        menuHistory.Add(history);
        Debug.Log($"履歴に保存: {menuType} (index: {currentIndex})　現在の履歴: {menuHistory.Count}");
    }

    /// <summary>
    /// 現在のメニュータイプを取得する
    /// </summary>
    private string GetCurrentMenuType()
    {
        if (uiElements.Length > 0 && ActionSelection.Count > 0 && uiElements[0] == ActionSelection[0])
            return "ActionSelection";
        else if (uiElements.Length > 0 && SkillSelection.Count > 0 && uiElements[0] == SkillSelection[0])
            return "SkillSelection";
        else if (uiElements.Length > 0 && ItemSelection.Count > 0 && uiElements[0] == ItemSelection[0])
            return "ItemSelection";
        else if (uiElements.Length > 0 && Switching_sides.Count > 0 && uiElements[0] == Switching_sides[0])
            return "Switching_sides";
        else if (uiElements.Length > 0 && Area_of_Effect.Count > 0 && uiElements[0] == Area_of_Effect[0])
            return "Area_of_Effect";
        else
            return "Unknown";
    }

    /// <summary>
    /// 前のメニューに戻る
    /// </summary>
    private void PreviousList()
    {
        string currentType = GetCurrentMenuType();
        //前の履歴チェック
        bool prevHistory = false;

        if (menuHistory.Count >= 1)
        {
            //前の履歴がある
            prevHistory = true;
        }

        //クイック行動をやめる
        if (QuickAction && prevHistory == false)
        {
            // --- タイムライン上の全アイコンを動かす ---
            Moveallicons();
            Area_of_Effect.Clear();

            QuickAction = false;
            NormalinputEnabled = false;

            marker.Hide();//行動UIを閉じる
            return;
        }
        else if (menuHistory.Count == 0)
        {
            Debug.LogWarning("履歴がありません。");
            return;
        }

        // 最新の履歴を取得
        MenuHistory lastHistory = menuHistory[menuHistory.Count - 1];
        menuHistory.RemoveAt(menuHistory.Count - 1);

        // SkillSelectionから戻る時にSkillFieldを非表示
        if (currentType == "SkillSelection")
        {
            skillField.SetActive(false);
            actionField.SetActive(true);

            skillDescriptionText.text = ("");
        }

        // ItemSelectionから戻る時に itemFieldを非表示
        if (currentType == "ItemSelection")
        {
            itemField.SetActive(false);
            actionField.SetActive(true);

            itemDescriptionText.text = ("");
        }

        // Switching_sidesから戻る時にAllyFieldを非表示
        if (currentType == "Switching_sides")
        {
            allyField.SetActive(false);
            actionField.SetActive(true);
        }

        if (currentType == "Area_of_Effect")
        {
            // Area_of_Effectリストをクリア（既存オブジェクトは削除しない）
            // B_SelectionSettingsコンポーネントも残す（既存オブジェクトなので）
            Area_of_Effect.Clear();

            // 戻る先のメニューに応じて対象フィールドを再表示
            if (lastHistory.menuType == "SkillSelection")
            {
                if (skillField != null) skillField.SetActive(true);
                if (itemField != null) itemField.SetActive(false);
                //if (actionField != null) actionField.SetActive(false);
            }
            else if (lastHistory.menuType == "ItemSelection")
            {
                if (itemField != null) itemField.SetActive(true);
                if (skillField != null) skillField.SetActive(false);
                //if (actionField != null) actionField.SetActive(false);
            }
        }

        //クイック行動対象選択に戻る
        if (currentType == "ActionSelection" && QuickAction)
        {
            actionField.SetActive(false);
        }

        // 履歴から復元
        uiElements = lastHistory.elements;
        currentIndex = lastHistory.index;

        // 配列と範囲の安全確認
        if (uiElements == null || uiElements.Length == 0)
        {
            Debug.LogWarning("UI要素が空です。復元できません。");
            return;
        }

        // 範囲外チェック
        if (currentIndex < 0 || currentIndex >= uiElements.Length)
        {
            currentIndex = Mathf.Max(0, uiElements.Length - 1);
        }

        if (uiElements[currentIndex] == null)
        {
            // nullの場合は最初の有効なオブジェクトを探す
            currentIndex = 0;
            for (int i = 0; i < uiElements.Length; i++)
            {
                if (uiElements[i] != null)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        SelectUI = uiElements[currentIndex];


        if (menuHistory.Count == 0)
        {
            prevHistory = false;
        }

        if (QuickAction && prevHistory == false)//クイック行動対象選択時のみ動くよう
        {
            // クイック行動中なら枠が見えるように親に移動
            if (ActionSelectionUI != null)
            {
                Frame.transform.SetParent(ActionSelectionUI.transform, false);
                MoveFrametoObject(SelectUI);
                Debug.Log($"クイック行動対象選択");
            }
        }
        else
        {
            MoveFrameTo(SelectUI);
        }

        Debug.Log($"現在の履歴: {menuHistory.Count}");
        Debug.Log($"履歴から復元: {lastHistory.menuType} (index: {lastHistory.index})");

        if (GameManager.Instance.audioSource != null && GameManager.Instance.decision != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.decision); // キャンセル音
        }
    }

    /// <summary>
    /// 選択中のUIを縦方向に切り替える
    /// </summary>
    private void MoveSelectionVertical(int direction)
    {
        if (uiElements.Length == 0) return;

        StopBlink();

        currentIndex += direction;
        if (currentIndex < 0) currentIndex = uiElements.Length - 1;
        else if (currentIndex >= uiElements.Length) currentIndex = 0;

        SelectUI = uiElements[currentIndex];

        if (SelectUI != null)
        {
            // UI か 3Dキャラかで処理を切り替える
            if (SelectUI.GetComponent<RectTransform>() != null)
            {
                // UI要素 → FrameをUIに合わせて移動
                MoveFrameTo(SelectUI);
            }
            else
            {
                // UIでない = Area_of_Effect (キャラオブジェクト)
                MoveFrametoObject(SelectUI);
            }
        }

        StartBlink();
    }

    /// <summary>
    /// 枠（Frame）を現在選択中のUIに移動
    /// </summary>
    private void MoveFrameTo(GameObject target)
    {
        if (Frame != null && target != null)
        {
            Frame.transform.SetParent(target.transform, false);

            RectTransform frameRect = Frame.GetComponent<RectTransform>();
            RectTransform targetRect = target.GetComponent<RectTransform>();

            if (frameRect != null && targetRect != null)
            {
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = Vector2.zero;
                frameRect.offsetMax = Vector2.zero;
                frameRect.localPosition = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 枠（Frame）を現在選択中のオブジェクトに移動
    /// </summary>
    private void MoveFrametoObject(GameObject target)
    {
        // 枠をターゲット位置に移動（正確に World → UI）
        SetMarkerPositionToWorldObject(target);

        // キャラの大きさに合わせて枠サイズを調整
        ResizeFrameToObject(target);
    }

    /// <summary>
    /// ワールド座標のキャラクター → UI座標に正しく変換する
    /// </summary>
    private void SetMarkerPositionToWorldObject(GameObject target)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);

        RectTransform markerRect = Frame.GetComponent<RectTransform>();
        markerRect.position = screenPos;   // ← これで確実に UI の正しい位置に置ける
    }

    /// <summary>
    /// UIをオブジェクトのサイズに合わせる処理
    /// </summary>
    private void ResizeFrameToObject(GameObject target)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        Collider collider = target.GetComponentInChildren<Collider>();

        Bounds bounds;

        if (renderer != null)
        {
            bounds = renderer.bounds;
        }
        else if (collider != null)
        {
            bounds = collider.bounds;
        }
        else
        {
            Debug.LogWarning($"{target.name} に Renderer / Collider が見つかりません。サイズ変更できません。");
            return;
        }

        // ----- UIに変換 -----
        Vector3 worldSize = bounds.size;                   // 3Dサイズ
        Vector3 screenSize = WorldSizeToUIScreenSize(worldSize);

        RectTransform markerRect = Frame.GetComponent<RectTransform>();
        markerRect.sizeDelta = new Vector2(screenSize.x, screenSize.y);
    }
    private Vector2 WorldSizeToUIScreenSize(Vector3 worldSize)
    {
        // ワールド空間の幅 → 画面上のピクセル幅
        Vector3 screenPoint1 = Camera.main.WorldToScreenPoint(Vector3.zero);
        Vector3 screenPoint2 = Camera.main.WorldToScreenPoint(new Vector3(worldSize.x, worldSize.y, worldSize.z));

        return new Vector2(
            Mathf.Abs(screenPoint2.x - screenPoint1.x),
            Mathf.Abs(screenPoint2.y - screenPoint1.y)
        );
    }

    /// <summary>
    /// 枠の点滅を開始する
    /// </summary>
    private void StartBlink()
    {
        if (frameCg == null) return;

        if (GameManager.Instance.audioSource != null && GameManager.Instance.choice != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.choice); // 移動音
        }

        frameCg.alpha = 0.5f;
        blinkTween = frameCg.DOFade(1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 枠の点滅を停止し、透明にする
    /// </summary>
    private void StopBlink()
    {
        if (blinkTween != null)
        {
            blinkTween.Kill();
            blinkTween = null;
        }

        if (frameCg != null)
        {
            frameCg.alpha = 0f;
        }
    }

    /// <summary>
    /// 入力処理の有効/無効を設定する
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        NormalinputEnabled = enabled;
        Debug.Log($"入力処理を {(enabled ? "有効" : "無効")} にしました");
    }

    /// <summary>
    /// 選択中のスキル説明表示
    /// </summary>
    private void Skilliconexplanation()
    {
        //選択中のスキル説明
        var skillQuantity = SelectUI.GetComponent<SkillQuantity>();
        if (skillQuantity != null)
        {
            skillDescriptionText.text = ($"{skillQuantity.D_Sk_StatusData.ItemDescription}\n\n" +
                $"{skillQuantity.D_Sk_StatusData.EfficacyItemDescription}");
        }
    }
    /// <summary>
    /// 選択中のアイテム説明表示
    /// </summary>
    private void Itemiconexplanation()
    {
        //選択中のアイテム説明
        var itemQuantity = SelectUI.GetComponent<ItemQuantity>();
        if (itemQuantity != null)
        {
            itemDescriptionText.text = ($"{itemQuantity.D_It_StatusData.ItemDescription}\n\n" +
                $"{itemQuantity.D_It_StatusData.EfficacyItemDescription}");
        }
    }

    // --- メニュー処理メソッド ---

    /// <summary>
    /// スキル選択処理
    /// </summary>
    private void SkillSelectionProcess()
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // スキルアイコンを生成
        GenerateSkillIcons();

        // SkillFieldを表示、ActionFieldを非表示
        if (skillField != null) skillField.SetActive(true);
        if (actionField != null) actionField.SetActive(false);

        // 選択対象を SkillSelection に切り替え
        uiElements = SkillSelection.ToArray();
        currentIndex = 0;
        if (uiElements.Length > 0)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
        }

        Skilliconexplanation();
        Debug.Log("UI 切替 → SkillSelection");
    }

    /// <summary>
    /// アイテム選択処理
    /// </summary>
    private void ItemSelectionProcess()
    {
        // 消耗品アイテムを生成
        GenerateItemIcons(new string[] { "Buff", "DeBuff", "Attack", "Magic", "HP_Recovery", "MP_Recovery" });

        //アイテムがないなら進まない
        if (ItemSelection.Count == 0)
        {
            // いつか「ぶぶー」効果音を再生
            Debug.Log("所持している消耗品アイテムがありません");
            return;
        }

        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // itemFieldを表示、ActionFieldを非表示
        if (itemField != null) itemField.SetActive(true);
        if (actionField != null) actionField.SetActive(false);

        // 選択対象を ItemSelection に切り替え
        uiElements = ItemSelection.ToArray();
        currentIndex = 0;
        if (uiElements.Length > 0)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
        }

        Itemiconexplanation();
        Debug.Log("UI 切替 → ItemSelection");
    }

    /// <summary>
    /// 味方交代処理
    /// </summary>
    private void Switching_sidesProcess()
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // キャラクターアイコンを生成
        GenerateAllyIcons();

        // AllyFieldを表示、ActionFieldを非表示
        if (allyField != null) allyField.SetActive(true);
        if (actionField != null) actionField.SetActive(false);

        // 選択対象を Switching_sides に切り替え
        uiElements = Switching_sides.ToArray();
        currentIndex = 0;
        if (uiElements.Length > 0)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
        }
        Debug.Log("UI 切替 → Switching_sides");
    }

    /// <summary>
    /// スキル使用処理
    /// </summary>
    private void SkillUseProcess()
    {
        // 使用するスキルの対象判別
        var targetSkill = SelectUI.GetComponent<SkillQuantity>()?.D_Sk_StatusData;
        if (targetSkill == null)
        {
            Debug.LogError("スキルデータが見つかりません！");
            return;
        }

        //クイック行動中クイック以外のスキルを発動できなくする
        if (QuickAction　== true && targetSkill.SeeKinds != D_Sk_StatusData.Kinds.Quick)
        {
            Debug.Log("クイック行動中はクイックスキル以外を発動できません");
            return;
        }

        //スキルを発動するキャラクターに疲労があればクイックスキルを発動できなくする。
        bool hasfatigue = currentCharacterData.ActiveBuffs.Any(buff => buff.baseData != null && buff.baseData.name == "Fatigue");

        if (hasfatigue && targetSkill.SeeKinds == D_Sk_StatusData.Kinds.Quick)
        {
            Debug.Log("疲労のデバフによりクイックスキルを発動できません。");
            return;
        }

        // 行動ゾーンに入っているアイコンにあるTimelineIconController.ActivatedSkillsにスキルデータを入れる
        TimelineIconController currentIcon = null;
        foreach (var icon in allyIcons)
        {
            if (icon.characterData == currentCharacterData)
            {
                currentIcon = icon;
                break;
            }
        }

        //念のため入れる前にリセット
        currentIcon.ActionReset();

        if (currentIcon != null)
        {
            currentIcon.ActivatedSkills = targetSkill;
        }
        else
        {
            Debug.LogError("現在のキャラクターのTimelineIconControllerが見つかりません！");
            return;
        }

        if (targetSkill.MpConsumption > currentIcon.characterData.Mp)
        {
            Debug.Log("MPが足りないのでスキルが発動できません");
            return;
        }

        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // 既存のArea_of_Effectリストをクリア（既存オブジェクトは削除しない）
        Area_of_Effect.Clear();

        // スキル範囲に基づいて対象を決定し、Area_of_Effectに追加
        switch (targetSkill.SeeSkillRange)
        {
            case D_Sk_StatusData.SkillRange.Himself://自身
                // スキル選択中のキャラクター（自身）を追加
                GenerateAreaOfEffectTargets(new List<GameObject> { currentIcon.characterObject });
                break;

            case D_Sk_StatusData.SkillRange.Single_ally://味方単体
                // character全て追加
                GenerateAreaOfEffectTargets(character);
                break;

            case D_Sk_StatusData.SkillRange.Alla_llies://味方全体
                // character全て追加し全てに枠を付ける、保留
                GenerateAreaOfEffectTargets(character);
                // TODO: 全体対象の場合は自動選択にするか、全員に枠を付ける処理を追加
                break;

            case D_Sk_StatusData.SkillRange.Single_enemy://敵単体
                // E_character全て追加
                GenerateAreaOfEffectTargets(E_character);
                break;

            case D_Sk_StatusData.SkillRange.All_enemies://敵全体
                // E_character全て追加し全てに枠を付ける、保留
                GenerateAreaOfEffectTargets(E_character);
                // TODO: 全体対象の場合は自動選択にするか、全員に枠を付ける処理を追加
                break;

            default:
                Debug.Log("未対応の Range: " + targetSkill.SeeSkillRange);
                return;
        }

        // スキルを非表示
        if (skillField != null) skillField.SetActive(false);

        // 選択対象を Area_of_Effect に切り替え
        if (Area_of_Effect.Count > 0)
        {
            uiElements = Area_of_Effect.ToArray();
            currentIndex = 0;
            SelectUI = uiElements[currentIndex];

            // ここで親を "Action Selection UI" に移動
            if (ActionSelectionUI != null)
            {
                Frame.transform.SetParent(ActionSelectionUI.transform, false);
            }
            else
            {
                Debug.LogError("ActionSelectionUI が設定されていません！");
            }

            MoveFrametoObject(SelectUI);
            StartBlink();
        }
        else
        {
            Debug.LogWarning("Area_of_Effect に対象がありません！");
        }

        Debug.Log($"スキル使用処理: {targetSkill.name}");
    }

    /// <summary>
    /// アイテム使用処理
    /// </summary>
    private void ItemUseProcess()
    {
        // 使用するアイテムの対象判別
        var targetItem = SelectUI.GetComponent<ItemQuantity>()?.D_It_StatusData;
        if (targetItem == null)
        {
            Debug.LogError("アイテムデータが見つかりません！");
            return;
        }

        //クイック行動中クイックスキル以外発動できない
        if (QuickAction)
        {
            Debug.Log("クイック行動中はクイックスキル以外を発動できません");
            return;
        }

        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // 行動ゾーンに入っているアイコンにあるTimelineIconController.ActivatedItemにアイテムデータを入れる
        TimelineIconController currentIcon = null;
        foreach (var icon in allyIcons)
        {
            if (icon.characterData == currentCharacterData)
            {
                currentIcon = icon;
                break;
            }
        }

        //念のため入れる前にリセット
        currentIcon.ActionReset();

        if (currentIcon != null)
        {
            currentIcon.ActivatedItem = targetItem;
            //numericalProcessing.ItemUse_ItData = targetItem;
        }
        else
        {
            Debug.LogError("現在のキャラクターのTimelineIconControllerが見つかりません！");
            return;
        }

        // 既存のArea_of_Effectリストをクリア（既存オブジェクトは削除しない）
        Area_of_Effect.Clear();

        // アイテム範囲に基づいて対象を決定し、Area_of_Effectに追加
        switch (targetItem.SeeItemRange)
        {
            case D_It_StatusData.ItemRange.Himself://自身
                // アイテム選択中のキャラクター（自身）を追加
                GenerateAreaOfEffectTargets(new List<GameObject> { currentIcon.characterObject });
                break;

            case D_It_StatusData.ItemRange.Single_ally://味方単体
                // character全て追加
                GenerateAreaOfEffectTargets(character);
                break;

            case D_It_StatusData.ItemRange.Alla_llies://味方全体
                // character全て追加し全てに枠を付ける、保留
                GenerateAreaOfEffectTargets(character);
                // TODO: 全体対象の場合は自動選択にするか、全員に枠を付ける処理を追加
                break;

            case D_It_StatusData.ItemRange.Single_enemy://敵単体
                // E_character全て追加
                GenerateAreaOfEffectTargets(E_character);
                break;

            case D_It_StatusData.ItemRange.All_enemies://敵全体
                // E_character全て追加し全てに枠を付ける、保留
                GenerateAreaOfEffectTargets(E_character);
                // TODO: 全体対象の場合は自動選択にするか、全員に枠を付ける処理を追加
                break;

            default:
                Debug.Log("未対応の Range: " + targetItem.SeeItemRange);
                return;
        }

        // アイテムを非表示
        if (itemField != null) itemField.SetActive(false);

        // 選択対象を Area_of_Effect に切り替え
        if (Area_of_Effect.Count > 0)
        {
            uiElements = Area_of_Effect.ToArray();
            currentIndex = 0;
            SelectUI = uiElements[currentIndex];

            // ここで親を "Action Selection UI" に移動
            if (ActionSelectionUI != null)
            {
                Frame.transform.SetParent(ActionSelectionUI.transform, false);
            }
            else
            {
                Debug.LogError("ActionSelectionUI が設定されていません！");
            }

            MoveFrametoObject(SelectUI);
            StartBlink();
        }
        else
        {
            Debug.LogWarning("Area_of_Effect に対象がありません！");
        }

        Debug.Log($"アイテム使用処理: {targetItem.name}");
    }

    /// <summary>
    /// クイック行動対象決定
    /// </summary>
    private void QuickActionProcess()
    {
        string rawName = SelectUI.name;
        string targetName = rawName;
        // 先頭の "B_" を除去
        if (targetName.StartsWith("B_"))
            targetName = targetName.Substring(2);

        // 後ろの "(Clone)" を除去
        //targetName = targetName.Replace("(Clone)", "");

        Debug.Log(targetName + "のクイック行動をします。");

        // --- AllyParticipationList / EnemyParticipationList の中から一致する名前を探す ---

        // まず味方
        foreach (var Ic_data in allyIcons)
        {
            if (Ic_data.name == $"Ic_{targetName}")
            {
                QuickActionDecision(Ic_data);
                break;
            }
        }
    }

    /// <summary>
    /// キャラクター交代処理
    /// </summary>
    private void CharacterSwitchingProcess()
    {
        // TODO: キャラクター交代処理を実装
        Debug.Log("キャラクター交代処理");
    }

    /// <summary>
    /// 行動対象決定
    /// </summary>
    private void Targetdetermination()
    {
        // 行動ゾーンに入っているアイコンにあるTimelineIconControllerを取得
        TimelineIconController currentIcon = null;
        foreach (var icon in allyIcons)
        {
            if (icon.characterData == currentCharacterData)
            {
                currentIcon = icon;
                break;
            }
        }

        if (currentIcon == null)
        {
            Debug.LogError("現在行動中のキャラクターの TimelineIconController が見つかりません！");
            return;
        }

        // --- SelectUI の名前からターゲット名を抽出 ---
        // 例: "B_Taro(Clone)" → "Taro"
        string rawName = SelectUI.name;
        string targetName = rawName;

        // 先頭の "B_" を除去
        if (targetName.StartsWith("B_"))
            targetName = targetName.Substring(2);

        // 後ろの "(Clone)" を除去
        targetName = targetName.Replace("(Clone)", "");

        // --- AllyParticipationList / EnemyParticipationList の中から一致する名前を探す ---
        D_Ch_StatusData foundTarget = null;

        // まず味方
        foreach (var data in AllyParticipationList)
        {
            if (data.name == targetName)
            {
                foundTarget = data;
                break;
            }
        }

        // 見つからなければ敵
        if (foundTarget == null)
        {
            foreach (var data in EnemyParticipationList)
            {
                if (data.name == targetName)
                {
                    foundTarget = data;
                    break;
                }
            }
        }

        // --- 見つかったらセット ---
        if (foundTarget != null)
        {
            currentIcon.Target_of_Action = foundTarget;
            Debug.Log($"ターゲット決定 → {foundTarget.name}");
        }
        else
        {
            Debug.LogError($"ターゲット名 '{targetName}' に一致するデータが見つかりませんでした。");
            return;
        }

        // --- タイムライン上の全アイコンを動かす ---
        Moveallicons();

        currentIcon.state = TimelineIconController.TimelineState.Acting_up;//行動ゾーンにいる

        marker.Hide();//行動UIを閉じる

        QuickAction = false;

        Debug.Log("行動対象選択処理");
    }

    /// <summary>
    /// 逃げる処理
    /// </summary>
    private void RunAwayProcess()
    {
        // TODO: 逃げる処理を実装
        Debug.Log("逃げる処理");
    }


    /// <summary>
    /// クイック行動開始
    /// 対象選択
    /// </summary>
    private void QuickActionSelection()
    {
        //クイック行動開始
        QuickAction = true;
        Debug.Log("クイック行動開始");

        // UI操作を有効化
        SetInputEnabled(true);

        // --- タイムライン上の全アイコンを停止 ---
        Stopallicons();

        // 履歴をクリア
        menuHistory.Clear();

        if (marker != null)
        {
            marker.Show();  //ここで表示
        }
        // 枠が見えるように親を "Action Selection UI" に移動
        if (ActionSelectionUI != null)
        {
            Frame.transform.SetParent(ActionSelectionUI.transform, false);
        }

        // ActionFieldを非表示
        if (actionField != null)
        {
            actionField.SetActive(false);
        }

        //クイック行動をする味方を選択
        GenerateAreaOfEffectTargets(character);

        // 選択対象を Area_of_Effect に切り替え
        uiElements = Area_of_Effect.ToArray();
        currentIndex = 0;
        SelectUI = uiElements[currentIndex];

        // フレームを移動して点滅を開始
        if (SelectUI != null)
        {
            MoveFrametoObject(SelectUI);
            StartBlink();
        }
    }

    /// <summary>
    /// クイック行動対象決定
    /// </summary>
    private void QuickActionDecision(TimelineIconController statuscheck)
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        //対象が行動ゾーン内にいたらクイック行動はできない
        if (statuscheck.state == TimelineIconController.TimelineState.Interrupted)
        {
            return;
        }

        // 現在選択中のキャラクターデータを保存
        currentCharacterData = statuscheck.characterData;

        if (marker != null)
        {
            marker._target = statuscheck.characterObject.transform;
            marker.Show();  //ここで表示
            marker.OnUpdatePosition();
        }

        // ActionFieldを表示
        if (actionField != null)
        {
            actionField.SetActive(true);
        }

        // 選択対象を ActionSelection に切り替え
        uiElements = ActionSelection.ToArray();
        currentIndex = 0;
        SelectUI = uiElements[currentIndex];

        // フレームを移動して点滅を開始
        if (SelectUI != null)
        {
            MoveFrameTo(SelectUI);
            StartBlink();
        }

        Debug.Log($"クイック行動対象決定: {currentCharacterData.name}");
    }

    ///<summary>
    ///タイムライン上の全アイコンを停止
    ///</summary>
    public void Stopallicons()
    {
        foreach (var ally in allyIcons)
        {
            if (ally.state == TimelineIconController.TimelineState.Acting_up)
                ally.state = TimelineIconController.TimelineState.Interrupted;
            else
                ally.state = TimelineIconController.TimelineState.WaitingForCommand;
        }

        foreach (var enemy in enemyIcons)
        {
            if (enemy.state == TimelineIconController.TimelineState.Acting_up)
                enemy.state = TimelineIconController.TimelineState.Interrupted;
            else
                enemy.state = TimelineIconController.TimelineState.WaitingForCommand;
        }
    }

    ///<summary>
    ///タイムライン上の全アイコンを動かす
    ///</summary>
    public void Moveallicons()
    {
        foreach (var ally in allyIcons)
        {
            if (ally.state == TimelineIconController.TimelineState.Interrupted)
                ally.state = TimelineIconController.TimelineState.Acting_up;
            else
                ally.state = TimelineIconController.TimelineState.Moving;
        }

        foreach (var enemy in enemyIcons)
        {
            if (enemy.state == TimelineIconController.TimelineState.Interrupted)
                enemy.state = TimelineIconController.TimelineState.Acting_up;
            else
                enemy.state = TimelineIconController.TimelineState.Moving;
        }
    }



    // --- UI生成メソッド ---

    /// <summary>
    /// スキルアイコンを生成（Unlockが trueのスキルのB_Iconを生成）
    /// </summary>
    private void GenerateSkillIcons()
    {
        if (currentCharacterData == null)
        {
            Debug.LogError("キャラクターデータが設定されていません！");
            return;
        }

        if (currentCharacterData.SkillList == null)
        {
            Debug.LogError("スキルデータベース（SkillList）が設定されていません！");
            return;
        }

        if (skillField == null)
        {
            Debug.LogError("SkillField が見つかりません！");
            return;
        }

        // 既存のスキル選択をクリア
        SkillSelection.Clear();

        // 既存のスキルオブジェクトを削除
        foreach (Transform child in skillField.transform)
        {
            if (child.name == "SkillDescription")
                continue;
            Destroy(child.gameObject);
        }

        // SkillListはスキルデータベース（Db_Sk_StatusDataBase）なので、そのItemListからUnlockが trueのスキルを取得
        if (currentCharacterData.SkillList.ItemList == null)
        {
            Debug.LogError("スキルデータベースのItemListが nullです！");
            return;
        }

        // Unlockが trueのスキルを取得
        var unlockedSkills = currentCharacterData.SkillList.ItemList
            .Where(skill => skill != null && skill.Unlock)
            .OrderBy(skill => skill.Id)
            .ToList();

        // 位置設定（一列配置、最大10個）
        Vector2[] positions = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float x = 0f;
            float y = 450f - (i * 95f);
            positions[i] = new Vector2(x, y);
        }

        // スキルを生成
        int skillCount = Mathf.Min(unlockedSkills.Count, 10);
        for (int i = 0; i < skillCount; i++)
        {
            var skillData = unlockedSkills[i];

            if (skillData.B_Icon == null)
            {
                Debug.LogWarning($"Skill {i} の B_Icon が設定されていません");
                continue;
            }

            // スキルアイコンをSkillFieldの子として生成
            GameObject skillInstance = Instantiate(skillData.B_Icon, skillField.transform);

            // 位置を設定
            RectTransform skillRect = skillInstance.GetComponent<RectTransform>();
            if (skillRect != null)
            {
                skillRect.anchoredPosition = positions[i];
            }

            // B_SelectionSettings コンポーネントを追加
            var selectionSettings = skillInstance.GetComponent<B_SelectionSettings>();
            if (selectionSettings == null)
            {
                selectionSettings = skillInstance.AddComponent<B_SelectionSettings>();
            }
            selectionSettings.choose = B_SelectionSettings.Choose.Skill_Use;

            // SkillSelection リストに追加
            SkillSelection.Add(skillInstance);

            Debug.Log($"Skill Icon {i} を生成しました: {skillData.name} at {positions[i]}");
        }

        Debug.Log($"合計 {SkillSelection.Count} 個のスキル選択オブジェクトを生成しました");
    }

    /// <summary>
    /// アイテムアイコンを生成（消耗品アイテム）
    /// </summary>
    private void GenerateItemIcons(string[] itemKinds)
    {
        if (db_PlayerItem == null)
        {
            Debug.LogError("Player_Item データベースが設定されていません！");
            return;
        }

        if (itemField == null)
        {
            Debug.LogError("itemField が見つかりません！");
            return;
        }

        // 既存のアイテム選択をクリア
        ItemSelection.Clear();

        // 既存のアイテムオブジェクトを削除
        foreach (Transform child in itemField.transform)
        {
            if (child.name == "ItemDescription")
                continue;
            Destroy(child.gameObject);
        }

        // 条件に合うアイテムをID順で取得（最大20個）
        var filteredItems = new List<D_It_StatusData>();
        foreach (var item in db_PlayerItem.ItemList)
        {
            bool isMatch = false;
            foreach (string kindString in itemKinds)
            {
                if (System.Enum.TryParse<D_It_StatusData.Kinds>(kindString, out D_It_StatusData.Kinds kindEnum))
                {
                    if (item.SeeKinds == kindEnum)
                    {
                        isMatch = true;
                        break;
                    }
                }
            }

            if (isMatch)
            {
                filteredItems.Add(item);
            }
        }

        // ID順でソートして最大20個まで
        filteredItems = filteredItems
            .OrderBy(item => item.Id)
            .Take(20)
            .ToList();

        // 位置設定（一列配置、最大10個）
        Vector2[] positions = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float x = 0f;
            float y = 450f - (i * 95f);
            positions[i] = new Vector2(x, y);
        }

        // アイテムを生成
        int itemCount = filteredItems.Count;
        for (int i = 0; i < itemCount; i++)
        {
            var itemData = filteredItems[i];

            if (itemData.Icon == null)
            {
                Debug.LogWarning($"Item {i} の Icon が設定されていません");
                continue;
            }

            // アイテムアイコンを itemFieldの子として生成
            GameObject itemInstance = Instantiate(itemData.Icon, itemField.transform);

            // 位置を設定
            RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchoredPosition = positions[i];
            }

            // B_SelectionSettings コンポーネントを追加
            var selectionSettings = itemInstance.GetComponent<B_SelectionSettings>();
            if (selectionSettings == null)
            {
                selectionSettings = itemInstance.AddComponent<B_SelectionSettings>();
            }
            selectionSettings.choose = B_SelectionSettings.Choose.Item_Use;

            // ItemSelection リストに追加
            ItemSelection.Add(itemInstance);

            Debug.Log($"Item Icon {i} を生成しました: {itemData.name} at {positions[i]}");
        }

        Debug.Log($"合計 {ItemSelection.Count} 個のアイテム選択オブジェクトを生成しました");
    }

    /// <summary>
    /// 味方キャラクターアイコンを生成（Ally Listの全てのデータのIcon_Character2を生成）
    /// </summary>
    private void GenerateAllyIcons()
    {
        if (dss_Ch_StatusDataStores == null)
        {
            Debug.LogError("Character Data Stores が設定されていません！");
            return;
        }

        // FindDatabaseWithName を使用して Ally List データベースを取得
        var allyListDatabase = dss_Ch_StatusDataStores.FindDatabaseWithName("Ally List");
        if (allyListDatabase == null)
        {
            Debug.LogError("Ally List データベースが見つかりません！");
            return;
        }

        // Ally List からキャラクターデータを取得
        var allyList = allyListDatabase.ItemList;
        if (allyList == null || allyList.Count == 0)
        {
            Debug.LogWarning("Ally List にデータがありません");
            return;
        }

        if (allyField == null)
        {
            Debug.LogError("AllyField が見つかりません！");
            return;
        }

        // 既存の味方交代選択をクリア
        Switching_sides.Clear();

        // 既存のキャラクターオブジェクトを削除
        foreach (Transform child in allyField.transform)
        {
            Destroy(child.gameObject);
        }

        // 位置設定（4個まで）
        Vector2[] positions = new Vector2[]
        {
            new Vector2(0, 380),
            new Vector2(0, 120),
            new Vector2(0, -140),
            new Vector2(0, -400)
        };

        // 最大4個まで生成
        int maxIcons = Mathf.Min(allyList.Count, 4);

        for (int i = 0; i < maxIcons; i++)
        {
            var characterData = allyList[i];

            if (characterData.Icon_Character2 == null)
            {
                Debug.LogWarning($"Character {i} の Icon_Character2 が設定されていません");
                continue;
            }

            // アイコンをAllyFieldの子として生成
            GameObject iconInstance = Instantiate(characterData.Icon_Character2, allyField.transform);

            // 位置を設定
            RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = positions[i];
            }

            // 念のためなければ B_SelectionSettings コンポーネントを追加
            var selectionSettings = iconInstance.GetComponent<B_SelectionSettings>();
            if (selectionSettings == null)
            {
                selectionSettings = iconInstance.AddComponent<B_SelectionSettings>();
            }
            selectionSettings.choose = B_SelectionSettings.Choose.Character_Switching;

            //ステータスの設定
            var StatusUpdates = iconInstance.GetComponent<CharacterIconStatus>();
            if (StatusUpdates != null)
            {
                StatusUpdates.StatusUpdateMini();
            }

            // Switching_sides リストに追加
            Switching_sides.Add(iconInstance);

            Debug.Log($"Ally Icon {i} を生成しました: {characterData.name} at {positions[i]}");
        }

        Debug.Log($"合計 {Switching_sides.Count} 個の味方交代選択オブジェクトを生成しました");
    }

    /// <summary>
    /// 効果範囲対象をArea_of_Effectに追加（characterまたはE_characterから既存オブジェクトを追加）
    /// </summary>
    private void GenerateAreaOfEffectTargets(List<GameObject> targetCharacters)
    {
        if (targetCharacters == null || targetCharacters.Count == 0)
        {
            Debug.LogWarning("対象キャラクターがありません");
            return;
        }

        // 既存のcharacter、E_characterオブジェクトをArea_of_Effectに追加
        foreach (var characterObj in targetCharacters)
        {
            if (characterObj == null)
            {
                Debug.Log("何も送られて来ませんでした");
                continue;
            }

            // 既にArea_of_Effectに含まれている場合はスキップ
            if (Area_of_Effect.Contains(characterObj))
            {
                continue;
            }

            // B_SelectionSettings コンポーネントを追加（なければ）
            var selectionSettings = characterObj.GetComponent<B_SelectionSettings>();
            if (selectionSettings == null)
            {
                selectionSettings = characterObj.AddComponent<B_SelectionSettings>();
            }

            //一つ前の履歴チェック
            bool prevHistory = false;

            if (menuHistory.Count >= 2)
            {
                //前の履歴がある
                prevHistory = true;
            }

            //クイック行動中ならそのキャラクターの行動選択に移行できるようにする
            if (QuickAction && prevHistory == false)
            {
                //クイック行動対象
                selectionSettings.choose = B_SelectionSettings.Choose.Quick_Action;
            }
            else
            {
                //アイテム、スキル発動対象
                selectionSettings.choose = B_SelectionSettings.Choose.AreaofEffect;
            }
            //対象選択で履歴がないのはクイック行動のみなので行動対象か発動対象か判別できる
            //これがないとクイック行動中、発動対象が選択できない

            // Area_of_Effect リストに追加
            Area_of_Effect.Add(characterObj);

            Debug.Log($"Area of Effect Target を追加しました: {characterObj.name}");
        }

        Debug.Log($"合計 {Area_of_Effect.Count} 個の効果範囲対象オブジェクトを追加しました");
    }

    /// <summary>
    /// HPが0になったキャラクターのオブジェクトとアイコンを削除する
    /// </summary>
    /// <param name="characterData">削除するキャラクターのデータ</param>
    public void RemoveCharacterOnDeath(D_Ch_StatusData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("削除対象のキャラクターデータが null です");
            return;
        }

        // 味方か敵かを判定
        bool isAlly = AllyParticipationList.Contains(characterData);
        bool isEnemy = EnemyParticipationList.Contains(characterData);

        if (!isAlly && !isEnemy)
        {
            Debug.LogWarning($"キャラクター {characterData.name} が参戦リストに見つかりません");
            return;
        }

        // TimelineIconControllerからcharacterObjectを取得（最も確実な方法）
        TimelineIconController targetIcon = null;
        GameObject characterObject = null;
        
        if (isAlly)
        {
            foreach (var icon in allyIcons)
            {
                if (icon != null && icon.characterData == characterData)
                {
                    targetIcon = icon;
                    if (icon.characterObject != null)
                    {
                        characterObject = icon.characterObject;
                    }
                    break;
                }
            }
        }
        else
        {
            foreach (var icon in enemyIcons)
            {
                if (icon != null && icon.characterData == characterData)
                {
                    targetIcon = icon;
                    if (icon.characterObject != null)
                    {
                        characterObject = icon.characterObject;
                    }
                    break;
                }
            }
        }

        // TimelineIconControllerから取得できなかった場合、リストから直接探す
        if (characterObject == null)
        {
            if (isAlly)
            {
                foreach (var ally in spawnedAllies)
                {
                    if (ally != null)
                    {
                        // オブジェクト名からキャラクターデータを特定
                        string objectName = ally.name.Replace("(Clone)", "").Replace("B_", "");
                        if (characterData.name == objectName || ally.name.Contains(characterData.name))
                        {
                            characterObject = ally;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var enemy in spawnedEnemies)
                {
                    if (enemy != null)
                    {
                        string objectName = enemy.name.Replace("(Clone)", "").Replace("B_", "");
                        if (characterData.name == objectName || enemy.name.Contains(characterData.name))
                        {
                            characterObject = enemy;
                            break;
                        }
                    }
                }
            }
        }

        // キャラクターオブジェクトを削除
        if (characterObject != null)
        {
            // リストから削除
            if (isAlly)
            {
                spawnedAllies.Remove(characterObject);
                character.Remove(characterObject);
            }
            else
            {
                spawnedEnemies.Remove(characterObject);
                E_character.Remove(characterObject);
            }

            // オブジェクトを削除
            Destroy(characterObject);
            Debug.Log($"キャラクターオブジェクト {characterObject.name} を削除しました");
        }

        // タイムラインアイコンを削除
        if (targetIcon != null)
        {
            if (isAlly)
            {
                allyIcons.Remove(targetIcon);
            }
            else
            {
                enemyIcons.Remove(targetIcon);
            }

            // アイコンオブジェクトを削除
            if (targetIcon.gameObject != null)
            {
                Destroy(targetIcon.gameObject);
                Debug.Log($"タイムラインアイコン {targetIcon.gameObject.name} を削除しました");
            }
        }

        // ステータスアイコンを削除（StatusIconsの子オブジェクトから）
        /*if (StatusIcons != null)
        {
            foreach (Transform child in StatusIcons.transform)
            {
                var iconStatus = child.GetComponent<CharacterIconStatus>();
                if (iconStatus != null && iconStatus.status == characterData)
                {
                    Destroy(child.gameObject);
                    Debug.Log($"ステータスアイコン {child.name} を削除しました");
                    break;
                }
            }
        }*/

        // Area_of_Effectからも削除（該当する場合）
        if (characterObject != null)
        {
            Area_of_Effect.Remove(characterObject);
        }

        Debug.Log($"キャラクター {characterData.name} の削除処理が完了しました");
    }
}
