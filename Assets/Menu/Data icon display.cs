using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;
using UnityEngine.UI;

public class Dataicondisplay : MonoBehaviour
{

    public D_Sk_StatusData D_Sk_StatusData;

    [SerializeField] private GameObject DataIcon;
    
    public void IconDisplay()
    {
        // ▼ Image取得
        Image image = DataIcon.GetComponent<Image>();

        if (image == null)
        {
            Debug.LogWarning($"{DataIcon.name} に Image がありません");
            return;
        }

        // ▼ データが無い場合は非表示
        if (D_Sk_StatusData == null)
        {
            image.sprite = null;
            image.enabled = true;
            return;
        }

        // ▼ アイコンが存在する場合
        if (D_Sk_StatusData.DataIcon != null)
        {
            image.sprite = D_Sk_StatusData.DataIcon;
            image.enabled = true;
        }
        else
        {
            // ▼ アイコン未設定時
            image.sprite = null;
            image.enabled = true;
        }
    }
}
