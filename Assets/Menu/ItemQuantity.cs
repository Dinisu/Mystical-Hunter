using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;
using TMPro;
using System.Linq;
using static UnityEditor.Progress;

public class ItemQuantity : MonoBehaviour
{
    private TextMeshProUGUI itemQuantityText;
    private TextMeshProUGUI itemText;

    public D_It_StatusData D_It_StatusData;

    private Ds_It_StatusDataStore ds_It_StatusDataStore;
    private Db_Ch_StatusDataBase db_Players;

    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;

    private void Awake()
    {
        ds_It_StatusDataStore = FindObjectOfType<Ds_It_StatusDataStore>();
        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();

        // "(Clone)" を除いて名前を取得
        string originalName = gameObject.name.Replace("(Clone)", "").Trim();
        // オブジェクト名でFindWithNameを使ってデータを取得
        D_It_StatusData = ds_It_StatusDataStore.FindWithName(originalName);

        itemText = GetComponent<TextMeshProUGUI>();
        // 子オブジェクトのQuantityTextをitemQuantityTextに設定
        Transform quantityTextChild = transform.Find("QuantityText");
        if (quantityTextChild != null)
        {
            itemQuantityText = quantityTextChild.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"QuantityText 子オブジェクトが見つかりません: {gameObject.name}");
        }

        db_Players = dss_Ch_StatusDataStores.FindDatabaseWithName("Ally List");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 装備中かどうか判定  db_Players.ItemList.Any(...)：誰か1人でも条件に当てはまるかどうか
        bool isEquipped = db_Players.ItemList.Any(player =>
            player.Weapon == D_It_StatusData ||
            player.Armor == D_It_StatusData ||
            player.Accessories1 == D_It_StatusData ||
            player.Accessories2 == D_It_StatusData
        );


        itemText.text = ($" {D_It_StatusData.Name}");

        if (isEquipped)
        {
            itemQuantityText.text = "装備中";
        }
        else
        {
            // 個数表示（装備品は表示しない）
            switch (D_It_StatusData.SeeKinds)
            {
                case D_It_StatusData.Kinds.Weapon:
                case D_It_StatusData.Kinds.Armor:
                case D_It_StatusData.Kinds.Accessories:
                    itemQuantityText.text = "";
                    break;
                default:
                    itemQuantityText.text = $"{D_It_StatusData.Number}";
                    break;
            }
        }
    }

    public void QuantityUpdate()
    {
        // 装備中かどうか判定
        bool isEquipped = db_Players.ItemList.Any(player =>
            player.Weapon == D_It_StatusData ||
            player.Armor == D_It_StatusData ||
            player.Accessories1 == D_It_StatusData ||
            player.Accessories2 == D_It_StatusData
        );


        itemText.text = ($" {D_It_StatusData.Name}");

        if (isEquipped)
        {
            itemQuantityText.text = "装備中";
        }
        else
        {
            // 個数表示（装備品は表示しない）
            switch (D_It_StatusData.SeeKinds)
            {
                case D_It_StatusData.Kinds.Weapon:
                case D_It_StatusData.Kinds.Armor:
                case D_It_StatusData.Kinds.Accessories:
                    itemQuantityText.text = "";
                    break;
                default:
                    itemQuantityText.text = $" {D_It_StatusData.Number}";
                    break;
            }
        }
    }
}
