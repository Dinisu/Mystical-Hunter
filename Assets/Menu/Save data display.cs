using TMPro;
using UnityEngine;
using System.IO;

public class Savedatadisplay : MonoBehaviour
{
    [Header("セーブデータの番号")]
    public int SaveNumber;

    [SerializeField, Header("表示するテキスト")]
    private TextMeshProUGUI StatusText1;
    [SerializeField]
    private TextMeshProUGUI StatusText2;
    void Start()
    {
        DisplaySaveData();
    }

    public void DisplaySaveData()
    {
        string path = SaveManager.Instance.GetFilePath(SaveNumber);

        // セーブデータが無い場合
        if (!File.Exists(path))
        {
            StatusText1.text = ($"セーブデータ{SaveNumber}\n" + "データ無し");
            StatusText2.text = ("");
            return;
        }

        // JSON読み込み
        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            StatusText1.text = ($"セーブデータ{SaveNumber}\n"+"データ無し");
            StatusText2.text = ("");
            return;
        }

        // ▼ 味方1人目を表示
        if (data.AlliesStatus.Count == 0)
        {
            StatusText1.text = "NO ALLY DATA";
            return;
        }

        // ▼ プレイ時間
        string playTime = FormatPlayTime(data.PlayTime);

        string allyText = "";

        //▼ 味方全員を表示する
        foreach (var ally in data.AlliesStatus)
        {
            string name = GetAllyNameById(ally.Id);
            allyText += ($"{name} Lv.{ally.Level}/");
        }

        // ▼ 表示
        StatusText1.text =($"セーブデータ{SaveNumber}\n" +$"{allyText}");
        StatusText2.text = ($"プレイ時間:{playTime}");
    }

    // ID → 名前取得
    private string GetAllyNameById(int id)
    {
        var db = SaveManager.Instance.db_allyDataBase.ItemList;

        var ally = db.Find(x => x.Id == id);

        if (ally != null)
        {
            return ally.Name;
        }

        return "名無し";
    }

    // プレイ時間フォーマット
    private string FormatPlayTime(float time)
    {
        int hours = Mathf.FloorToInt(time / 3600);
        int minutes = Mathf.FloorToInt((time % 3600) / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}