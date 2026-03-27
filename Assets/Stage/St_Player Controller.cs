using UnityEngine;
using UnityEngine.InputSystem;

public class St_PlayerController : MonoBehaviour
{
    public static St_PlayerController Controllerinstance;
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    
    private Rigidbody rb;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private GameObject currentItemObject; // 現在触れているItemタグのオブジェクト
    private bool isMenuOpen = false; // メニューが開いているかどうか

    [SerializeField] private GameObject Mmenu_Canvas; // プレハブ参照
    private GameObject menuCanvasInstance; // 生成されたインスタンス
    private InventManager inventManager; // InventManagerへの参照

    void Awake()
    {
        Controllerinstance = this;
        // Rigidbodyを取得
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody が見つかりません！");
        }

        // PlayerInputを取得
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput がシーン内に見つかりません！");
            }
        }

        // InventManagerを取得
        inventManager = FindObjectOfType<InventManager>();
        if (inventManager == null)
        {
            Debug.LogWarning("InventManager がシーン内に見つかりません。");
        }
    }

    void OnEnable()
    {
        // Moveアクションを登録
        if (playerInput != null && playerInput.actions != null)
        {
            var move = playerInput.actions["Move"];
            if (move != null)
            {
                move.performed += OnMove;
                move.canceled += OnMove;
            }
            else
            {
                Debug.LogError("Move アクションが見つかりません！");
            }

            // Attackアクションを登録
            var attack = playerInput.actions["Attack"];
            if (attack != null)
            {
                attack.performed += OnAttack;
            }
            else
            {
                Debug.LogError("Attack アクションが見つかりません！");
            }

            // Menuアクションを登録
            var menu = playerInput.actions["Menu"];
            if (menu != null)
            {
                menu.performed += OnMenu;
            }
            else
            {
                Debug.LogError("Menu アクションが見つかりません！");
            }
        }
    }

    void OnDisable()
    {
        // イベントを解除
        if (playerInput != null && playerInput.actions != null)
        {
            var move = playerInput.actions["Move"];
            if (move != null)
            {
                move.performed -= OnMove;
                move.canceled -= OnMove;
            }

            var attack = playerInput.actions["Attack"];
            if (attack != null)
            {
                attack.performed -= OnAttack;
            }
            var menu = playerInput.actions["Menu"];
            if (menu != null)
            {
                menu.performed -= OnMenu;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 移動処理
        MovePlayer();
    }

    /// <summary>
    /// Move入力（上下左右キー / スティック）を受け取る
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// プレイヤーを移動させる
    /// </summary>
    private void MovePlayer()
    {
        if (rb == null) return;

        // メニューが開いている場合は移動しない
        if (isMenuOpen) return;

        // 移動方向を計算（XZ平面での移動）
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        
        // 移動速度を適用
        Vector3 velocity = moveDirection * moveSpeed;
        
        // Y軸の速度は保持（重力の影響を受けるため）
        velocity.y = rb.linearVelocity.y;
        
        // Rigidbodyのvelocityを設定
        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Attack入力（決定ボタン）を受け取る
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Itemタグのオブジェクトに触れている場合
        if (currentItemObject != null)
        {
            // 判定だけ（中身は後に実装）
            Debug.Log($"Itemタグのオブジェクトに触れています: {currentItemObject.name}");
            // TODO: ここにItemタグのオブジェクトに対する処理を追加
            return;
        }
    }

    /// <summary>
    /// Menu入力（メニューボタン）を受け取る
    /// メニューの表示、非表示
    /// </summary>
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (menuCanvasInstance == null || !menuCanvasInstance.activeSelf)
        {
            ShowMenuCanvas();
        }
        else
        {
            HideMenuCanvas();
        }
    }

    /// <summary>
    /// Mmenu_Canvasを表示する（プレハブから生成）
    /// </summary>
    private void ShowMenuCanvas()
    {
        // プレハブが設定されていない場合はエラー
        if (Mmenu_Canvas == null)
        {
            Debug.LogError("Mmenu_Canvasプレハブが設定されていません。Inspectorで設定してください。");
            return;
        }

        // インスタンスが存在しない場合はプレハブから生成
        if (menuCanvasInstance == null)
        {
            menuCanvasInstance = Instantiate(Mmenu_Canvas);
            // InventManagerを取得
            inventManager = FindObjectOfType<InventManager>();
            if (inventManager == null)
            {
                Debug.LogWarning("InventManager がシーン内に見つかりません。メニューの非表示処理がスキップされます。");
            }
            Debug.Log("Mmenu_Canvasをプレハブから生成しました");
        }

        // メニューを表示
        if (menuCanvasInstance != null)
        {
            menuCanvasInstance.SetActive(true);
        }
        isMenuOpen = true;


        if (inventManager != null)
        {
            inventManager.SetInputEnabled(true);
        }

        // 移動入力をリセット（メニューが開いている間は動かないように）
        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        Debug.Log("Mmenu_Canvasを表示しました");
    }

    /// <summary>
    /// Mmenu_Canvasを非表示にする（外部から呼び出し可能）
    /// </summary>
    public void HideMenuCanvas()
    {
        if (menuCanvasInstance != null)
        {
            menuCanvasInstance.SetActive(false);
        }
        isMenuOpen = false;

        // InventManagerのメニューを非表示にする
        if (inventManager != null)
        {
            inventManager.SetInputEnabled(false);
        }

        Debug.Log("Mmenu_Canvasを非表示にしました");
    }

    /// <summary>
    /// Itemタグのオブジェクトとの接触開始
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            currentItemObject = other.gameObject;
            Debug.Log($"Itemタグのオブジェクトに接触: {currentItemObject.name}");
        }
    }

    /// <summary>
    /// Itemタグのオブジェクトとの接触終了
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            if (currentItemObject == other.gameObject)
            {
                Debug.Log($"Itemタグのオブジェクトから離脱: {currentItemObject.name}");
                currentItemObject = null;
            }
        }
    }
}
