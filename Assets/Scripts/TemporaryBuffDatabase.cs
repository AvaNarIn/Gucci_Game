using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffDatabase", menuName = "Database/Buff Database")]
public class TemporaryBuffDatabase : ScriptableObject
{
    public List<TemporaryBuffData> allBuffs;

    public TemporaryBuffData GetRandomBuff(List<TemporaryBuffData> exclude = null)
    {
        var pool = exclude != null ? allBuffs.FindAll(b => !exclude.Contains(b)) : allBuffs;
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }
}