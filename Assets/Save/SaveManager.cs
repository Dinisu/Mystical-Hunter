using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class SaveManager : MonoBehaviour
{
    string filePath;
    SaveData save;

    //セーブデータアクセス用
    public static SaveData SaveData => Instance.save;

    //インスタンスの宣言
    public static SaveManager Instance = null;

    //データストア
    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;
    private Dss_It_StatusDataStores dss_It_StatusDataStores;
    private Dss_Ev_StatusDataStores dss_Ev_StatusDataStores;
    private Dss_Sk_StatusDataStores dss_Sk_StatusDataStores;

    private Db_It_StatusDataBase db_PlayerItem;
    private Db_Ch_StatusDataBase db_allyDataBase;
    private Db_Sk_StatusDataBase db_skillDataBase;
    private Db_Ev_StatusDataBase db_eventDataBase;

    //起動処理
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;  //自分自身を設定
            DontDestroyOnLoad(this.gameObject);//破壊されないように

            //セーブデータ
            save = new SaveData();
        }
        else
        {
            Destroy(this.gameObject);
        }

        dss_Ch_StatusDataStores = FindObjectOfType<Dss_Ch_StatusDataStores>();
        dss_It_StatusDataStores = FindObjectOfType<Dss_It_StatusDataStores>();
        dss_Ev_StatusDataStores = FindObjectOfType<Dss_Ev_StatusDataStores>();
        dss_Sk_StatusDataStores = FindObjectOfType<Dss_Sk_StatusDataStores>();

        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Player_Item");
        db_allyDataBase = dss_Ch_StatusDataStores.FindDatabaseWithName("Ally List");
        db_skillDataBase = dss_Sk_StatusDataStores.FindDatabaseWithName("For search_Skill");
        db_eventDataBase = dss_Ev_StatusDataStores.FindDatabaseWithName("For search_Event");
    }

    /// <summary>
    /// セーブデータの保存先
    /// 確認しやすいようにUnityエディタ上ではプロジェクト内にビルド後は正規の場所に
    /// </summary>
    private string GetFilePath(int saveNumber)
    {
#if UNITY_EDITOR
        return filePath = Application.dataPath + $"/Save/LocationToStoreData/savedata_{saveNumber}.json";
#else
    return filePath = Application.persistentDataPath + $"/savedata_{saveNumber}.json";
#endif
    }
    //セーブ実行
    public void Save(int saveNumber)
    {
        SaveDataProcessing(); // セーブ前に現在データを反映

        //セーブデータをJSON形式に変換
        string json = JsonUtility.ToJson(save, true);

        string path = GetFilePath(saveNumber);

        //ファイルに書き込む
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.Write(json);
        }

        Debug.Log($"セーブデータの場所:{path}");
        Debug.Log($"セーブ完了: {path}");
    }

    public void Load(int saveNumber)
    {
        string path = GetFilePath(saveNumber);

        if (!File.Exists(path))
        {
            Debug.Log("セーブデータなし。初期化します");
            InitSaveData();
            return;
        }

        using (StreamReader reader = new StreamReader(path))
        {
            string data = reader.ReadToEnd();
            save = JsonUtility.FromJson<SaveData>(data);
        }
        Debug.Log($"ロード完了: {path}");
    }

    private void SaveDataProcessing()
    {
        //リセット
        save.AlliesStatus.Clear();
        save.SkillStatus.Clear();
        save.ItemStatus.Clear();
        save.EventStatus.Clear();

        // 所持金
        save.PlayerMoney = GameManager.Instance.PlayerMoney;

        // シーン名
        save.CurrentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // プレイヤー位置
        Vector3 pos = GameObject.FindGameObjectWithTag("Player").transform.position;

        save.PlayerPosX = pos.x;
        save.PlayerPosY = pos.y;
        save.PlayerPosZ = pos.z;

        // フラグ
        save.ShouldRestorePlayerPosition = GameManager.Instance.ShouldRestorePlayerPosition;

        var db_allyData = db_allyDataBase.ItemList;
        foreach (var allyData in db_allyData)
        {
            SaveData.AlliesData data = new SaveData.AlliesData();

            data.Id = allyData.Id;
            data.Level = allyData.Level;
            data.Exp = allyData.Exp;
            data.Sp = allyData.Sp;

            data.MaxHp = allyData.MaxHp;
            data.Hp = allyData.Hp;

            data.MaxMp = allyData.MaxMp;
            data.Mp = allyData.Mp;

            data.Attack = allyData.Attack;
            data.Magic = allyData.Magic;

            data.Defense = allyData.Defense;
            data.MagicDefense = allyData.MagicDefense;

            data.Speed = allyData.Speed;
            data.CriticalRate = allyData.CriticalRate;

            data.Weapon = allyData.Weapon.Id;
            data.Armor = allyData.Armor.Id;
            data.Accessories1 = allyData.Accessories1.Id;
            data.Accessories2 = allyData.Accessories2.Id;

            save.AlliesStatus.Add(data);
        }

        var db_SkillData = db_skillDataBase.ItemList;
        foreach (var skillData in db_SkillData)
        {
            SaveData.SkillData data = new SaveData.SkillData();

            data.Id = skillData.Id;
            data.UnlockSkills = skillData.Unlock;

            save.SkillStatus.Add(data);
        }

        var db_ItemData = db_PlayerItem.ItemList;
        foreach (var itemData in db_ItemData)
        {
            SaveData.ItemData data = new SaveData.ItemData();

            data.Id = itemData.Id;
            data.Number = itemData.Number;

            save.ItemStatus.Add(data);
        }

        var db_EventData = db_eventDataBase.ItemList;
        foreach (var eventData in db_EventData)
        {
            SaveData.EventData data = new SaveData.EventData();

            data.Id = eventData.Id;
            data.EventFlag1 = eventData.Event1;
            data.EventFlag2 = eventData.Event2;
            data.EventFlag3 = eventData.Event3;

            save.EventStatus.Add(data);
        }
    }

    public void InitSaveData()
    {

    }
}
