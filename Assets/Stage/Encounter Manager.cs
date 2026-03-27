using App.BaseSystem.DataStores.ScriptableObjects.Status;
using GameConstants;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : MonoBehaviour
{
    [SerializeField, Header("エンカウントする敵のリスト")]
    private Db_Ch_StatusDataBase EncounterEnemys;

    [SerializeField, Header("エンカウントエリア")]
    private List<BoxCollider> EncounterArea = new List<BoxCollider>();

    [SerializeField, Header("毎秒エンカウント確率(%)")]
    [Range(0f, 100f)]
    private float encounterRatePerSecond = 3f;

    /* 
       | encounterRatePerSecond | 体感             |
       |------------------------|------------------|
       | 1%                     | かなり低い       |
       | 3%                     | 普通のフィールド |
       | 5%                     | やや多い         |
       | 8%以上                 | ダンジョン       |
    */


    private enum BattleStage
    {
        BattleScene_Forest
    }
    [SerializeField, Header("このシーン")]
    private SceneName SceneName;
    [SerializeField, Header("戦闘する場所")]
    private BattleStage battleStage;

    private float encounterTimer = 0f;
    private bool isPlayerInside = false;
    private Transform playerTransform;
    private Vector3 lastPlayerPos;

    private int insideAreaCount = 0;
    private float encounterGauge = 0f;

    /*
   　 ダッシュ中はエンカウント率 UP
　　　敵テーブルをエリアごとに切替
     */
    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        lastPlayerPos = playerTransform.position;
    }

    private void Update()
    {
        if (!isPlayerInside) return;

        bool isMoving = Vector3.Distance(playerTransform.position, lastPlayerPos) > 0.01f;
        lastPlayerPos = playerTransform.position;
        if (!isMoving) return;

        Debug.Log("エンカウントエリアでプレイヤーが動いている");

        encounterGauge += encounterRatePerSecond * Time.deltaTime;

        if (encounterGauge >= 100f)
        {
            encounterGauge = 0f;
            StartEncounter();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"エンカウントエリア");

        insideAreaCount++;
        isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"エンカウントエリア");

        insideAreaCount--;

        if (insideAreaCount <= 0)
        {
            insideAreaCount = 0;
            isPlayerInside = false;
            encounterTimer = 0f;
        }
    }


    private void StartEncounter()
    {
        Debug.Log("★★★ StartEncounter 呼ばれた ★★★");
        isPlayerInside = false;

        // ▼ プレイヤー位置保存
        GameManager.Instance.SceneName = SceneName;
        GameManager.Instance.PlayerPosition = playerTransform.position;
        GameManager.Instance.ShouldRestorePlayerPosition = true;

        // ▼ 敵をランダムで 1～3 体（重複なし）
        GameManager.Instance.EncounteredEnemys = new List<D_Ch_StatusData>();

        int enemyCount = Random.Range(1, 4);
        List<D_Ch_StatusData> pool = new List<D_Ch_StatusData>(EncounterEnemys.ItemList);

        for (int i = 0; i < enemyCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            GameManager.Instance.EncounteredEnemys.Add(pool[index]);
            pool.RemoveAt(index);
        }

        Debug.Log($"エンカウント！ 敵数: {GameManager.Instance.EncounteredEnemys.Count}");

        // ▼ 戦闘シーンへ
        SceneManager.LoadScene(battleStage.ToString());
    }
}
