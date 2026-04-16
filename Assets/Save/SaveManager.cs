using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        LoadDataProcessing();
        Debug.Log($"ロード完了: {path}");
    }

    /// <summary>
    /// セーブするデータの設定処理
    /// </summary>
    private void SaveDataProcessing()
    {
        //リセット
        save.AlliesStatus.Clear();
        save.SkillStatus.Clear();
        save.ItemStatus.Clear();
        save.EventStatus.Clear();

        //------------------------
        // 基本データ
        //------------------------

        // 所持金
        save.PlayerMoney = GameManager.Instance.PlayerMoney;

        //プレイ時間
        save.PlayTime = GameManager.Instance.PlayTime;

        // シーン名
        save.CurrentScene = GameManager.Instance.CurrentSceneName;

        // プレイヤー位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Vector3 pos = player.transform.position;

            save.PlayerPosX = pos.x;
            save.PlayerPosY = pos.y;
            save.PlayerPosZ = pos.z;
        }
        else
        {
            Debug.LogWarning("Playerが見つかりませんでした");
        }


        //------------------------
        // 味方データ
        //------------------------
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

            data.Weapon = allyData.Weapon != null ? allyData.Weapon.Id : -1;
            data.Armor = allyData.Armor != null ? allyData.Armor.Id : -1;
            data.Accessories1 = allyData.Accessories1 != null ? allyData.Accessories1.Id : -1;
            data.Accessories2 = allyData.Accessories2 != null ? allyData.Accessories2.Id : -1;
            //-1 = 装備なし

            save.AlliesStatus.Add(data);
        }

        //------------------------
        // スキルデータ
        //------------------------
        var db_SkillData = db_skillDataBase.ItemList;
        foreach (var skillData in db_SkillData)
        {
            SaveData.SkillData data = new SaveData.SkillData();

            data.Id = skillData.Id;
            data.UnlockSkills = skillData.Unlock;

            save.SkillStatus.Add(data);
        }

        //------------------------
        // アイテムデータ
        //------------------------
        var db_ItemData = db_PlayerItem.ItemList;
        foreach (var itemData in db_ItemData)
        {
            SaveData.ItemData data = new SaveData.ItemData();

            data.Id = itemData.Id;
            data.Number = itemData.Number;

            save.ItemStatus.Add(data);
        }

        //------------------------
        // イベントデータ
        //------------------------
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

    /// <summary>
    /// ロードしたセーブデータをゲーム内へ反映する
    /// </summary>
    private void LoadDataProcessing()
    {
        //------------------------
        // 基本データ
        //------------------------

        GameManager.Instance.PlayerMoney = save.PlayerMoney;
        GameManager.Instance.PlayTime = save.PlayTime;

        GameManager.Instance.PlayerPosition = new Vector3
        (
            save.PlayerPosX,
            save.PlayerPosY,
            save.PlayerPosZ
        );

        GameManager.Instance.ShouldRestorePlayerPosition = save.ShouldRestorePlayerPosition;

        //------------------------
        // 味方データ
        //------------------------

        foreach (var loadedAlly in save.AlliesStatus)
        {
            var allyData = db_allyDataBase.ItemList.Find(x => x.Id == loadedAlly.Id);

            if (allyData == null)
            {
                Debug.LogWarning($"味方ID:{loadedAlly.Id} が見つかりません");
                continue;
            }

            allyData.Level = loadedAlly.Level;
            allyData.Exp = loadedAlly.Exp;
            allyData.Sp = loadedAlly.Sp;

            allyData.MaxHp = loadedAlly.MaxHp;
            allyData.Hp = loadedAlly.Hp;

            allyData.MaxMp = loadedAlly.MaxMp;
            allyData.Mp = loadedAlly.Mp;

            allyData.Attack = loadedAlly.Attack;
            allyData.Magic = loadedAlly.Magic;

            allyData.Defense = loadedAlly.Defense;
            allyData.MagicDefense = loadedAlly.MagicDefense;

            allyData.Speed = loadedAlly.Speed;
            allyData.CriticalRate = loadedAlly.CriticalRate;

            // 装備復元（ID→装備DB検索）
            allyData.Weapon = db_PlayerItem.ItemList.Find(x => x.Id == loadedAlly.Weapon);

            allyData.Armor = db_PlayerItem.ItemList.Find(x => x.Id == loadedAlly.Armor);

            allyData.Accessories1 = db_PlayerItem.ItemList.Find(x => x.Id == loadedAlly.Accessories1);

            allyData.Accessories2 = db_PlayerItem.ItemList.Find(x => x.Id == loadedAlly.Accessories2);
        }


        //------------------------
        // スキルデータ
        //------------------------

        foreach (var loadedSkill in save.SkillStatus)
        {
            var skillData = db_skillDataBase.ItemList.Find(x => x.Id == loadedSkill.Id);

            if (skillData != null)
            {
                skillData.Unlock = loadedSkill.UnlockSkills;
            }
        }


        //------------------------
        // アイテムデータ
        //------------------------

        foreach (var loadedItem in save.ItemStatus)
        {
            var itemData = db_PlayerItem.ItemList.Find(x => x.Id == loadedItem.Id);

            if (itemData != null)
            {
                itemData.Number = loadedItem.Number;
            }
        }


        //------------------------
        // イベントデータ
        //------------------------

        foreach (var loadedEvent in save.EventStatus)
        {
            var eventData = db_eventDataBase.ItemList.Find(x => x.Id == loadedEvent.Id);

            if (eventData != null)
            {
                eventData.Event1 = loadedEvent.EventFlag1;
                eventData.Event2 = loadedEvent.EventFlag2;
                eventData.Event3 = loadedEvent.EventFlag3;
            }
        }

        //------------------------
        // 最後にシーン移動
        //------------------------
        SceneManager.LoadScene(save.CurrentScene.ToString());

    }
    public void InitSaveData()
    {

    }
}
