using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;

public class Ev_Itemacquisitionevent : MonoBehaviour
{
    [SerializeField, Header("イベントデータ")]
    private D_Ev_StatusData Ev_StatusData;
    [SerializeField, Header("宝箱かどうか")]
    private bool Treasurechest;

    [SerializeField, Header("入手するアイテムデータ")]
    private D_It_StatusData It_StatusData;


    private Dss_It_StatusDataStores dss_It_StatusDataStores;//データストア
    private Db_It_StatusDataBase db_PlayerItem;//プレイヤーの所持アイテムデータ

    private void Awake()
    {
        dss_It_StatusDataStores = FindObjectOfType<Dss_It_StatusDataStores>();
    }
        void Start()
    {
        // FindDatabaseWithName を使用して Player_Item データベースを取得
        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Player_Item");

        //このオブジェクトからアイテムを既に入手済みかチェック
        Objectafteracquisition();
    }

    /// <summary>
    /// アイテムを手に入れる処理
    /// </summary>
    public void Getitems()
    {
        //このオブジェクトからアイテムを既に入手済みでないなら
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

            //イベント1をtrueにしてアイテムを入手済みにする
            Ev_StatusData.Event1 = true;
            Objectafteracquisition();
        }
    }

    private void Objectafteracquisition()
    {
        if (!Ev_StatusData.Event1)
        {
            if (Treasurechest)
            {
                //後に実装予定
                //見た目を変える
            }
            else
            {
                //このオブジェクトを削除
                Destroy(gameObject);
            }
        }
    }
}
