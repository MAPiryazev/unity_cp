using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SimpleWaveSpawner : MonoBehaviour
{
    const float MinReferenceLookupInterval = 0.1f;

    [Header("Enemy Types")]
    [SerializeField] EnemyDefinition enemyDefinition;
    [Tooltip("When assigned, heavy enemies are mixed into each wave starting from firstWaveWithHeavy.")]
    [SerializeField] EnemyDefinition heavyEnemyDefinition;
    [Tooltip("Probability (0–1) that any given enemy in a wave is a heavy. Only applies when heavyEnemyDefinition is set.")]
    [SerializeField] [Range(0f, 1f)] float heavyEnemyChance = 0.25f;
    [Tooltip("Wave number (1-based) from which heavy enemies start appearing. Set to 1 to have them from the start.")]
    [SerializeField] int firstWaveWithHeavy = 3;

    [Header("Spawn")]
    [SerializeField] Transform playerTarget;
    [SerializeField] ArenaBootstrap arena;
    [SerializeField] int startingEnemyCount = 2;
    [SerializeField] int additionalEnemiesPerWave = 1;
    [SerializeField] float spawnInterval = 0.5f;
    [SerializeField] float delayBetweenWaves = 2f;
    [SerializeField] float spawnPaddingFromWall = 1.5f;
    [SerializeField] bool autoStart = true;
    [SerializeField] bool allowSceneLookupFallback = true;
    [SerializeField] float referenceLookupInterval = 1f;

    readonly HashSet<Health> _aliveEnemies = new HashSet<Health>();
    int _waveIndex;
    Coroutine _loopRoutine;
    float _nextReferenceLookupTime;

    public int CurrentWaveIndex => _waveIndex + 1;
    public event System.Action<int> WaveStarted;

    public void Initialize(Transform target, ArenaBootstrap arenaBootstrap, EnemyDefinition definition = null)
    {
        playerTarget = target;
        arena = arenaBootstrap;
        if (definition != null)
            enemyDefinition = definition;
    }

    void Start()
    {
        ResolveSceneReferences();
        TryStartLoop();
    }

    void OnDisable()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }

        foreach (var health in _aliveEnemies)
        {
            if (health != null)
                health.Died -= HandleEnemyDeath;
        }

        _aliveEnemies.Clear();
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(delayBetweenWaves);
            WaveStarted?.Invoke(CurrentWaveIndex);
            yield return SpawnWave();
            yield return new WaitUntil(() => _aliveEnemies.Count == 0);
            _waveIndex++;
        }
    }

    IEnumerator SpawnWave()
    {
        int enemyCount = startingEnemyCount + _waveIndex * additionalEnemiesPerWave;
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            if (i < enemyCount - 1)
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy()
    {
        if (!EnsureSpawnReferences())
            return;

        var definition = PickDefinition();
        var enemyObject = SimpleEnemyFactory.CreateEnemy(GetSpawnPosition(), playerTarget, definition);
        var health = enemyObject.GetComponent<Health>();
        if (health == null)
            return;

        _aliveEnemies.Add(health);
        health.Died += HandleEnemyDeath;
    }

    EnemyDefinition PickDefinition()
    {
        if (heavyEnemyDefinition != null
            && CurrentWaveIndex >= firstWaveWithHeavy
            && Random.value < heavyEnemyChance)
        {
            return heavyEnemyDefinition;
        }

        return enemyDefinition;
    }

    void HandleEnemyDeath(DamageInfo _)
    {
        CleanupDeadEnemies();
    }

    void CleanupDeadEnemies()
    {
        _aliveEnemies.RemoveWhere(health =>
        {
            if (health == null)
                return true;

            if (health.IsAlive)
                return false;

            health.Died -= HandleEnemyDeath;
            return true;
        });
    }

    void ResolveSceneReferences()
    {
        if (playerTarget == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                playerTarget = player.transform;
        }

        if (arena == null)
            arena = GetComponent<ArenaBootstrap>() ?? FindFirstObjectByType<ArenaBootstrap>();
    }

    bool EnsureSpawnReferences()
    {
        if (playerTarget != null)
            return true;

        if (!allowSceneLookupFallback)
            return false;

        if (Time.time < _nextReferenceLookupTime)
            return false;

        _nextReferenceLookupTime = Time.time + Mathf.Max(MinReferenceLookupInterval, referenceLookupInterval);
        ResolveSceneReferences();
        return playerTarget != null;
    }

    void TryStartLoop()
    {
        if (!autoStart || _loopRoutine != null)
            return;

        _loopRoutine = StartCoroutine(SpawnLoop());
    }

    Vector3 GetSpawnPosition()
    {
        float halfExtent = arena != null ? Mathf.Max(2f, arena.HalfExtent - spawnPaddingFromWall) : 12f;
        float side = Random.Range(0, 4);
        float axis = Random.Range(-halfExtent, halfExtent);

        Vector3 position = side switch
        {
            < 1f => new Vector3(axis, 0f, halfExtent),
            < 2f => new Vector3(axis, 0f, -halfExtent),
            < 3f => new Vector3(halfExtent, 0f, axis),
            _ => new Vector3(-halfExtent, 0f, axis)
        };

        return position;
    }
}
