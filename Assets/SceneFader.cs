using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using App.BaseSystem.DataStores.ScriptableObjects.Status;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    private Ds_Ev_StatusDataStore ds_Ev_StatusDataStore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        ds_Ev_StatusDataStore = FindObjectOfType<Ds_Ev_StatusDataStore>();
    }

    private void Start()
    {
        // 最初フェードイン
        fadeImage.color = new Color(0, 0, 0, 1);
        fadeImage.DOFade(0f, fadeDuration);
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        var StopEvent = ds_Ev_StatusDataStore.FindWithName("PlayerStop");

        //プレイヤー停止イベントON
        StopEvent.Event1 = true;
        // フェードアウト
        yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();

        // シーン読み込み
        SceneManager.LoadScene(sceneName);

        yield return null;

        //プレイヤー停止イベントOFF
        StopEvent.Event1 = false;
        // フェードイン
        yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
    }
}