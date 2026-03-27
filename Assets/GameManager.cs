using System.Collections.Generic;
using UnityEngine;
using GameConstants;
using App.BaseSystem.DataStores.ScriptableObjects.Status;

public class GameManager : MonoBehaviour
{
    //シングルトンの宣言
    public static GameManager Instance = null;

    //戦闘復帰用
    public SceneName SceneName;//移動前のシーン名を格納
    // プレイヤーの位置情報を保存
    public Vector3 PlayerPosition = Vector3.zero;
    public bool ShouldRestorePlayerPosition = false;

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

    // エンカウントした敵の情報をクリア
    public void ClearEncounteredEnemies()
    {
        foreach (var enemy in EncounteredEnemys)
        {
            enemy.Hp = enemy.MaxHp;
        }
        EncounteredEnemys.Clear();
    }

    // プレイヤーの位置情報をクリア
    public void ClearPlayerPosition()
    {
        PlayerPosition = Vector3.zero;
        ShouldRestorePlayerPosition = false;
    }
}
