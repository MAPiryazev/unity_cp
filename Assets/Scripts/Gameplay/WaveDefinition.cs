using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Waves/Wave Definition", fileName = "WaveDefinition")]
public sealed class WaveDefinition : ScriptableObject
{
    [SerializeField] WaveEntry[] entries = Array.Empty<WaveEntry>();

    public WaveEntry[] Entries => entries;

    void OnValidate()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            entry.Clamp();
            entries[i] = entry;
        }
    }
}

[Serializable]
public struct WaveEntry
{
    [SerializeField] EnemyDefinition enemy;
    [SerializeField] int count;
    [SerializeField] float spawnInterval;

    public EnemyDefinition Enemy => enemy;
    public int Count => count;
    public float SpawnInterval => spawnInterval;

    public void Clamp()
    {
        count = Mathf.Max(0, count);
        spawnInterval = Mathf.Max(0f, spawnInterval);
    }
}
