using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// 헤더 이름으로 접근하는 CSV 리더.
///
/// 기존 파싱은 text.Split(',') 후 고정 인덱스([10] = atk)를 썼기 때문에
/// 컬럼을 하나 추가하면 그 뒤 전부가 밀렸다. 이 클래스는 헤더 이름으로 찾으므로
/// 컬럼 순서를 바꾸거나 새 컬럼을 끼워 넣어도 코드를 고치지 않는다.
///
/// 지원 : CRLF / LF, UTF-8 BOM, 빈 행 스킵, 셀 앞뒤 공백 제거.
/// 미지원 : 셀 안의 콤마와 따옴표. 그래서 목록은 '|' 로 구분한다 (effectIds).
/// </summary>
public class CsvTable
{
    public class Row
    {
        private readonly Dictionary<string, string> _cells;
        public int LineNumber { get; private set; }

        public Row(Dictionary<string, string> cells, int lineNumber)
        {
            _cells = cells;
            LineNumber = lineNumber;
        }

        public bool Has(string column)
        {
            string v;
            return _cells.TryGetValue(column, out v) && v.Length > 0;
        }

        public string GetString(string column, string fallback = "")
        {
            string v;
            if (_cells.TryGetValue(column, out v) && v.Length > 0) return v;
            return fallback;
        }

        public int GetInt(string column, int fallback = 0)
        {
            int r;
            if (int.TryParse(GetString(column), NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out r)) return r;
            return fallback;
        }

        public float GetFloat(string column, float fallback = 0f)
        {
            float r;
            if (float.TryParse(GetString(column), NumberStyles.Float,
                               CultureInfo.InvariantCulture, out r)) return r;
            return fallback;
        }

        public bool GetBool(string column, bool fallback = false)
        {
            string v = GetString(column).ToUpperInvariant();
            if (v == "TRUE" || v == "1" || v == "Y") return true;
            if (v == "FALSE" || v == "0" || v == "N") return false;
            return fallback;
        }

        /// <summary>'|' 로 구분된 정수 목록. 빈 셀이면 길이 0 배열.</summary>
        public int[] GetIntList(string column)
        {
            string raw = GetString(column);
            if (raw.Length == 0) return _empty;

            string[] parts = raw.Split('|');
            List<int> list = new List<int>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                int v;
                if (int.TryParse(parts[i].Trim(), out v)) list.Add(v);
            }
            return list.ToArray();
        }

        private static readonly int[] _empty = new int[0];
    }

    public List<string> Headers { get; private set; }
    public List<Row> Rows { get; private set; }

    public static CsvTable Parse(string text)
    {
        CsvTable t = new CsvTable();
        t.Headers = new List<string>();
        t.Rows = new List<Row>();

        if (string.IsNullOrEmpty(text)) return t;

        // BOM 제거
        if (text[0] == '﻿') text = text.Substring(1);

        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        int headerLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0) continue;
            headerLine = i;
            break;
        }
        if (headerLine < 0) return t;

        string[] head = lines[headerLine].Split(',');
        for (int i = 0; i < head.Length; i++) t.Headers.Add(head[i].Trim());

        for (int i = headerLine + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Trim().Length == 0) continue;               // 마지막 개행이 만든 빈 행

            string[] cells = line.Split(',');
            Dictionary<string, string> map =
                new Dictionary<string, string>(t.Headers.Count, StringComparer.Ordinal);

            for (int c = 0; c < t.Headers.Count; c++)
                map[t.Headers[c]] = c < cells.Length ? cells[c].Trim() : "";

            t.Rows.Add(new Row(map, i + 1));
        }

        return t;
    }
}
