using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;
using TMPro;

public class SkillQuantity : MonoBehaviour
{
    private TextMeshProUGUI skillQuantityText;
    private TextMeshProUGUI skillText;

    public D_Sk_StatusData D_Sk_StatusData;

    private Ds_Sk_StatusDataStore ds_Sk_StatusDataStore;


    private void Awake()
    {
        ds_Sk_StatusDataStore = FindObjectOfType<Ds_Sk_StatusDataStore>();

        skillText = GetComponent<TextMeshProUGUI>();
        // 子オブジェクトのQuantityTextをitemQuantityTextに設定
        Transform quantityTextChild = transform.Find("QuantityText");
        if (quantityTextChild != null)
        {
            skillQuantityText = quantityTextChild.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"QuantityText 子オブジェクトが見つかりません: {gameObject.name}");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // "(Clone)" を除いて名前を取得
        string originalName = gameObject.name.Replace("(Clone)", "").Trim();
        // オブジェクト名でFindWithNameを使ってデータを取得
        D_Sk_StatusData = ds_Sk_StatusDataStore.FindWithName(originalName);

        UpdateTexts();
    }

    public void QuantityUpdate()
    {
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        if (D_Sk_StatusData == null)
        {
            skillText.text = "";
            if (skillQuantityText != null)
            {
                skillQuantityText.text = "";
            }
            return;
        }

        skillText.text = $" {D_Sk_StatusData.Name}";

        if (skillQuantityText != null)
        {
            skillQuantityText.text = $"MP {D_Sk_StatusData.MpConsumption}";
        }//MPを効果説明に移してここは再発動のクールタイムにするかもその場合
        //QuantityUpdateをクールタイム更新時の処理で呼び出す用に
    }
}
