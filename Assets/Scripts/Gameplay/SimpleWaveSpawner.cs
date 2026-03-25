using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SimpleWaveSpawner : MonoBehaviour
{
    [SerializeField] EnemyDefinition enemyDefinition;
    [SerializeField] Transform playerTarget;
    [SerializeField] ArenaBootstrap arena;
    [SerializeField] int startingEnemyCount = 2;
    [SerializeField] int additionalEnemiesPerWave = 1;
    [SerializeField] float spawnInterval = 0.5f;
    [SerializeField] float delayBetweenWaves = 2f;
    [SerializeField] float spawnPaddingFromWall = 1.5f;
    [SerializeField] bool autoStart = true;

    readonly HashSet<Health> _aliveEnemies = new HashSet<Health>();
    int _waveIndex;
    Coroutine _loopRoutine;

    void Start()
    {
        ResolveSceneReferences();
        if (autoStart)
            _loopRoutine = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (_loopRoutine != null)
            StopCoroutine(_loopRoutine);

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
        ResolveSceneReferences();
        var enemyObject = SimpleEnemyFactory.CreateEnemy(GetSpawnPosition(), playerTarget, enemyDefinition);
        var health = enemyObject.GetComponent<Health>();
        if (health == null)
            return;

        _aliveEnemies.Add(health);
        health.Died += HandleEnemyDeath;
    }

    void HandleEnemyDeath(DamageInfo damageInfo)
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
            arena = FindFirstObjectByType<ArenaBootstrap>();
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
