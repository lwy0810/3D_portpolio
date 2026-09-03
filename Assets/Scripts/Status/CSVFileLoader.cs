using UnityEngine;

public class CSVFileLoader : MonoBehaviour
{
    private const string Folder = "Status/";

    /// <summary>Resources/Status/{fileName}.csv 를 문자열로 읽는다.</summary>
    public static string Load(string fileName)
    {
        TextAsset text = Resources.Load<TextAsset>(Folder + fileName);

        if (text == null)
        {
            Debug.LogError($"[CSVFileLoader] Resources/{Folder}{fileName} 을 찾을 수 없습니다.");
            return string.Empty;
        }

        return text.text;
    }

    /// <summary>헤더 이름으로 접근하는 표로 읽는다.</summary>
    public static CsvTable LoadTable(string fileName)
    {
        return CsvTable.Parse(Load(fileName));
    }

    // ── 기존 호출부 호환용 ────────────────────────────────────
    public static string OnCharacterLoadCSV(string fileName) => Load(fileName);
    public static string OnMonsterLoadCSV(string fileName) => Load(fileName);
}
