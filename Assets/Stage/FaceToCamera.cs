using UnityEngine;

/// <summary>
/// このスクリプトをスプライト付きのオブジェクトにアタッチすると、
/// 現在アクティブなカメラに常に正対するようになります。
/// Billboard 表現に適しています。
/// </summary>
public class FaceToCamera : MonoBehaviour
{
    [Tooltip("カメラを自動で検索する（Sceneに複数ある場合は false 推奨）")]
    public bool autoFindCamera = true;

    [Tooltip("明示的に指定したカメラ（autoFindCamera が false のとき使用）")]
    public Camera targetCamera;

    private void Start()
    {
        if (autoFindCamera)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        transform.forward = Camera.main.transform.forward;
    }
}
