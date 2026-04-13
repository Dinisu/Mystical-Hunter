using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System.Linq;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using TMPro;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class InventManager : MonoBehaviour
{
    // --- 外部参照（ステータス数値処理） ---
    public NumericalProcessing numericalProcessing;

    [SerializeField]// --- UIオブジェクト（戦闘UIパネル） ---
    private GameObject statusMenu;//自身
    [SerializeField]
    private TextMeshProUGUI MoneyText;


    [SerializeField, Header("メニュー選択")]
    public List<GameObject> MenuField = new List<GameObject>();
    [SerializeField, Header("キャラクターデータ")]
    public List<GameObject> CharacterStatus = new List<GameObject>();

    [SerializeField, Header("ステータスメニュー")]
    public List<GameObject> StatusMenu = new List<GameObject>();
    [SerializeField, Header("アイテムメニュー")]
    public List<GameObject> ItemMenu = new List<GameObject>();
    [SerializeField, Header("スキルメニュー")]
    public List<GameObject> SkillMenu = new List<GameObject>();
    [SerializeField, Header("アイテム選択")]
    public List<GameObject> ItemChoice = new List<GameObject>();
    [SerializeField, Header("キャラクター使用アイテム")]
    public List<GameObject> Charactersuseitems = new List<GameObject>();
    [SerializeField, Header("装備選択")]
    public List<GameObject> StatusEquipment = new List<GameObject>();




    //ステータスメニューオブジェクト
    private GameObject statusField;
    private GameObject selectionField;
    private GameObject itemField;
    private GameObject skillField;
    private GameObject itemChoiceField;
    private GameObject itemDescription;
    private GameObject EquipmentChoiceField;

    //アイテム説明テキスト
    private TextMeshProUGUI descriptionText;

    //キャラクターデータストア
    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;
    private Dss_It_StatusDataStores dss_It_StatusDataStores;

    private D_Ch_StatusData d_ch_Status;
    private Db_It_StatusDataBase db_PlayerItem;//プレイヤーの所持アイテムデータ
    private CharacterIconStatus characterIconStatus;

    private StatusNumber d_Character;//選択中のキャラクターステータス表示スクリプト

    [SerializeField, Header("現在選択中のUI")]
    public GameObject SelectUI;

    //private int beforecurrentIndex;

    // 履歴管理システム
    private List<MenuHistory> menuHistory = new List<MenuHistory>();

    [System.Serializable]
    private class MenuHistory
    {
        public GameObject[] elements;//その時に操作可能だった UI 要素の配列
        public int index;//選択されていた要素の位置
        public string menuType; //メニューの種類　例:"MenuField", "CharacterStatus", "StatusMenu"

        public MenuHistory(GameObject[] elements, int index, string menuType)
        {
            this.elements = elements;
            this.index = index;
            this.menuType = menuType;
        }
    }

    private GameObject[] uiElements; // UI格納配列（切り替え用）
    private int currentIndex = 0;    // 選択中インデックス

    public GameObject Frame;                 // 枠：点滅対象

    private Tween blinkTween;
    private CanvasGroup frameCg;

    private PlayerInput playerInput;
    private bool inputEnabled = true; // 入力処理の有効/無効フラグ

    private void Awake()
    {
        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();
        dss_It_StatusDataStores = FindObjectOfType<Dss_It_StatusDataStores>();
        // 数値処理スクリプト取得
        numericalProcessing = GameObject.Find("Numerical Processing").GetComponent<NumericalProcessing>();
        // 数値処理スクリプトに自身を送る
        numericalProcessing.InventManager = this;

        // FindDatabaseWithName を使用して Player_Item データベースを取得
        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Player_Item");

        // メニュー非表示時は操作不可にしておく
        inputEnabled = statusMenu != null && statusMenu.activeSelf;
    }
    void Start()
    {
        // 数値処理スクリプト取得
        numericalProcessing = GameObject.Find("Numerical Processing").GetComponent<NumericalProcessing>();

        // Frame に CanvasGroup がなければ追加
        frameCg = Frame.GetComponent<CanvasGroup>();
        if (frameCg == null) frameCg = Frame.AddComponent<CanvasGroup>();
        frameCg.alpha = 0f; // 初期は非選択状態（透明）

        // UI要素リスト設定
        // SelectionField の中身を配列にコピー
        uiElements = MenuField.ToArray();

        //所持金表示
        MoneyText.GetComponent<CharacterIconStatus>()?.DisplayOfMoneyHeld();

        // 最初のUIを選択状態に設定
        if (uiElements.Length > 0)
        {
            currentIndex = 0;
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI); // 枠を移動
            StartBlink();          // 枠を点滅開始
        }

        // PlayerInput が別のオブジェクトについている場合はこちらで探す
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput がシーン内に見つかりません！");
            }
        }

        // 子オブジェクトの中から "Status Field" を探す
        statusField = FindObjectOrError(transform, "Status Field");
        if (statusField == null) return;

        selectionField = FindObjectOrError(transform, "Selection Field");
        if (selectionField == null) return;

        itemField = FindObjectOrError(transform, "Item Field");
        if (itemField == null) return;

        skillField = FindObjectOrError(transform, "Skill Field");
        if (skillField == null) return;

        itemChoiceField = FindObjectOrError(itemField.transform, "ItemChoiceField");
        if (itemChoiceField == null) return;

        itemDescription = FindObjectOrError(itemField.transform, "ItemDescription");
        if (itemDescription == null) return;

        EquipmentChoiceField = FindObjectOrError(statusField.transform, "EquipmentChoiceField");
        if (EquipmentChoiceField == null) return;

        descriptionText = FindComponentOrError<TextMeshProUGUI>(itemDescription.transform,"DescriptionText");
        if (descriptionText == null) return;

        // 最初は非表示にしておく
        statusField.SetActive(false);
        itemField.SetActive(false);

        //キャラクターアイコンを取得＆生成
        GenerateCharacterIcons();

        // プレイヤー入力のアクションを取得
        var move = playerInput.actions["Move"];
        move.performed += OnMove;   // 入力が行われた瞬間に呼ばれる
        move.canceled += OnMove;   // 入力が離された瞬間に呼ばれる

        var attack = playerInput.actions["Attack"];
        attack.performed += OnAttack;

        var cancel = playerInput.actions["Cancel"];
        cancel.performed += OnCancel;
    }

    private GameObject FindObjectOrError(Transform parent, string objectName)
    {
        GameObject obj = parent.Find(objectName)?.gameObject;

        if (obj == null)
        {
            Debug.LogError($"{objectName} が見つかりません！");
        }

        return obj;
    }

    private T FindComponentOrError<T>(Transform parent, string objectName) where T : Component
    {
        T comp = parent.Find(objectName)?.GetComponent<T>();

        if (comp == null)
        {
            Debug.LogError($"{objectName} が見つかりません！");
        }

        return comp;
    }

    // --- 入力処理 (Input System) ---
    /// <summary>
    /// Move入力（上下左右キー / スティック）を受け取る
    /// StatusMenuなどの時のみ二列レイアウト対応、他は一列移動
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        // メニューが非表示・操作不可なら無視
        if (!inputEnabled || !IsMenuActive()) return; // 入力が無効の場合は処理しない

        Debug.Log($"OnMove 呼ばれた！ phase={context.phase} value={context.ReadValue<Vector2>()}");
        if (!context.performed) return;

        Vector2 input = context.ReadValue<Vector2>();
        string currentMenuType = GetCurrentMenuType();

        if (GameManager.Instance.audioSource != null && GameManager.Instance.choice != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.choice); // 選択音
        }

        // 二列対応の移動をするか
        if (currentMenuType == "StatusMenu" || currentMenuType == "ItemChoice")
        {
            // 縦方向の移動（二列対応）
            if (input.y > 0.5f) // 上入力
            {
                MoveSelectionVerticalTwoColumn(-1);
            }
            else if (input.y < -0.5f) // 下入力
            {
                MoveSelectionVerticalTwoColumn(1);
            }

            // 横方向の移動
            if (input.x > 0.5f) // 右入力
            {
                MoveSelectionHorizontalTwoColumn(1);
            }
            else if (input.x < -0.5f) // 左入力
            {
                MoveSelectionHorizontalTwoColumn(-1);
            }
        }
        else if (currentMenuType == "StatusMenu") 
        {
            // スキルメニューは五列移動
            // 縦方向の移動（五列対応）
            if (input.y > 0.5f) // 上入力
            {
                MoveSelectionVerticalFiveColumn(-1);
            }
            else if (input.y < -0.5f) // 下入力
            {
                MoveSelectionVerticalFiveColumn(1);
            }

            // 横方向の移動
            if (input.x > 0.5f) // 右入力
            {
                MoveSelectionHorizontalFiveColumn(-1);
            }
            else if (input.x < -0.5f) // 左入力
            {
                MoveSelectionHorizontalFiveColumn(1);
            }
        }
        else 
        {
            // その他のメニューは従来の一列移動
            if (input.y > 0.5f) // 上入力
            {
                ChangeSelectionVertical(-1);
            }
            else if (input.y < -0.5f) // 下入力
            {
                ChangeSelectionVertical(1);
            }
        }

        //アイテム生成
        if (currentMenuType == "ItemMenu")
        {
            // 既存のアイテムオブジェクトを削除
            foreach (Transform child in itemChoiceField.transform)
            {
                if (child.name != "Selection Toggle") // Selection Toggleは残す
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            Itemicondisplay();
        }

        //アイテム説明
        if (currentMenuType == "ItemChoice")
        {
            Itemiconexplanation();
        }
        else
        {
            descriptionText.text = ("");
        }
    }

    private void Itemiconexplanation()
    {
        //選択中のアイテム説明
        var itemQuantity = SelectUI.GetComponent<ItemQuantity>();
        if (itemQuantity != null)
        {
            descriptionText.text = ($"{itemQuantity.D_It_StatusData.ItemDescription}\n\n" +
                $"{itemQuantity.D_It_StatusData.EfficacyItemDescription}");
        }
    }

    private void Itemicondisplay()//アイテムアイコン表示
    {
        // 既存のアイテムオブジェクトを削除念のため2回目
        foreach (Transform child in itemChoiceField.transform)
        {
            if (child.name != "Selection Toggle") // Selection Toggleは残す
            {
                DestroyImmediate(child.gameObject);
            }
        }
        // 選択中UIにアタッチされた SelectionSettings を取得
        var settings = SelectUI.GetComponent<SelectionSettings>();
        if (settings != null)
        {
            // enum Choose を確認
            switch (settings.choose)
            {
                case SelectionSettings.Choose.Consumables://消耗品アイテムを生成
                    GenerateItemIcons(new string[] { "Buff", "DeBuff", "Attack", "Magic", "HP_Recovery", "MP_Recovery" });
                    break;
                case SelectionSettings.Choose.Equipment://装備品アイテムを生成
                    GenerateItemIcons(new string[] { "Weapon", "Armor", "Accessories" });
                    break;
                case SelectionSettings.Choose.Valuables://貴重品アイテムを生成
                    GenerateItemIcons(new string[] { "Valuables" });
                    break;

                default:
                    Debug.Log("未対応の Choose: " + settings.choose);
                    break;
            }
        }
        else
        {
            Debug.LogWarning("SelectionSettings がアタッチされていません → " + SelectUI.name);
        }
    }
    /// <summary>
    /// Attack入力（決定ボタン）を受け取る
    /// 現在選択中のUIに対して処理を実行
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        // メニューが非表示・操作不可なら無視
        if (!inputEnabled || !IsMenuActive()) return; // 入力が無効の場合は処理しない
        if (!context.performed) return;

        if (SelectUI != null)
        {
            Debug.Log("Attack pressed → " + SelectUI.name);

            if (GameManager.Instance.audioSource != null && GameManager.Instance.decision != null)
            {
                GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.decision); // 決定音
            }

            // 選択中UIにアタッチされた SelectionSettings を取得
            var settings = SelectUI.GetComponent<SelectionSettings>();
            if (settings != null)
            {
                // enum Choose を確認
                switch (settings.choose)
                {
                    case SelectionSettings.Choose.StatusIcon://キャラクターアイコン選択
                        StatusIconprocess();
                        break;
                    case SelectionSettings.Choose.StatusMenu://ステータスメニュー表示
                        StatusMenuprocess();
                        break;
                    case SelectionSettings.Choose.ItemMenu://アイテムメニュー表示
                        ItemMenuprocess();
                        break;
                    case SelectionSettings.Choose.SkillMenu://スキルメニュー表示
                        SkillMenuprocess();
                        break;
                    case SelectionSettings.Choose.Consumables://消耗品選択
                        Consumablesprocess();
                        break;
                    case SelectionSettings.Choose.Equipment://装備品選択
                        Equipmentprocess();
                        break;
                    case SelectionSettings.Choose.Valuables://貴重品選択
                        Valuablesprocess();
                        break;
                    case SelectionSettings.Choose.ItemUsechoice://アイテム使用選択
                        ItemUsechoiceprocess();
                        break;
                    case SelectionSettings.Choose.ItemUse://アイテム使用
                        ItemUseprocess();
                        break;
                    case SelectionSettings.Choose.WeaponChoice://武器選択
                        Generateequipment(new string[] { "Weapon" });
                        break;
                    case SelectionSettings.Choose.ArmorChoice://防具選択
                        Generateequipment(new string[] { "Armor" });
                        break;
                    case SelectionSettings.Choose.Accessories1Choice://アクセサリー1選択
                        Generateequipment(new string[] { "Accessories" });
                        break;
                    case SelectionSettings.Choose.Accessories2Choice://アクセサリー2選択
                        Generateequipment(new string[] { "Accessories" });
                        break;

                    default:
                        Debug.Log("未対応の Choose: " + settings.choose);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("SelectionSettings がアタッチされていません → " + SelectUI.name);
            }
        }
    }

    /// <summary>
    /// Cancel入力（キャンセルボタン）を受け取る
    /// 一つ前の履歴に戻る処理
    /// </summary>
    public void OnCancel(InputAction.CallbackContext context)
    {
        // メニューが非表示・操作不可なら無視
        if (!inputEnabled || !IsMenuActive()) return; // 入力が無効の場合は処理しない
        
        Debug.Log($"OnCancel called. phase={context.phase} control={context.control?.path} value={context.ReadValueAsObject()}");
        if (!context.performed) return;

        Debug.Log("Cancel pressed");

        if (GameManager.Instance.audioSource != null && GameManager.Instance.cancel != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.cancel); // キャンセル音
        }

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
        Debug.Log($"履歴に保存: {menuType} (index: {currentIndex})");
    }

    /// <summary>
    /// 現在のメニュータイプを取得する
    /// </summary>
    private string GetCurrentMenuType()
    {
        if (uiElements.Length > 0 && MenuField.Count > 0 && uiElements[0] == MenuField[0])
            return "MenuField";
        else if (uiElements.Length > 0 && CharacterStatus.Count > 0 && uiElements[0] == CharacterStatus[0])
            return "CharacterStatus";
        else if (uiElements.Length > 0 && StatusMenu.Count > 0 && uiElements[0] == StatusMenu[0])
            return "StatusMenu";
        else if (uiElements.Length > 0 && ItemMenu.Count > 0 && uiElements[0] == ItemMenu[0])
            return "ItemMenu";
        else if (uiElements.Length > 0 && SkillMenu.Count > 0 && uiElements[0] == SkillMenu[0])
            return "SkillMenu";
        else if (uiElements.Length > 0 && ItemChoice.Count > 0 && uiElements[0] == ItemChoice[0])
            return "ItemChoice";
        else if (uiElements.Length > 0 && Charactersuseitems.Count > 0 && uiElements[0] == Charactersuseitems[0])
            return "ItemUsechoice";
        else if (uiElements.Length > 0 && StatusEquipment.Count > 0 && uiElements[0] == StatusEquipment[0])
            return "StatusEquipment";

        else
            return "Unknown";
    }

    /// <summary>
    /// キャンセル、一つ前の履歴に戻る処理の中身
    /// </summary>
    private void PreviousList()
    {
        if (menuHistory.Count == 0)
        {
            Debug.LogWarning("履歴がありません。メニューを閉じます。");
            St_PlayerController.Controllerinstance.HideMenuCanvas();
            return;
        }

        // 最新の履歴を取得
        MenuHistory lastHistory = menuHistory[menuHistory.Count - 1];
        menuHistory.RemoveAt(menuHistory.Count - 1);

        // ステータスメニューから戻る時にキャラアイコンの表示を更新
        // （現在が StatusMenu → 一つ前が CharacterStatus の想定）
        string currentType = GetCurrentMenuType();
        if (currentType == "StatusMenu" &&  characterIconStatus != null)
        {
            selectionField.SetActive(true);
            statusField.SetActive(false);
            characterIconStatus.StatusUpdates();
        }

        // ItemMenuから戻った際にアイテムメニューを非表示にしてstatusFieldを表示
        if (currentType == "ItemMenu")
        {
            // アイテムメニューを非表示,statusFieldを表示
            if (itemField != null && statusField != null)
            {
                itemField.SetActive(false);
                selectionField.SetActive(true);

                foreach(var Status in CharacterStatus)
                {
                    Status.GetComponent<CharacterIconStatus>()?.StatusUpdates();
                }

                // アイテムメニューのアイテムオブジェクトを削除
                foreach (Transform child in itemChoiceField.transform)
                {
                    if (child.name != "Selection Toggle") // Selection Toggleは残す
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        //スキルメニューから戻った際
        if (currentType == "SkillMenu")
        {
            skillField.SetActive(false);
            statusField.SetActive(true);
        }

        //アイテム選択から戻った際、アイテム説明リセット
        if (currentType == "ItemChoice")
        {
            descriptionText.text = ("");
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

        // ItemChoiceの場合、現在のItemChoiceリストと同期させる（削除されたオブジェクトを除外）
        if (lastHistory.menuType == "ItemChoice" && ItemChoice != null && ItemChoice.Count > 0)
        {
            // 現在のItemChoiceリストから有効なオブジェクトのみを取得
            var validItemChoice = new List<GameObject>();
            foreach (var go in ItemChoice)
            {
                if (go != null)
                {
                    validItemChoice.Add(go);
                }
            }
            
            // 有効なオブジェクトがあれば、uiElementsを更新
            if (validItemChoice.Count > 0)
            {
                uiElements = validItemChoice.ToArray();
                
                // 履歴のインデックスが有効な範囲内か確認
                if (currentIndex < 0 || currentIndex >= uiElements.Length)
                {
                    currentIndex = 0;
                }
                
                // 選択されるオブジェクトが有効か確認
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
            }
            else
            {
                // 有効なオブジェクトがない場合は警告
                Debug.LogWarning("ItemChoice に有効なオブジェクトがありません。");
                return;
            }
        }
        else
        {
            // その他のメニュータイプの場合、nullチェックのみ
            //配列が減って範囲外に出た時用
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
                
                // すべてnullの場合は警告
                if (uiElements[currentIndex] == null)
                {
                    Debug.LogWarning("復元先のUI要素がすべて無効です。");
                    return;
                }
            }
        }

        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI);

        //キャラクターアイテム使用から戻った際
        if (currentType == "ItemUsechoice")
        {
            Itemiconexplanation();//アイテム説明を表示

            //Charactersuseitemsをリセット　
            foreach (var obj in Charactersuseitems)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            // リストを空にする
            Charactersuseitems.Clear();
            //アイテム説明を表示する
            itemDescription.SetActive(true);
        }

        if (currentType == "StatusEquipment")//装備欄非表示
        {
            // 既存のアイテム選択をクリア
            StatusEquipment.Clear();

            // 既存のアイテムオブジェクトを削除
            foreach (Transform child in EquipmentChoiceField.transform)
            {
                if (child.name != "Selection Toggle") // Selection Toggleは残す
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            EquipmentChoiceField.SetActive(false);
        }

        Debug.Log($"履歴から復元: {lastHistory.menuType} (index: {lastHistory.index})");

        if (GameManager.Instance.audioSource != null && GameManager.Instance.decision != null)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.decision); // キャンセル音（仮）
        }
    }

    /// <summary>
    /// 選択中のUIを縦方向に切り替える（従来の上下移動）
    /// direction = -1（上へ移動）、+1（下へ移動）
    /// </summary>
    private void ChangeSelectionVertical(int direction)
    {
        if (uiElements.Length == 0) return;

        StopBlink(); // 古い選択の点滅を止める

        // インデックスを更新（範囲外ならループする）
        currentIndex += direction;
        if (currentIndex < 0) currentIndex = uiElements.Length - 1;
        else if (currentIndex >= uiElements.Length) currentIndex = 0;

        // 新しい選択対象を更新
        SelectUI = uiElements[currentIndex];

        // 枠を移動して点滅開始
        MoveFrameTo(SelectUI);
        StartBlink();
    }

    /// <summary>
    /// 選択中のUIを縦方向に切り替える（二列レイアウト対応）
    /// direction = -1（上へ移動）、+1（下へ移動）
    /// 二列の場合：1→3, 2→4 のように2つ飛ばしで移動
    /// </summary>
    private void MoveSelectionVerticalTwoColumn(int direction)
    {
        if (uiElements.Length == 0) return;

        StopBlink(); // 古い選択の点滅を止める

        // 二列レイアウトの場合の縦移動ロジック
        // 例：0,1,2,3 の要素がある場合 
        // 上移動: 2→0, 3→1 (2つ前へ)
        // 下移動: 0→2, 1→3 (2つ後へ)

        int newIndex = currentIndex;
        
        if (direction > 0) // 下へ移動
        {
            // 2つ後へ移動（範囲外ならループ）
            newIndex = currentIndex + 2;
            if (newIndex >= uiElements.Length)
            {
                // 範囲外の場合、適切な位置に調整
                if (uiElements.Length % 2 == 0)
                {
                    // 偶数個の場合：0→2, 1→3, 2→0, 3→1
                    newIndex = (currentIndex + 2) % uiElements.Length;
                }
                else
                {
                    // 奇数個の場合：最後の要素に移動
                    newIndex = uiElements.Length - 1;
                }
            }
        }
        else // 上へ移動
        {
            // 2つ前へ移動（範囲外ならループ）
            newIndex = currentIndex - 2;
            if (newIndex < 0)
            {
                // 範囲外の場合、適切な位置に調整
                if (uiElements.Length % 2 == 0)
                {
                    // 偶数個の場合：0→2, 1→3, 2→0, 3→1
                    newIndex = (currentIndex - 2 + uiElements.Length) % uiElements.Length;
                }
                else
                {
                    // 奇数個の場合：最初の要素に移動
                    newIndex = 0;
                }
            }
        }

        // インデックスを更新
        currentIndex = newIndex;
        SelectUI = uiElements[currentIndex];

        // 枠を移動して点滅開始
        MoveFrameTo(SelectUI);
        StartBlink();
    }

    /// <summary>
    /// 選択中のUIを横方向に切り替える（二列レイアウト対応）
    /// direction = -1（左へ移動）、+1（右へ移動）
    /// </summary>
    private void MoveSelectionHorizontalTwoColumn(int direction)
    {
        if (uiElements.Length == 0) return;

        StopBlink(); // 古い選択の点滅を止める

        // 二列レイアウトの場合の横移動ロジック
        // 例：0,1,2,3 の要素がある場合
        // 列1: 0, 2 (偶数インデックス)
        // 列2: 1, 3 (奇数インデックス)
        
        int newIndex = currentIndex;
        
        if (direction > 0) // 右へ移動
        {
            // 現在が左列（偶数）なら右列（奇数）の同じ行へ
            if (currentIndex % 2 == 0 && currentIndex + 1 < uiElements.Length)
            {
                newIndex = currentIndex + 1;
            }
        }
        else // 左へ移動
        {
            // 現在が右列（奇数）なら左列（偶数）の同じ行へ
            if (currentIndex % 2 == 1 && currentIndex - 1 >= 0)
            {
                newIndex = currentIndex - 1;
            }
        }

        // インデックスを更新
        currentIndex = newIndex;
        SelectUI = uiElements[currentIndex];

        // 枠を移動して点滅開始
        MoveFrameTo(SelectUI);
        StartBlink();
    }

    /// <summary>
    /// 選択中のUIを縦方向に切り替える（五列レイアウト対応）
    /// direction = -1（上へ移動）、+1（下へ移動）
    /// 五列の場合：1→6, 2→7 のように5つ飛ばしで移動
    /// </summary>
    private void MoveSelectionVerticalFiveColumn(int direction)
    {

    }

    /// <summary>
    /// 選択中のUIを横方向に切り替える（五列レイアウト対応）
    /// direction = -1（左へ移動）、+1（右へ移動）
    /// </summary>
    private void MoveSelectionHorizontalFiveColumn(int direction)
    {
        // 五列レイアウトの場合の横移動ロジック
        // 例：0,~14 の要素がある場合
        // 列1: 0, 5, 10
        // 列2: 1, 6, 11 
    }

    /// <summary>
    /// 枠（Frame）を現在選択中のUIに移動
    /// 選択対象の子オブジェクトとして配置し、サイズも合わせる
    /// </summary>
    private void MoveFrameTo(GameObject target)
    {
        if (Frame != null && target != null)
        {
            // 枠を対象UIの子にする
            Frame.transform.SetParent(target.transform, false);

            // サイズと位置をリセット
            RectTransform frameRect = Frame.GetComponent<RectTransform>();
            RectTransform targetRect = target.GetComponent<RectTransform>();

            if (frameRect != null && targetRect != null)
            {
                // サイズを親UIに合わせる
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = Vector2.zero;
                frameRect.offsetMax = Vector2.zero;

                // 位置もリセット
                frameRect.localPosition = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 枠の点滅を開始する（選択状態の可視化）
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
            frameCg.alpha = 0f; // 非選択で透明化
        }
    }

    /// <summary>
    /// キャラクターアイコンを生成し、SelectionFieldの子オブジェクトとして指定位置に配置する
    /// </summary>
    private void GenerateCharacterIcons()//メニュー表示時呼ぶ
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

        // SelectionFieldを探す（このスクリプトがアタッチされているオブジェクトの子オブジェクト）
        Transform selectionField = transform.Find("Selection Field");
        if (selectionField == null)
        {
            Debug.LogError("Selection Field が見つかりません！");
            return;
        }

        // 位置設定（4個まで）
        Vector2[] positions = new Vector2[]
        {
            new Vector2(960, 380),   // 一番目
            new Vector2(960, 120),   // 二番目
            new Vector2(960, -140),  // 三番目
            new Vector2(960, -400)   // 四番目
        };

        // 最大4個まで生成
        int maxIcons = Mathf.Min(allyList.Count, 4);
        
        for (int i = 0; i < maxIcons; i++)
        {
            var characterData = allyList[i];
            
            // Icon_Character プレハブが設定されているかチェック
            if (characterData.Icon_Character1 == null)
            {
                Debug.LogWarning($"Character {i} の Icon_Character が設定されていません");
                continue;
            }

            // アイコンをSelectionFieldの子として生成
            GameObject iconInstance = Instantiate(characterData.Icon_Character1, selectionField);
            
            // 位置を設定
            RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = positions[i];
            }

            // CharacterIconStatus コンポーネントを追加
            var icon_characterIconStatus = iconInstance.GetComponent<CharacterIconStatus>();
            
            // キャラクターデータを設定
            icon_characterIconStatus.status = characterData;
            
            // ステータス表示を更新
            icon_characterIconStatus.StatusUpdates();

            // CharacterStatus リストに追加
            CharacterStatus.Add(iconInstance);

            // SelectionSettings コンポーネントを追加（必要に応じて）
            var selectionSettings = iconInstance.GetComponent<SelectionSettings>();
            if (selectionSettings == null)
            {
                selectionSettings = iconInstance.AddComponent<SelectionSettings>();
            }
            // 必要に応じて SelectionSettings の設定を行う

            Debug.Log($"Character Icon {i} を生成しました: {characterData.name} at {positions[i]}");
        }

        Debug.Log($"合計 {CharacterStatus.Count} 個のキャラクターアイコンを生成しました");
    }

    /// <summary>
    /// アイテムアイコンを生成し、ItemChoiceFieldに二列配置する
    /// </summary>
    /// <param name="itemKinds">生成するアイテムの種類配列</param>
    private void GenerateItemIcons(string[] itemKinds)
    {
        if (db_PlayerItem == null)
        {
            Debug.LogError("Player_Item データベースが設定されていません！");
            return;
        }

        if (itemChoiceField == null)
        {
            Debug.LogError("ItemChoiceField が見つかりません！");
            return;
        }

        // 既存のアイテム選択をクリア
        ItemChoice.Clear();
        
        // 既存のアイテムオブジェクトを削除
        foreach (Transform child in itemChoiceField.transform)
        {
            if (child.name != "Selection Toggle") // Selection Toggleは残す
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 条件に合うアイテムをID順で取得（最大20個）
        var filteredItems = new List<D_It_StatusData>();
        foreach (var item in db_PlayerItem.ItemList)
        {
            // enum値を直接比較
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

        // 二列配置の位置設定
        // 左列: x = -230, 右列: x = 230
        // y軸: 450から-95ずつ減らして最大10行（-405まで）
        Vector2[] positions = new Vector2[20];
        for (int i = 0; i < 20; i++)
        {
            int row = i / 2; // 行（0-9）
            int col = i % 2; // 列（0=左, 1=右）
            
            float x = col == 0 ? -230f : 230f;
            float y = 450f - (row * 95f);
            positions[i] = new Vector2(x, y);
        }

        // アイテムを生成
        int itemCount = filteredItems.Count;
        for (int i = 0; i < itemCount; i++)
        {
            var itemData = filteredItems[i];
            
            // アイテムプレハブが設定されているかチェック
            if (itemData.Icon == null)
            {
                Debug.LogWarning($"Item {i} の Icon が設定されていません");
                continue;
            }

            // アイテムアイコンをItemChoiceFieldの子として生成
            GameObject itemInstance = Instantiate(itemData.Icon, itemChoiceField.transform);
            
            // 位置を設定
            RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchoredPosition = positions[i];
            }

            // ItemQuantity コンポーネントを追加
            var itemQuantity = itemInstance.GetComponent<ItemQuantity>();
            if (itemQuantity == null)
            {
                itemQuantity = itemInstance.AddComponent<ItemQuantity>();
            }

            // SelectionSettings コンポーネントを追加
            /*var selectionSettings = itemInstance.GetComponent<SelectionSettings>();
            if (selectionSettings == null)
            {
                selectionSettings = itemInstance.AddComponent<SelectionSettings>();
            }*/

            // ItemChoice リストに追加
            ItemChoice.Add(itemInstance);

            Debug.Log($"Item Icon {i} を生成しました: {itemData.name} at {positions[i]}");
        }

        // Selection Toggleを1列目と2列目の一番下に配置
        Transform selectionToggle = itemChoiceField.transform.Find("Selection Toggle");
        if (selectionToggle != null)
        {
            // 1列目の一番下（9行目）
            GameObject toggle1 = Instantiate(selectionToggle.gameObject, itemChoiceField.transform);
            /*RectTransform toggle1Rect = toggle1.GetComponent<RectTransform>();
            if (toggle1Rect != null)
            {
                toggle1Rect.anchoredPosition = new Vector2(-230f, -405f);
            }*/
            ItemChoice.Add(toggle1);

            // 2列目の一番下（9行目）
            GameObject toggle2 = Instantiate(selectionToggle.gameObject, itemChoiceField.transform);
            ItemChoice.Add(toggle2);

            Debug.Log("Selection Toggle を配置しました");
        }

        // TODO: 20個を超える場合のSelection Toggle処理を追加
        // オーバーした分を切り替えて表示できるようにする予定
        if (filteredItems.Count >= 20)
        {
            Debug.LogWarning("アイテムが20個以上あります。Selection Toggle処理を実装予定");
        }

        Debug.Log($"合計 {ItemChoice.Count} 個のアイテム選択オブジェクトを生成しました");
    }

    //装備欄の表示、アイコン生成切り替え
    private void Generateequipment(string[] EquipmentKinds) 
    {
        //GenerateItemIconsの装備版
        //EquipmentChoiceFieldの子オブジェクトとして一列で生成
        //X軸は0、YはGenerateItemIconsと同じで10個まで
        
        if (db_PlayerItem == null)
        {
            Debug.LogError("Player_Item データベースが設定されていません！");
            return;
        }

        if (EquipmentChoiceField == null)
        {
            Debug.LogError("EquipmentChoiceField が見つかりません！");
            return;
        }

        // 既存のアイテム選択をクリア
        StatusEquipment.Clear();
        
        // 既存のアイテムオブジェクトを削除　　3/19たまに削除しきれないことがある後修正
        foreach (Transform child in EquipmentChoiceField.transform)
        {
            if (child.name != "Selection Toggle") // Selection Toggleは残す
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 条件に合うアイテムをID順で取得（最大10個）
        var filteredItems = new List<D_It_StatusData>();
        foreach (var item in db_PlayerItem.ItemList)
        {
            // enum値を直接比較
            bool isMatch = false;
            foreach (string kindString in EquipmentKinds)
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
        
        // ID順でソートして最大10個まで
        filteredItems = filteredItems
            .OrderBy(item => item.Id)
            .Take(10)
            .ToList();

        // 一列配置の位置設定
        // X軸は0、Y軸: 450から-95ずつ減らして最大10行（-405まで）
        Vector2[] positions = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float x = 0f; // 一列なのでX軸は0
            float y = 450f - (i * 95f);
            positions[i] = new Vector2(x, y);
        }

        // アイテムを生成
        int itemCount = filteredItems.Count;
        for (int i = 0; i < itemCount; i++)
        {
            var itemData = filteredItems[i];
            
            // アイテムプレハブが設定されているかチェック
            if (itemData.Icon == null)
            {
                Debug.LogWarning($"Item {i} の Icon が設定されていません");
                continue;
            }

            // アイテムアイコンをEquipmentChoiceFieldの子として生成
            GameObject itemInstance = Instantiate(itemData.Icon, EquipmentChoiceField.transform);
            
            // 位置を設定
            RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchoredPosition = positions[i];
            }

            // ItemQuantity コンポーネントを追加（装備品の場合も個数管理が必要な場合は追加）
            var itemQuantity = itemInstance.GetComponent<ItemQuantity>();
            if (itemQuantity == null)
            {
                itemQuantity = itemInstance.AddComponent<ItemQuantity>();
            }

            // StatusEquipment リストに追加
            StatusEquipment.Add(itemInstance);

            Debug.Log($"Equipment Icon {i} を生成しました: {itemData.name} at {positions[i]}");
        }

        // Selection Toggleを一番下に配置（一列なので1つ）
        Transform selectionToggle = EquipmentChoiceField.transform.Find("Selection Toggle");
        if (selectionToggle != null)
        {
            GameObject toggle = Instantiate(selectionToggle.gameObject, EquipmentChoiceField.transform);
            StatusEquipment.Add(toggle);

            Debug.Log("Selection Toggle を配置しました");
        }

        // TODO: 10個を超える場合のSelection Toggle処理を追加
        // オーバーした分を切り替えて表示できるようにする予定（別のメソッドで）
        if (filteredItems.Count >= 10)
        {
            Debug.LogWarning("装備品が10個以上あります。Selection Toggle処理を実装予定");
        }

        Debug.Log($"合計 {StatusEquipment.Count} 個の装備品選択オブジェクトを生成しました");

        //メニューの切り替え
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // 装備一覧表示
        if (EquipmentChoiceField != null)
        {
            EquipmentChoiceField.SetActive(true);
        }

        // 選択対象を StatusEquipment に切り替え
        uiElements = StatusEquipment.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新
        Debug.Log("UI 切替 → StatusEquipment");
    }

    //メニューの決定処理
    private void StatusIconprocess()//アイコン表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();
        
        // 選択対象を CharacterStatus に切り替え
        uiElements = CharacterStatus.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新
        Debug.Log("UI 切替 → CharacterStatus");
    }
    private void StatusMenuprocess()//ステータスメニュー表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        characterIconStatus = SelectUI.GetComponent<CharacterIconStatus>();

        //ステータスメニューの数値スクリプトを獲得し、そこに選択したキャラクターデータを送る
        d_Character = statusField.GetComponent<StatusNumber>();
        if (d_Character != null)
        {
            d_Character.D_status = characterIconStatus.status;
        }
        d_Character.StatusNumber_Updates();
        d_ch_Status = d_Character.D_status;

        // ステータスメニュー表示
        if (statusField != null)
        {
            statusField.SetActive(true);
            EquipmentChoiceField.SetActive(false);
            selectionField.SetActive(false);
        }

        // 見つかったらアクティブにする（表示）
        //statusField.gameObject.SetActive(true);
        // 選択対象を切り替え
        uiElements = StatusMenu.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新
        Debug.Log("UI 切替 → StatusMenu");
    }

    private void SkillMenuprocess()//アイテムメニュー表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        //習得可能スキル表示


        // スキルメニュー表示
        if (skillField != null)
        {
            skillField.SetActive(true);
            statusField.SetActive(false);
        }

        // 選択対象を切り替え
        uiElements = SkillMenu.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新
        Debug.Log("UI 切替 → SkillMenu");
    }

    private void ItemMenuprocess()//アイテムメニュー表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        // 消耗品アイテムを生成（Buff,DeBuff,Attack,Magic,Recovery）
        GenerateItemIcons(new string[] { "Buff", "DeBuff", "Attack", "Magic", "HP_Recovery", "MP_Recovery" });

        // アイテムメニュー表示
        if (statusField != null)
        {
            itemField.SetActive(true);
            selectionField.SetActive(false);
        }

        // 選択対象を切り替え
        uiElements = ItemMenu.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新
        Debug.Log("UI 切替 → ItemMenu");
    }

    private void Consumablesprocess()//消耗品表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();
        
        // 消耗品アイテムを生成（Buff,DeBuff,Attack,Magic,Recovery）
        //GenerateItemIcons(new string[] { "Buff", "DeBuff", "Attack", "Magic", "Recovery" });
        
        // 選択対象を ItemChoice に切り替え
        uiElements = ItemChoice.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新

        Itemiconexplanation();
        // ItemChoiceFieldを表示
        /*if (itemChoiceField != null)
        {
            itemChoiceField.SetActive(true);
            itemField.SetActive(false);
        }*/

        Debug.Log("UI 切替 → ItemChoice (Consumables)");
    }

    private void Equipmentprocess()//装備品表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();
        
        // 選択対象を ItemChoice に切り替え
        uiElements = ItemChoice.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新

        Itemiconexplanation();

        Debug.Log("UI 切替 → ItemChoice (Equipment)");
    }

    private void Valuablesprocess()//貴重品表示
    {
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();
        
        // 選択対象を ItemChoice に切り替え
        uiElements = ItemChoice.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新

        Itemiconexplanation();

        Debug.Log("UI 切替 → ItemChoice (Valuables)");
    }

    private void ItemUsechoiceprocess()//アイテムを使用するキャラクターを選択する
    {
        //numericalProcessingに使用するアイテムデータを送る
        numericalProcessing.ItemUse_ItData = SelectUI.GetComponent<ItemQuantity>()?.D_It_StatusData;
        if (numericalProcessing.ItemUse_ItData == null)
        {
            Debug.LogError("アイテムデータが見つかりません！");
            return;
        }

        // 履歴が2つ以上あるときだけ「ひとつ前の状態」をチェック
        if (menuHistory.Count > 1)
        {
            // ひとつ前のメニュー（最新のひとつ前 = Count - 1）
            var previousMenu = menuHistory[menuHistory.Count -1];

            if (previousMenu.menuType == "StatusMenu") // ここで判定
            {
                ItemUseprocess();
                return;
            }
        }
        // 現在の状態を履歴に保存
        SaveCurrentStateToHistory();

        itemDescription.SetActive(false);

        //ひとつ前がステータスメニューでなければに改造する11/3予定ならは装備
        //アイテムを使用するキャラアイコン2生成,選択する

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
        // itemFieldをTransform
        Transform itemField = transform.Find("Item Field");
        if (itemField == null)
        {
            Debug.LogError("Item Field が見つかりません！");
            return;
        }

        // 位置設定（4個まで）
        Vector2[] positions = new Vector2[]
        {
            new Vector2(700, 380),   // 一番目
            new Vector2(700, 120),   // 二番目
            new Vector2(700, -140),  // 三番目
            new Vector2(700, -400)   // 四番目
        };

        // 最大4個まで生成
        int maxIcons = Mathf.Min(allyList.Count, 4);

        for (int i = 0; i < maxIcons; i++)
        {
            var characterData = allyList[i];

            // Icon_Character プレハブが設定されているかチェック
            if (characterData.Icon_Character2 == null)
            {
                Debug.LogWarning($"Character {i} の Icon_Character2 が設定されていません");
                continue;
            }

            // アイコンをitemFieldの子として生成
            GameObject iconInstance = Instantiate(characterData.Icon_Character2, itemField);

            // 位置を設定
            RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = positions[i];
            }

            // CharacterIconStatus コンポーネントを追加
            var icon_characterIconStatus = iconInstance.GetComponent<CharacterIconStatus>();

            // キャラクターデータを設定
            icon_characterIconStatus.status = characterData;

            // ステータス表示を更新
            icon_characterIconStatus.StatusUpdateMini();

            // Charactersuseitems リストに追加
            Charactersuseitems.Add(iconInstance);

            Debug.Log($"Character Icon {i} を生成しました: {characterData.name} at {positions[i]}");
        }

        // 選択対象を Charactersuseitems に切り替え
        uiElements = Charactersuseitems.ToArray();
        currentIndex = 0; // 先頭に戻す
        SelectUI = uiElements[currentIndex];
        MoveFrameTo(SelectUI); // 枠を更新

        Debug.Log($"合計 {CharacterStatus.Count} 個のキャラクターアイコンを生成しました");
    }

    private void ItemUseprocess()//アイテム使用処理
    {
        // ひとつ前のメニュー（最新のひとつ前 = Count - 1）
        var previousMenu = menuHistory[menuHistory.Count - 1];

        if (previousMenu.menuType == "StatusMenu") // ひとつ前のメニューがStatusMenuなら
        {
            numericalProcessing.Use_subject_ChData = d_ch_Status;
        }
        else
        {
            //アイテムを使用するキャラクターデータを送る
            numericalProcessing.Use_subject_ChData = SelectUI.GetComponent<CharacterIconStatus>()?.status;
        }

        if (numericalProcessing.Use_subject_ChData == null)
        {
            Debug.LogError("キャラクターデータが見つかりません！");
            return;
        }

        //アイテム使用処理
        numericalProcessing.Itemtypedetermination();

        if(previousMenu.menuType == "StatusMenu")
        {
            d_Character.StatusNumber_Updates();//ステータス更新

            foreach (var ui in StatusEquipment)//UI全て更新
            {
                ui?.GetComponent<ItemQuantity>()?.QuantityUpdate();
            }
        }
        else
        {
            //アイテム個数更新
            SelectUI.GetComponent<CharacterIconStatus>()?.StatusUpdateMini();

            if (menuHistory == null || menuHistory.Count == 0)
            {
                Debug.LogWarning("履歴が空です。");
                return;
            }

            foreach (var ui in ItemChoice)//UI全て更新
            {
                ui?.GetComponent<ItemQuantity>()?.QuantityUpdate();
            }

            //ひとつ前の要素で選択していたUIの更新処理を実行する
            //menuHistory[^1].elements[menuHistory[^1].index].GetComponent<ItemQuantity>()?.QuantityUpdate();

            //使用するアイテムの個数が0ならそのアイコンを削除してItemChoiceを詰めて戻る
            if (numericalProcessing.ItemUse_ItData.Number == 0)
            {
                Removemissingitems();
                return;
            }
        }
    }

    private void Removemissingitems()//アイテムアイコン削除と整理
    {
        if (numericalProcessing == null || numericalProcessing.ItemUse_ItData == null)
        {
            Debug.LogWarning("削除対象のアイテムデータがありません。");
            return;
        }
        string targetName = numericalProcessing.ItemUse_ItData.name;
        if (string.IsNullOrEmpty(targetName))
        {
            Debug.LogWarning("削除対象アイテム名が空です。");
            return;
        }

        // 比較を正規化する (strip "(Clone)" and trim)
        string Normalize(string s) => s?.Replace("(Clone)", "").Trim();

        //ItemChoice リスト内の該当アイコンを削除（後ろから回す）
        bool removedAny = false;

        for (int i = ItemChoice.Count - 1; i >= 0; i--)
        {
            var go = ItemChoice[i];
            if (go == null)
            {
                ItemChoice.RemoveAt(i);
                continue;
            }

            if (Normalize(go.name) == targetName)
            {
                // シーン上オブジェクトを削除
                Destroy(go);
                ItemChoice.RemoveAt(i);
                removedAny = true;
                // 複数個の同名アイコンをすべて消したければ break を外す（ここではすべて削除）
            }
        }

        // 残ったアイテムアイコンを取得（Selection Toggle は除外して詰める）
        var iconList = new List<GameObject>();
        // 保持されている ItemChoice リストを優先して使う（ただし null と Toggle を除外）
        foreach (var go in ItemChoice)
        {
            if (go == null) continue;
            if (Normalize(go.name) == "Selection Toggle") continue;
            iconList.Add(go);
        }

        // もし ItemChoice が正確でなければ、親から取得するフォールバック
        if (iconList.Count == 0)
        {
            foreach (Transform child in itemChoiceField.transform)
            {
                if (child == null) continue;
                if (Normalize(child.name) == "Selection Toggle") continue;
                iconList.Add(child.gameObject);
            }
        }

        // 位置配列を GenerateItemIcons と同じルールで作る
        Vector2[] positions = new Vector2[20];
        for (int i = 0; i < 20; i++)
        {
            int row = i / 2;
            int col = i % 2;
            float x = col == 0 ? -230f : 230f;
            float y = 450f - (row * 95f);
            positions[i] = new Vector2(x, y);
        }

        // アイコンを順に詰めて配置（最大 20 個）
        int placeCount = Mathf.Min(iconList.Count, 20);
        for (int i = 0; i < placeCount; i++)
        {
            var icon = iconList[i];
            if (icon == null) continue;

            var rect = icon.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = positions[i];
            }

            // 階層順や ItemChoice リストの順序も整える（任意）
            icon.transform.SetSiblingIndex(i);
        }

        // Selection Toggle の調整（存在するならアイコンの下に配置）
        // 既に存在する Selection Toggle オブジェクトを探して、必要なら最後に移動する
        /*var toggles = new List<GameObject>();
        foreach (Transform child in itemChoiceField.transform)
        {
            if (Normalize(child.name) == "Selection Toggle")
                toggles.Add(child.gameObject);
        }

        // アイコンの後にSelection Toggleを配置する（GenerateItemIcons のように 2 つ）
        for (int t = 0; t < toggles.Count; t++)
        {
            int idx = placeCount + t;
            toggles[t].transform.SetSiblingIndex(idx);
            var rect = toggles[t].GetComponent<RectTransform>();
            if (rect != null && idx < positions.Length)
            {
                rect.anchoredPosition = positions[Mathf.Min(idx, positions.Length - 1)];
            }
        }*/

        Debug.Log($"アイコン削除と詰め直し完了。removedAny={removedAny} remainingIcons={iconList.Count}");

        //処理後、戻る
        PreviousList();
    }

    /// <summary>
    /// 入力処理の有効/無効を設定する
    /// </summary>
    /// <param name="enabled">true: 有効, false: 無効</param>
    public void SetInputEnabled(bool enabled)
    {
        Debug.Log($"入力処理を {(enabled ? "有効" : "無効")} にしました");
        statusMenu.SetActive(enabled);
        inputEnabled = enabled;

        // CharacterStatus内のオブジェクトをすべて削除
        /* foreach (var obj in CharacterStatus)
         {
             if (obj != null)
             {
                 Destroy(obj);
             }
         }
         // リストを空にする
         CharacterStatus.Clear();*/

        if (uiElements != null)
        {
            string menuType = GetCurrentMenuType();
            // 現在のメニュータイプがMenuFieldなら選択フィールドのみ表示
            if (menuType == "MenuField")
            {
                selectionField.SetActive(true);
                statusField.SetActive(false);
                itemField.SetActive(false);
                EquipmentChoiceField.SetActive(false);

                //ステータスの更新
                foreach (var Status in CharacterStatus)
                {
                    Status.GetComponent<CharacterIconStatus>()?.StatusUpdates();
                }

                //アイテムを使用しようとしているキャラクターをクリア
                foreach (var obj in Charactersuseitems)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                Charactersuseitems.Clear();
                //アイテム説明を表示する
                itemDescription.SetActive(true);
            }

            MoneyText.GetComponent<CharacterIconStatus>()?.DisplayOfMoneyHeld();

            menuHistory.Clear();
            uiElements = MenuField.ToArray();
            // 現在の状態を履歴に保存
            SaveCurrentStateToHistory();

            currentIndex = 0;
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI); // 枠を更新
        }

        if (enabled)//いらないかも？
        {
            // メニューが開かれた時に履歴をクリア
            menuHistory.Clear();
            Debug.Log("メニューが開かれました。履歴をクリアしました。");
        }
        
        Debug.Log($"入力処理を {(enabled ? "有効" : "無効")} にしました");

        //現在の作りでは最初の履歴に戻ってからしか非表示にできないので履歴をリセットする必要はない
    }

    /// <summary>
    /// メニューがアクティブかどうかを判定
    /// </summary>
    private bool IsMenuActive()
    {
        return statusMenu != null && statusMenu.activeInHierarchy;
    }

    // 前のメニューで選択されていた要素を取得するメソッド
    public GameObject GetPreviousMenuSelectedElement()
    {
        if (menuHistory == null || menuHistory.Count == 0)
        {
            return null;
        }
        
        var previousMenu = menuHistory[^1];
        if (previousMenu.elements == null || previousMenu.index < 0 || previousMenu.index >= previousMenu.elements.Length)
        {
            return null;
        }
        
        return previousMenu.elements[previousMenu.index];
    }
}
