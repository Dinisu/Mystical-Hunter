using App.BaseSystem.DataStores.ScriptableObjects.Status;
using TMPro;
using UnityEngine;
using System.Collections;

public class Ev_Itemacquisitionevent : MonoBehaviour
{
    [SerializeField, Header("イベントデータ")]
    private D_Ev_StatusData Ev_StatusData;
    [SerializeField, Header("宝箱かどうか")]
    private bool Treasurechest;

    [SerializeField, Header("入手するアイテムデータ")]
    private D_It_StatusData It_StatusData;
    [SerializeField, Header("入手音")]
    private AudioClip payment;

    [SerializeField, Header("獲得アイテムテキスト")]
    public TextMeshProUGUI Acquireditemtext;


    private Dss_It_StatusDataStores dss_It_StatusDataStores;//データストア
    private Db_It_StatusDataBase db_PlayerItem;//プレイヤーの所持アイテムデータ

    private void Awake()
    {
        dss_It_StatusDataStores = FindObjectOfType<Dss_It_StatusDataStores>();

        // シーン上の "AcquireditemText" を自動参照（未設定の場合）
        if (Acquireditemtext == null)
        {
            var textObj = GameObject.Find("AcquireditemText");
            if (textObj != null)
            {
                Acquireditemtext = textObj.GetComponent<TextMeshProUGUI>();
                if (Acquireditemtext != null)
                {
                    var parentObj = Acquireditemtext.transform.parent != null ? Acquireditemtext.transform.parent.gameObject : null;
                    if (parentObj != null)
                    {
                        parentObj.SetActive(false);
                    }
                }
            }
        }
    }
    void Start()
    {
        // FindDatabaseWithName を使用して Player_Item データベースを取得
        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Player_Item");

        //このオブジェクトからアイテムを既に入手済みかチェック
        if (Ev_StatusData.Event1!)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// アイテムを手に入れる処理
    /// </summary>
    public void Getitems()
    {
        //アイテムが入手済みかチェック
        if (!Ev_StatusData.Event1)
        {
            // 既に所持リストに同一アイテムがあるなら個数加算
            D_It_StatusData existingItem = null;
            foreach (var owned in db_PlayerItem.ItemList)
            {
                if (owned == null) continue;
                if (owned == It_StatusData || owned.Id == It_StatusData.Id || owned.Name == It_StatusData.Name)
                {
                    existingItem = owned;
                    break;
                }
            }

            if (existingItem != null)
            {
                existingItem.Number += 1;
            }
            else
            {
                // 新規入手（ScriptableObjectアセット参照を追加）
                if (It_StatusData.Number <= 0)
                {
                    It_StatusData.Number = 1;
                }
                else
                {
                    It_StatusData.Number += 1;
                }
                db_PlayerItem.ItemList.Add(It_StatusData);
            }

            Debug.Log($"{It_StatusData}を手に入れた");

            if (GameManager.Instance.audioSource != null && payment != null)
            {
                GameManager.Instance.audioSource.PlayOneShot(payment); // 決定音
            }

            //イベント1をtrueにしてアイテムを入手済みにする
            Ev_StatusData.Event1 = true;
        }
        Objectafteracquisition();
    }

    private void Objectafteracquisition()
    {
        if (Ev_StatusData.Event1!)
        {
            if (Treasurechest)
            {
                //後に実装予定
                //見た目を変える

                //テキストの表示
                StartCoroutine(ShowAcquiredTextAnd($"{It_StatusData.Name}：1"));
            }
            else
            {
                //テキストの表示
                StartCoroutine(ShowAcquiredTextAnd($"{It_StatusData.Name}：1"));
            }
        }
    }

    /// <summary>
    /// 取得アイテム名を一定時間表示
    /// </summary>
    private IEnumerator ShowAcquiredTextAnd(string message)
    {
        if (Acquireditemtext != null)
        {
            // このオブジェクトはアクティブのまま、見た目/当たり判定のみ無効化
            HideObjectOnly();
            Acquireditemtext.text = message;
            var parentObj = Acquireditemtext.transform.parent != null ? Acquireditemtext.transform.parent.gameObject : null;
            if (parentObj != null)
            {
                parentObj.SetActive(true);
            }
            yield return new WaitForSeconds(1.5f);
            if (parentObj != null)
            {
                parentObj.SetActive(false);
            }
        }
        //このオブジェクトを削除
        Destroy(gameObject);
    }

    /// <summary>
    /// 見た目と当たり判定だけ無効化
    /// （GameObject自体はActiveのまま）
    /// </summary>
    public void HideObjectOnly()
    {
        // ▼ Renderer無効化（見た目）
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        // ▼ Collider無効化（当たり判定）
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }
}
