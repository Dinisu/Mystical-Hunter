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
            if (characterData.Hp > 0)
            {
                // Moving か Acting_up のときだけ処理
                if (state == TimelineState.Moving || state == TimelineState.Acting_up)
                {
                    float currentSpeed = numericalProcessing.GetEffectiveSpeed(characterData);
                    float actionMultiplier = GetActionSpeedMultiplier();//行動速度倍率
                    currentProgress += currentSpeed * actionMultiplier * speedScale * 0.02f; // 固定時間進行


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

                    // 即時発動
                    if (IsInstantAction() && state == TimelineState.Acting_up)
                    {
                        currentProgress = actionZoneEnd;
                        Debug.Log("即時発動!");
                    }

                    // 行動発動
                    if (currentProgress >= actionZoneEnd)
                    {
                        currentProgress = actionZoneEnd;
                        state = TimelineState.Acting_up;
                        OnActionExecute?.Invoke(this);
                    }
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

    /// <summary>
    /// 発動予定のスキル、アイテムの行動速度倍率を取得する
    /// </summary>
    private float GetActionSpeedMultiplier()
    {
        // スキル優先
        if (ActivatedSkills != null)
        {
            switch (ActivatedSkills.SeeKinds)
            {
                case D_Sk_StatusData.Kinds.Fast:
                    return 2f;

                case D_Sk_StatusData.Kinds.slow:
                    return 0.6f;
            }
        }

        // アイテム
        if (ActivatedItem != null)
        {
           /* switch (ActivatedItem.SeeKinds)
            {
                case D_It_StatusData.Kinds.HP_Recovery:
                case D_It_StatusData.Kinds.MP_Recovery:
                    return 999f; // 即時発動用
            }*/
        }

        return 1f;
    }

    /// <summary>
    /// 発動予定のスキル、アイテムの即時発動判定
    /// </summary>
    private bool IsInstantAction()
    {
        if (ActivatedSkills != null)
        {
            if (ActivatedSkills.SeeKinds == D_Sk_StatusData.Kinds.Quick ||
                ActivatedSkills.SeeKinds == D_Sk_StatusData.Kinds.Defense)
                return true;
        }

        if (ActivatedItem != null)
        {
            if (ActivatedItem.SeeKinds == D_It_StatusData.Kinds.HP_Recovery ||
                ActivatedItem.SeeKinds == D_It_StatusData.Kinds.MP_Recovery)
                return true;
        }

        return false;
    }

    public void ResumeMovement()
    {
        isActionTriggered = false;
        state = TimelineState.WaitingForCommand;
        currentProgress = 0f;
    }

    public void ActionReset()
    {
        ActivatedSkills = null;
        ActivatedItem = null;
        Target_of_Action = null;
    }
}
