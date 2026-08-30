using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ItemDataBaker/RecipeDataBaker처럼 "CSV -> ScriptableObject" 굽는 에디터 툴들이 공통으로 쓰는 유틸.
// 각 베이커에 파싱 로직을 따로 복사해두면 한쪽만 고치고 다른 쪽을 놓치는 불일치가 생기기 쉬워서 여기로 모았다.
public static class CsvBakeUtils
{
    // 헤더 줄을 컬럼 이름 -> 인덱스로 매핑 (위치가 아니라 이름으로 값을 찾기 위함)
    public static Dictionary<string, int> ParseHeader(string headerLine)
    {
        string[] header = headerLine.Split(',');
        var colIndex = new Dictionary<string, int>();
        for (int c = 0; c < header.Length; c++)
        {
            colIndex[header[c].Trim()] = c;
        }
        return colIndex;
    }

    public static bool HasRequiredColumns(Dictionary<string, int> colIndex, string[] requiredColumns, string bakerName)
    {
        foreach (var required in requiredColumns)
        {
            if (!colIndex.ContainsKey(required))
            {
                Debug.LogError($"[{bakerName}] CSV 헤더에 '{required}' 컬럼이 없습니다. 굽기를 중단합니다.");
                return false;
            }
        }
        return true;
    }

    // 해당 경로에 에셋이 이미 있으면 로드, 없으면 새로 생성 (ItemData/RecipeData/DatabaseSO 등 모든 굽기 대상에 공용)
    public static T GetOrCreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        return asset;
    }

    // 값이 비어 있으면 조용히 0, 값이 있는데 숫자가 아니면 경고 후 0
    public static int ParseIntOrWarn(string raw, string id, string columnName, int lineNumber, string bakerName)
    {
        if (string.IsNullOrEmpty(raw)) return 0;
        if (int.TryParse(raw, out int value)) return value;

        Debug.LogWarning($"[{bakerName}] '{id}'의 {columnName} 값 '{raw}'이(가) 숫자가 아니어서 0으로 처리됩니다. (CSV {lineNumber}번째 줄)");
        return 0;
    }

    // 값이 비어 있으면 조용히 0, 값이 있는데 숫자가 아니면 경고 후 0
    public static float ParseFloatOrWarn(string raw, string id, string columnName, int lineNumber, string bakerName)
    {
        if (string.IsNullOrEmpty(raw)) return 0f;
        if (float.TryParse(raw, out float value)) return value;

        Debug.LogWarning($"[{bakerName}] '{id}'의 {columnName} 값 '{raw}'이(가) 숫자가 아니어서 0으로 처리됩니다. (CSV {lineNumber}번째 줄)");
        return 0f;
    }

    // 값이 비어 있으면 조용히 기본값, 값이 있는데 enum 이름과 안 맞으면 경고 후 기본값
    public static T ParseEnumOrWarn<T>(string raw, string id, string columnName, int lineNumber, string bakerName) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(raw)) return default;
        if (Enum.TryParse<T>(raw, true, out T value)) return value;

        Debug.LogWarning($"[{bakerName}] '{id}'의 {columnName} 값 '{raw}'을(를) 알 수 없어 {default(T)}(으)로 처리됩니다. (CSV {lineNumber}번째 줄)");
        return default;
    }
}
