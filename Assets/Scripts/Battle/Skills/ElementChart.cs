using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ElementChart.csv (행 = 공격 속성, 열 = 대상 속성) 를 담는 조회표.
/// 지금까지 Element 는 액션 바 배경색만 바꿨지만, 이 표를 거치면 데미지에 곱셈으로 들어간다.
/// </summary>
public static class ElementChart
{
    private static Dictionary<string, Dictionary<string, float>> _table
        = new Dictionary<string, Dictionary<string, float>>();

    public static void Load(CsvTable csv)
    {
        _table.Clear();

        // 첫 컬럼은 공격 속성 이름, 나머지 컬럼 헤더가 대상 속성 이름
        List<string> defenders = new List<string>();
        for (int i = 1; i < csv.Headers.Count; i++) defenders.Add(csv.Headers[i]);

        foreach (CsvTable.Row r in csv.Rows)
        {
            string atk = Key(r.GetString("attacker"));
            if (atk.Length == 0) continue;

            Dictionary<string, float> line = new Dictionary<string, float>();
            foreach (string d in defenders) line[Key(d)] = r.GetFloat(d, 1f);

            _table[atk] = line;
        }
    }

    /// <summary>공격 속성이 대상 속성에 주는 배율. 표에 없으면 1.0.</summary>
    public static float Multiplier(string attacker, string defender)
    {
        Dictionary<string, float> line;
        if (!_table.TryGetValue(Key(attacker), out line)) return 1f;

        float m;
        return line.TryGetValue(Key(defender), out m) ? m : 1f;
    }

    public static bool IsLoaded => _table.Count > 0;

    private static string Key(string s)
    {
        return string.IsNullOrEmpty(s) ? "none" : s.Trim().ToLowerInvariant();
    }
}
