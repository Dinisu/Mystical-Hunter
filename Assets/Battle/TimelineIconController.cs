using UnityEngine;
using System;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine.Playables;
using System.Collections;

public class TimelineIconController : MonoBehaviour
{
    // --- 外部参照（ステータス数値処理） ---
    public NumericalProcessing numericalProcessing;
    public enum TimelineState { Moving, WaitingForCommand, Acting_up, Interrupted }

    [Header("進行設定")]
    public float currentProgress = 0f; // 0〜1
    public float actionZoneStart = 0.7f;
    public float actionZoneEnd = 1.0f;
    public TimelineState state = TimelineState.Moving;

    private float speedScale = 0.04f;  // 全体のゲージ進行係数 ゲージ速度調整係数
    private float speedModifier = 1f;  // 一時的な補正値（スロウ/ヘイストなど）

    [Header("キャラデータ参照")]
    public D_Ch_StatusData characterData;
    public GameObject characterObject;//キャラクターオブジェクト

    [SerializeField] private PlayableDirector director;

    public D_Sk_StatusData ActivatedSkills;//発動予定スキル
    public D_It_StatusData ActivatedItem;//発動予定アイテム
    public D_Ch_StatusData Target_of_Action;//行動対象

    public event Action<TimelineIconController> OnEnterActionZone;
    public event Action<TimelineIconController> OnActionExecute;

    private bool isActionTriggered = false;

    private void Awake()
    {
        // 数値処理スクリプト取得
        numericalProcessing = GameObject.Find("Numerical Processing").GetComponent<NumericalProcessing>();

        StartCoroutine(GaugeRoutine());
    }

    private IEnumerator GaugeRoutine()
    {
        // 無限ループ
        while (true)
        {
            // Moving か Acting_up のときだけ処理
            if (state == TimelineState.Moving || state == TimelineState.Acting_up)
            {
                float currentSpeed = numericalProcessing.GetEffectiveSpeed(characterData);
                currentProgress += currentSpeed * speedScale * 0.02f; // 固定時間進行

                // 行動ゾーンから出た場合、isActionTriggeredをリセット
                if (currentProgress < actionZoneStart && isActionTriggered)
                {
                    isActionTriggered = false;
                }

                // 行動ゾーン突入
                if (currentProgress >= actionZoneStart && !isActionTriggered)
                {
                    isActionTriggered = true;
                    state = TimelineState.WaitingForCommand;
                    OnEnterActionZone?.Invoke(this);//ここで処理をするように送る
                }

                // 行動発動
                if (currentProgress >= actionZoneEnd)
                {
                    currentProgress = actionZoneEnd;
                    state = TimelineState.Acting_up;
                    OnActionExecute?.Invoke(this);
                }
            }

            // 1フレーム相当の待ち時間
            yield return new WaitForSeconds(0.02f);
        }
    }

    public void BindCharacter(GameObject character)
    {
        if (director == null)
        {
            Debug.LogError("PlayableDirector が設定されていません");
            return;
        }

        // Timeline の全トラックを取得
        foreach (var output in director.playableAsset.outputs)
        {
            // 例えば AnimatorTrack をキャラの Animator にバインド
            if (output.outputTargetType == typeof(Animator))
            {
                var animator = character.GetComponent<Animator>();
                director.SetGenericBinding(output.sourceObject, animator);
            }

            // 必要なら AudioTrack, ControlTrack, ScriptTrack も追加可能
        }
    }

    public void ResumeMovement()
    {
        isActionTriggered = false;
        state = TimelineState.Moving;
        currentProgress = 0f;
    }

    /*public void ApplySpeedModifier(float multiplier, float duration)
    {
        StartCoroutine(TemporarySpeedChange(multiplier, duration));
    }

    private System.Collections.IEnumerator TemporarySpeedChange(float multiplier, float duration)//一時的に速度を変える（バフ・デバフ用)
        {
        speedModifier *= multiplier; // ここで倍率を掛ける
        yield return new WaitForSeconds(duration);
        speedModifier /= multiplier; // 元に戻す
    }*/
}
