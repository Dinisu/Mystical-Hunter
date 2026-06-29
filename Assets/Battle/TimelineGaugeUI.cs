using UnityEngine;
using UnityEngine.UI;

public class TimelineGaugeUI : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField, Header("速度関係")] private TimelineIconController iconController;//動かしたいキャラクターの速度関係
    [SerializeField, Header("ゲージ")] private RectTransform gaugeBar;  // ゲージ全体のRectTransform（長さ1200）
    [SerializeField, Header("キャラアイコン")] private RectTransform iconTransform; // キャラアイコンのUI位置
    //[SerializeField, Header("威力、効力1")] private Image gaugeFillImage; // ゲージバーにfillAmountを使う場合

    private float gaugeLength;

    void Start()
    {
        if (gaugeBar == null)
        {
            GameObject gaugeObj = GameObject.Find("Gauge");
            if (gaugeObj != null)
                gaugeBar = gaugeObj.GetComponent<RectTransform>();
            else
                Debug.LogError("Gauge オブジェクトがシーンにありません！");
        }

        // ゲージ全体の実サイズを取得（横方向）
        gaugeLength = gaugeBar.rect.width;
    }

    void Update()
    {
        if (iconController == null || gaugeBar == null) return;
        // 進行率を取得（0〜1）
        float progressRate = Mathf.Clamp01(iconController.currentProgress / iconController.actionZoneEnd);

        // アイコン位置で進行を表現する
        if (iconTransform != null)
        {
            float newX = Mathf.Lerp(0, gaugeLength, progressRate);
            Vector2 anchoredPos = iconTransform.anchoredPosition;
            anchoredPos.x = newX;
            iconTransform.anchoredPosition = anchoredPos;
        }
    }
}
