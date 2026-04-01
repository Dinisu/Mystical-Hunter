using System.Collections.Generic;
using UnityEngine;
using GameConstants;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //シングルトンの宣言
    public static GameManager Instance = null;

    //戦闘復帰用
    public SceneName SceneName;//移動前のシーン名を格納
    // プレイヤーの位置情報を保存
    public Vector3 PlayerPosition = Vector3.zero;
    public bool ShouldRestorePlayerPosition = false;

    [SerializeField, Header("所持金")]
    public int PlayerMoney;

    // エンカウントした敵の情報を保存
    public List<D_Ch_StatusData> EncounteredEnemys = new List<D_Ch_StatusData>();

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
        SceneName = sceneName;
        PlayerPosition = playerTransform.position;
        ShouldRestorePlayerPosition = true;
    }

    // プレイヤーの位置情報をクリア
    public void ClearPlayerPosition()
    {
        PlayerPosition = Vector3.zero;
        ShouldRestorePlayerPosition = false;
    }

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

        ShouldRestorePlayerPosition = false;
    }
}
