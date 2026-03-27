using App.BaseSystem.DataStores.ScriptableObjects.Status;

[System.Serializable]
/// <summary>
/// バフ、デバフのターン管理
/// </summary>
public class ActiveBuff//クラス
{
    public D_Sk_StatusData baseData; // 元データ（参照のみ）
    public int remainingTurns;       // 現在の残りターン

    public ActiveBuff(D_Sk_StatusData data)
    {
        baseData = data;
        remainingTurns = data.Duration; // 初期値は元データのDuration
    }
}
