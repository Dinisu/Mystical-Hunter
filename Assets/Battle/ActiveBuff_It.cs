using App.BaseSystem.DataStores.ScriptableObjects.Status;

[System.Serializable]
/// <summary>
/// バフ、デバフのターン管理　アイテム用
/// </summary>
public class ActiveBuff_It
{
    public D_It_StatusData baseData; // 元データ（参照のみ）
    public int remainingTurns;       // 現在の残りターン

    public ActiveBuff_It(D_It_StatusData data)
    {
        baseData = data;
        remainingTurns = data.Duration; // 初期値は元データのDuration
    }
}
