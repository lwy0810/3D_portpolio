using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSVFileLoader : MonoBehaviour
{
    public static string OnCharacterLoadCSV(string fileName)
    {
        string filePath = "Status/";
        filePath = string.Concat(filePath, fileName);

        TextAsset text = Resources.Load<TextAsset>(filePath);

        return text.text;
    }

    public static string OnMonsterLoadCSV(string fileName)
    {
        string filePath = "Status/";
        filePath = string.Concat(filePath, fileName);

        TextAsset text = Resources.Load<TextAsset>(filePath);

        return text.text;
    }


}
