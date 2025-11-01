using System.Collections.Generic;
using UnityEngine;

public static class DialogueFlags
{
    private static readonly Dictionary<string, bool> _flags = new();

    public static void Set(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;
        _flags[key] = value;
        // Debug.Log($"[Flags] {key} = {value}");
    }

    public static bool Get(string key, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(key)) return defaultValue;
        return _flags.TryGetValue(key, out var v) ? v : defaultValue;
    }
}
