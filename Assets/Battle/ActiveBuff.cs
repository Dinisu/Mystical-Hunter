using App.BaseSystem.DataStores.ScriptableObjects.Status;

[System.Serializable]
/// <summary>
/// バフ、デバフのターン管理
/// </summary>
public class ActiveBuff<T>
{
    public T baseData;// 元データ（参照のみ）
    public int remainingTurns;// 元データ（参照のみ）

    public ActiveBuff(T data, int duration)
    {
        baseData = data;
        remainingTurns = duration;
        //remainingTurns = data.Duration; // 初期値は元データのDuration
    }
}
