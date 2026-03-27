using App.BaseSystem.DataStores.ScriptableObjects.Status;
using TMPro;
using UnityEngine;

public class StatusNumber : MonoBehaviour
{
    [SerializeField, Header("表示するキャラクターステータスデータ")]
    public D_Ch_StatusData D_status;

    [SerializeField, Header("表示するステータステキスト")]
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI EXPText;
    public TextMeshProUGUI HpText;
    public TextMeshProUGUI MpText;
    public TextMeshProUGUI SpText;
    public TextMeshProUGUI AttackText;
    public TextMeshProUGUI MagicText;
    public TextMeshProUGUI DefenceText;
    public TextMeshProUGUI MagicDefenseText;
    public TextMeshProUGUI SpeedText;

    [SerializeField, Header("表示する装備テキスト")]
    public TextMeshProUGUI WeaponText;
    public TextMeshProUGUI ArmorText;
    public TextMeshProUGUI AccessoriesText1;
    public TextMeshProUGUI AccessoriesText2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void StatusNumber_Updates() //装備などでステータスが変わった際呼ぶ
    {
        NameText.text = ($"{D_status.Name}");
        LevelText.text = ($"Lv{D_status.Level}");
        EXPText.text = ($"EXP:{D_status.Exp}/{D_status.LevelExp[D_status.Level].NeedExp}");
        HpText.text = ($"HP:{D_status.Hp}/{D_status.MaxHp}");
        MpText.text = ($"MP:{D_status.Mp}/{D_status.MaxMp}");
        SpText.text = ($"SP:{D_status.Sp}");
        AttackText.text = ($"攻撃力:{D_status.Attack}");
        MagicText.text = ($"魔力:{D_status.Magic}");
        DefenceText.text = ($"防御力:{D_status.Defense}");
        MagicDefenseText.text = ($"魔防力:{D_status.MagicDefense}");
        SpeedText.text = ($"素早さ:{D_status.Speed}");

        WeaponText.text = ($"武器: {(D_status.Weapon == null ? "なし" : D_status.Weapon.Name)}");
        ArmorText.text = ($"防具: {(D_status.Armor == null ? "なし" : D_status.Armor.Name)}");
        AccessoriesText1.text = ($"アクセサリー1: {(D_status.Accessories1 == null ? "なし" : D_status.Accessories1.Name)}");
        AccessoriesText2.text = ($"アクセサリー2: {(D_status.Accessories2 == null ? "なし" : D_status.Accessories2.Name)}");
    }

    public void StatusNumber_comparison()//ステータス比較　　作成予定ステージ後
    {

    }
}
