using System.Collections.Generic;
using UnityEngine;
using GameConstants;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //シングルトンの宣言
    public static GameManager Instance = null;

    [Header("現在のシーン名")]
    public SceneName CurrentSceneName;
    [Header("前のシーン名")]
    public SceneName PreviousSceneName;//移動前のシーン名を格納

    // プレイヤーの位置情報を保存
    public Vector3 PlayerPosition = Vector3.zero;
    public bool ShouldRestorePlayerPosition = false;

    [Header("所持金")]
    public int PlayerMoney;

    // エンカウントした敵の情報を保存
    public List<D_Ch_StatusData> EncounteredEnemys = new List<D_Ch_StatusData>();

    [Header("プレイ時間")]
    public float PlayTime = 0f;

    [Header("選択、決定音、キャンセル")]
    public AudioClip choice;
    public AudioClip decision;
    public AudioClip cancel;
    public AudioSource audioSource;

    //初期化処理
    private void Awake()
    {
        //シングルトンがあるかどうかのチェック
        if (Instance == null)
        {
            Instance = this;//インスタンスを設定
            DontDestroyOnLoad(this.gameObject);//破棄されないようにする
        }
        else
        {
            Destroy(this.gameObject);//nullでなければ削除
        }

        // AudioSource の用意（Inspector未設定なら取得/追加）
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Update()
    {
        PlayTime += Time.deltaTime;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //現在のシーン名取得
        if (System.Enum.TryParse(scene.name, out SceneName sceneName))
        {
            CurrentSceneName = sceneName;
        }

        RestorePlayerPosition();
    }

    // エンカウントした敵の情報をクリア
    public void ClearEncounteredEnemies()
    {
        foreach (var enemy in EncounteredEnemys)
        {
            enemy.Hp = enemy.MaxHp;
        }
        EncounteredEnemys.Clear();
    }

    // プレイヤーの位置情報を保存
    public void SavePlayerPosition(SceneName sceneName, Transform playerTransform)
    {
        PreviousSceneName = sceneName;
        PlayerPosition = playerTransform.position;
        ShouldRestorePlayerPosition = true;
    }

    // プレイヤーの位置情報をクリア
    public void ClearPlayerPosition()
    {
        PlayerPosition = Vector3.zero;
        ShouldRestorePlayerPosition = false;
    }

    /// <summary>
    /// プレイヤーの位置を復元
    /// </summary>
    public void RestorePlayerPosition()
    {
        if (!ShouldRestorePlayerPosition)
            return;

        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("Player が見つかりません");
            return;
        }

        player.transform.position = PlayerPosition;

        Debug.Log($"位置復元: {PlayerPosition}");

        ShouldRestorePlayerPosition = false;
    }
}
