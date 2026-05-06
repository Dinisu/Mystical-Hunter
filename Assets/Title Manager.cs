using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleManager : MonoBehaviour
{
    [Header("タイトルメニュー")]
    public List<GameObject> TitleMenu = new List<GameObject>();

    [Header("ロードメニュー")]
    public List<GameObject> LoadMenu = new List<GameObject>();

    [Header("メニュー表示フィールド")]
    public GameObject TitleField;
    public GameObject LoadField;

    [Header("選択枠")]
    public GameObject Frame;

    private GameObject[] uiElements;                 // 現在操作対象の UI 要素配列
    private int currentIndex = 0;                    // 現在の選択インデックス
    private GameObject SelectUI;                     // 現在選択中の UI
    private PlayerInput playerInput;                 // InputSystem の PlayerInput
    private bool inputEnabled = true;                // 入力の有効/無効
    private CanvasGroup frameCg;                     // 選択枠の CanvasGroup
    private Tween blinkTween;

    // 前の選択状態を保存
    private int titleMenuIndex = 0;

    private void Awake()
    {
        // タイトル画面の初期メニューを設定
        uiElements = TitleMenu.ToArray();
    }

    private void Start()
    {
        // PlayerInput を取得
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
        }

        if (playerInput == null)
        {
            Debug.LogError("PlayerInput がシーン内に見つかりません！");
        }
        else
        {
            var move = playerInput.actions["Move"];
            if (move != null)
            {
                move.performed += OnMove;
                move.canceled += OnMove;
            }

            var attack = playerInput.actions["Attack"];
            if (attack != null)
            {
                attack.performed += OnAttack;
            }

            var cancel = playerInput.actions["Cancel"];
            if (cancel != null)
            {
                cancel.performed += OnCancel;
            }
        }

        // Frame に CanvasGroup がなければ追加
        if (Frame != null)
        {
            frameCg = Frame.GetComponent<CanvasGroup>();
            if (frameCg == null)
            {
                frameCg = Frame.AddComponent<CanvasGroup>();
            }
        }

        SetTitleMenu();
    }

    /// <summary>
    /// タイトルメニューを表示状態に設定
    /// </summary>
    private void SetTitleMenu()
    {
        if (TitleField != null) TitleField.SetActive(true);
        if (LoadField != null) LoadField.SetActive(false);

        uiElements = TitleMenu.ToArray();
        // 保存されたタイトルメニューのインデックスを復元
        currentIndex = titleMenuIndex;

        if (uiElements.Length > 0 && uiElements[0] != null)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
            StartBlink();
        }
        else
        {
            SelectUI = null;
            StopBlink();
        }
    }

    /// <summary>
    /// ロードメニューを表示状態に設定
    /// </summary>
    private void SetLoadMenu()
    {
        // タイトルメニューの選択インデックスを保存
        titleMenuIndex = currentIndex;

        currentIndex = 0;

        if (TitleField != null) TitleField.SetActive(false);
        if (LoadField != null) LoadField.SetActive(true);

        uiElements = LoadMenu.ToArray();

        if (uiElements.Length > 0 && uiElements[0] != null)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
            StartBlink();
        }
        else
        {
            SelectUI = null;
            StopBlink();
        }
    }

    /// <summary>
    /// Move 入力の処理
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!inputEnabled || !IsMenuActive()) return;
        if (!context.performed) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.y > 0.5f)
        {
            MoveSelectionVertical(-1);
        }
        else if (input.y < -0.5f)
        {
            MoveSelectionVertical(1);
        }
    }

    /// <summary>
    /// Attack 入力の処理
    /// LoadMenu と Load の両方を同じ処理に統一
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!inputEnabled || !IsMenuActive()) return;
        if (!context.performed) return;
        if (SelectUI == null) return;

        var settings = SelectUI.GetComponent<SelectionSettings>();
        if (settings == null)
        {
            Debug.LogWarning($"SelectionSettings がアタッチされていません → {SelectUI.name}");
            return;
        }

        switch (settings.choose)
        {
            case SelectionSettings.Choose.LoadMenu:
            case SelectionSettings.Choose.Load:
                LoadMenuProcess();
                break;
            default:
                Debug.Log($"TitleManager: 未対応の Choose: {settings.choose}");
                break;
        }
    }

    /// <summary>
    /// Cancel 入力の処理
    /// ロードメニューからタイトルメニューへ戻る
    /// </summary>
    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!inputEnabled || !IsMenuActive()) return;
        if (!context.performed) return;

        if (GetCurrentMenuType() == "LoadMenu")
        {
            SetTitleMenu();
        }
    }

    /// <summary>
    /// LoadMenu/Load 選択時の共通処理
    /// </summary>
    private void LoadMenuProcess()
    {
        if (GetCurrentMenuType() == "TitleMenu")
        {
            // タイトル画面から直接ロードスロット選択できる場合
            var saveDisplay = SelectUI?.GetComponent<Savedatadisplay>();
            if (saveDisplay != null)
            {
                ExecuteLoad(saveDisplay.SaveNumber);
                return;
            }

            SetLoadMenu();
        }
        else if (GetCurrentMenuType() == "LoadMenu")
        {
            var saveDisplay = SelectUI?.GetComponent<Savedatadisplay>();
            if (saveDisplay != null)
            {
                ExecuteLoad(saveDisplay.SaveNumber);
            }
            else
            {
                Debug.LogWarning($"LoadMenu の選択に Savedatadisplay が見つかりません → {SelectUI?.name}");
            }
        }
    }

    /// <summary>
    /// SaveManager でセーブデータをロードする
    /// </summary>
    private void ExecuteLoad(int saveNumber)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManagerが存在しません");
            return;
        }

        SaveManager.Instance.Load(saveNumber);
    }

    /// <summary>
    /// 選択中のメニューを縦に移動する
    /// </summary>
    private void MoveSelectionVertical(int direction)
    {
        if (uiElements == null || uiElements.Length == 0) return;

        currentIndex += direction;
        if (currentIndex < 0) currentIndex = 0;
        if (currentIndex >= uiElements.Length) currentIndex = uiElements.Length - 1;

        if (uiElements[currentIndex] != null)
        {
            SelectUI = uiElements[currentIndex];
            MoveFrameTo(SelectUI);
            StartBlink();
        }
    }

    /// <summary>
    /// 選択枠を現在の UI に移動する
    /// </summary>
    private void MoveFrameTo(GameObject target)
    {
        if (Frame == null || target == null) return;

        var frameRect = Frame.GetComponent<RectTransform>();
        var targetRect = target.GetComponent<RectTransform>();

        if (frameRect != null && targetRect != null)
        {
            Frame.transform.SetParent(target.transform, false);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            frameRect.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 選択枠の点滅を開始する
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
    /// 選択枠の点滅を停止する
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
    /// 現在表示中のメニュータイプを判定する
    /// </summary>
    private string GetCurrentMenuType()
    {
        if (uiElements != null && uiElements.Length > 0 && TitleMenu.Count > 0 && uiElements[0] == TitleMenu[0])
            return "TitleMenu";

        if (uiElements != null && uiElements.Length > 0 && LoadMenu.Count > 0 && uiElements[0] == LoadMenu[0])
            return "LoadMenu";

        return "Unknown";
    }

    /// <summary>
    /// メニューがアクティブかどうかを判定する
    /// </summary>
    private bool IsMenuActive()
    {
        if (TitleField != null && TitleField.activeSelf) return true;
        if (LoadField != null && LoadField.activeSelf) return true;
        return uiElements != null && uiElements.Length > 0;
    }
}
