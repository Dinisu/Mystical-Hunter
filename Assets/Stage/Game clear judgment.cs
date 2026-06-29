using UnityEngine;
using System.Collections;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using GameConstants;
using UnityEngine.SceneManagement;

public class Gameclearjudgment : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private D_Ev_StatusData Ev_StatusData;
    [SerializeField] private GameObject ClearObject;

    [SerializeField, Header("移動先シーン")]
    private SceneName SceneName;

    private Ds_Ev_StatusDataStore ds_Ev_StatusDataStore;

    private void Awake()
    {
        ds_Ev_StatusDataStore = FindAnyObjectByType<Ds_Ev_StatusDataStore>();
    }

    void Start()
    {
        ClearObject.SetActive(false);
        StartCoroutine(ClearText());
    }

    private IEnumerator ClearText()
    {
        if (Ev_StatusData != null && Ev_StatusData.Event1)
        {
            var StopEvent = ds_Ev_StatusDataStore.FindWithName("PlayerStop");

            //プレイヤー停止イベントON
            StopEvent.Event1 = true;


            ClearObject.SetActive(true);
            yield return new WaitForSecondsRealtime(3);

            //プレイヤー停止イベントOFF
            StopEvent.Event1 = false;
            // シーン読み込み
            SceneFader.Instance.FadeToScene(SceneName.ToString());
        }
        yield return null;
    }
}
