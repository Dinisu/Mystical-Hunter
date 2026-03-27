using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class St_CameraSample : MonoBehaviour
{
    private GameObject player;   // プレイヤーオブジェクト

    [Header("カメラのプレイヤーに対するオフセット")]
    public Vector3 cameraOffset = new Vector3(0f, 5f, -10f); // Inspector で調整可能

    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;
    private Db_Ch_StatusDataBase db_Players;

    private void Awake()
    {
        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();
    }

    void Start()
    {
        db_Players = dss_Ch_StatusDataStores.FindDatabaseWithName("Ally List");

        // Playerオブジェクトを探して取得
        player = GameObject.Find($"{db_Players.ItemList[0].name}");
    }

    void Update()
    {
        // プレイヤー位置 + 任意のオフセットをカメラ位置に設定
        transform.position = player.transform.position + cameraOffset;
    }
}
