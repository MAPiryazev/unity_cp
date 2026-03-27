using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class GameplayZoneSpawner : MonoBehaviour
{
    sealed class NoOpZoneEffectDefinition : ZoneEffectDefinition
    {
    }

    [Serializable]
    struct ZoneSpawnEntry
    {
        [SerializeField] ZoneEffectDefinition effect;
        [SerializeField] float weight;
        [SerializeField] Color visualColor;
        [SerializeField] float radius;

        public ZoneEffectDefinition Effect => effect;
        public float Weight => Mathf.Max(0f, weight);
        public Color VisualColor => visualColor.maxColorComponent > 0.001f ? visualColor : new Color(0.55f, 0.55f, 0.65f, 0.55f);
        public float Radius => Mathf.Max(1f, radius);
        public bool IsValid => effect != null;

        public static ZoneSpawnEntry Create(ZoneEffectDefinition effect, float weight, Color color, float radius)
        {
            var entry = new ZoneSpawnEntry();
            entry.effect = effect;
            entry.weight = Mathf.Max(0f, weight);
            entry.visualColor = color;
            entry.radius = Mathf.Max(1f, radius);
            return entry;
        }
    }

    [SerializeField] ArenaBootstrap arena;
    [SerializeField] Transform playerTarget;
    [SerializeField] ZoneEffectDefinition[] defaultEffects = Array.Empty<ZoneEffectDefinition>();
    [SerializeField] Vector2 spawnIntervalRange = new Vector2(12f, 20f);
    [SerializeField] Vector2 lifetimeRange = new Vector2(7f, 12f);
    [SerializeField] int maxActiveZones = 2;
    [SerializeField] float wallPadding = 2f;
    [SerializeField] float minDistanceFromPlayer = 4f;
    [SerializeField] int spawnPositionAttempts = 20;
    [SerializeField] ZoneSpawnEntry[] spawnTable = Array.Empty<ZoneSpawnEntry>();
    [SerializeField] bool debugLogs;

    readonly List<GameplayZone> _activeZones = new List<GameplayZone>();
    Coroutine _loopRoutine;
    float _nextSpawnTime;

    void Awake()
    {
        ClampConfig();
        ResolveReferences();
        EnsureSpawnTableIsInitialized();
        _nextSpawnTime = Time.time + GetRandomSpawnDelay();
    }

    void OnValidate()
    {
        ClampConfig();
    }

    void OnEnable()
    {
        if (_loopRoutine == null)
            _loopRoutine = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            CleanupDestroyedZones();
            if (_activeZones.Count < maxActiveZones && Time.time >= _nextSpawnTime)
            {
                if (TrySpawnZone())
                    _nextSpawnTime = Time.time + GetRandomSpawnDelay();
                else
                    _nextSpawnTime = Time.time + 1f;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    bool TrySpawnZone()
    {
        if (!HasAnyValidSpawnEntries())
        {
            LogWarning("Zone spawn aborted: spawn table does not contain valid entries.");
            return false;
        }

        ResolveReferences();
        if (!TryGetSpawnPosition(out var spawnPosition))
        {
            LogWarning("Zone spawn aborted: could not find spawn position.");
            return false;
        }

        var entry = ChooseSpawnEntry();
        if (!entry.IsValid)
        {
            LogWarning("Zone spawn aborted: weighted selection returned invalid entry.");
            return false;
        }

        var zoneObject = new GameObject($"GameplayZone_{_activeZones.Count + 1}");
        zoneObject.transform.SetParent(transform, false);
        zoneObject.transform.position = spawnPosition;

        var trigger = zoneObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = entry.Radius;

        var zone = zoneObject.AddComponent<GameplayZone>();
        // Ensure Unity trigger events fire reliably.
        var rb = zoneObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        zone.Configure(entry.Effect, shouldAffectTriggers: true);
        CreateZoneVisual(zoneObject.transform, entry.Radius, entry.VisualColor);

        _activeZones.Add(zone);
        float lifetime = GetRandomLifetime();
        StartCoroutine(DestroyZoneAfter(zoneObject, lifetime));
        LogDebug(
            $"Spawned zone '{entry.Effect.name}' at {spawnPosition} radius={entry.Radius:0.00} lifetime={lifetime:0.00}s active={_activeZones.Count}/{maxActiveZones}");
        return true;
    }

    IEnumerator DestroyZoneAfter(GameObject zoneObject, float lifetime)
    {
        yield return new WaitForSeconds(Mathf.Max(0.25f, lifetime));
        if (zoneObject != null)
            Destroy(zoneObject);
    }

    bool TryGetSpawnPosition(out Vector3 position)
    {
        float halfExtent = arena != null ? Mathf.Max(2f, arena.HalfExtent - wallPadding) : 8f;
        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            position = new Vector3(
                UnityEngine.Random.Range(-halfExtent, halfExtent),
                0f,
                UnityEngine.Random.Range(-halfExtent, halfExtent));

            if (playerTarget != null)
            {
                var fromPlayer = position - playerTarget.position;
                fromPlayer.y = 0f;
                if (fromPlayer.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
                    continue;
            }

            return true;
        }

        position = Vector3.zero;
        return false;
    }

    ZoneSpawnEntry ChooseSpawnEntry()
    {
        float totalWeight = 0f;
        for (int i = 0; i < spawnTable.Length; i++)
        {
            if (spawnTable[i].IsValid)
                totalWeight += spawnTable[i].Weight;
        }

        if (totalWeight <= 0.0001f)
        {
            LogWarning("Zone spawn aborted: total weight is zero.");
            return default;
        }

        float random = UnityEngine.Random.Range(0f, totalWeight);
        float accumulated = 0f;
        ZoneSpawnEntry fallback = default;
        bool hasFallback = false;
        for (int i = 0; i < spawnTable.Length; i++)
        {
            if (!spawnTable[i].IsValid)
                continue;

            fallback = spawnTable[i];
            hasFallback = true;
            accumulated += spawnTable[i].Weight;
            if (random <= accumulated)
                return spawnTable[i];
        }

        return hasFallback ? fallback : default;
    }

    void ResolveReferences()
    {
        if (arena == null)
            arena = GetComponent<ArenaBootstrap>() ?? FindFirstObjectByType<ArenaBootstrap>();

        if (playerTarget == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                playerTarget = player.transform;
        }
    }

    void CleanupDestroyedZones()
    {
        _activeZones.RemoveAll(zone => zone == null || !zone.gameObject.activeInHierarchy);
    }

    float GetRandomSpawnDelay()
    {
        return UnityEngine.Random.Range(
            Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y),
            Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y));
    }

    float GetRandomLifetime()
    {
        return UnityEngine.Random.Range(
            Mathf.Min(lifetimeRange.x, lifetimeRange.y),
            Mathf.Max(lifetimeRange.x, lifetimeRange.y));
    }

    void ClampConfig()
    {
        maxActiveZones = Mathf.Max(1, maxActiveZones);
        wallPadding = Mathf.Max(0f, wallPadding);
        minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
        spawnPositionAttempts = Mathf.Max(1, spawnPositionAttempts);
        spawnIntervalRange = new Vector2(
            Mathf.Max(0.25f, spawnIntervalRange.x),
            Mathf.Max(0.25f, spawnIntervalRange.y));
        lifetimeRange = new Vector2(
            Mathf.Max(0.25f, lifetimeRange.x),
            Mathf.Max(0.25f, lifetimeRange.y));
        if (spawnTable == null)
            spawnTable = Array.Empty<ZoneSpawnEntry>();
    }

    static void CreateZoneVisual(Transform parent, float radius, Color color)
    {
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "ZoneVisual";
        visual.transform.SetParent(parent, false);
        // Cylinder is ~0.04 tall after scale; floor top is ~y=0. Keep the disc sitting on the floor (was -0.48 — fully underground).
        float discHalfHeight = 0.02f;
        visual.transform.localPosition = new Vector3(0f, discHalfHeight, 0f);
        visual.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

        var collider = visual.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = renderer.material;
            if (mat != null)
                mat.color = color;
        }
    }

    void EnsureSpawnTableIsInitialized()
    {
        // Even if there are valid entries, we want to guarantee that our expected zones
        // are present (Slow + Smoke). Otherwise, weighted selection can effectively
        // stop spawning one of them.
        if (HasAnyValidSpawnEntries())
        {
            bool hasSlow = false;
            bool hasSmoke = false;
            for (int i = 0; i < spawnTable.Length; i++)
            {
                if (!spawnTable[i].IsValid)
                    continue;

                if (spawnTable[i].Effect is SlowZoneEffectDefinition)
                    hasSlow = true;
                else if (spawnTable[i].Effect is SmokeZoneEffectDefinition)
                    hasSmoke = true;
            }

            if (hasSlow && hasSmoke)
                return;
        }

        var generatedEntries = BuildEntriesFromEffects(defaultEffects);
        if (generatedEntries.Length > 0)
        {
            spawnTable = generatedEntries;
            LogDebug($"Initialized zone spawn table from default effects ({generatedEntries.Length} entries).");
            return;
        }

        generatedEntries = BuildEntriesFromProjectAssets();
        if (generatedEntries.Length > 0)
        {
            spawnTable = generatedEntries;
            LogDebug($"Initialized zone spawn table from project assets ({generatedEntries.Length} entries).");
            return;
        }

        // This fallback keeps the gameplay loop alive even when assets are not wired yet.
        var noOpEffect = ScriptableObject.CreateInstance<NoOpZoneEffectDefinition>();
        noOpEffect.name = "NoOpZoneEffect";
        spawnTable = new[]
        {
            CreateEntry(noOpEffect, 1f, new Color(0.45f, 0.45f, 0.45f, 0.45f), 3.5f)
        };
        LogWarning("Zone spawn table was empty. Injected a no-op fallback effect; wire real zone assets in GameplayZoneSpawner.defaultEffects.");
    }

    ZoneSpawnEntry[] BuildEntriesFromEffects(ZoneEffectDefinition[] effects)
    {
        if (effects == null || effects.Length == 0)
            return Array.Empty<ZoneSpawnEntry>();

        var entries = new List<ZoneSpawnEntry>(effects.Length);
        for (int i = 0; i < effects.Length; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            // "Small" vs "Big" zone radii:
            // В текущей логике радиус задаётся здесь, поэтому делаем различие по типу эффекта.
            // По умолчанию радиус 3.5, а для Slow-зоны (маленькая) делаем в 2 раза больше.
            float radius = effect is SlowZoneEffectDefinition ? 7.0f : 3.5f;
            entries.Add(CreateEntry(effect, 1f, GetDefaultColor(i), radius));
        }

        return entries.ToArray();
    }

    ZoneSpawnEntry[] BuildEntriesFromProjectAssets()
    {
#if UNITY_EDITOR
        var guids = AssetDatabase.FindAssets("t:ZoneEffectDefinition");
        if (guids == null || guids.Length == 0)
            return Array.Empty<ZoneSpawnEntry>();

        var effects = new List<ZoneEffectDefinition>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var effect = AssetDatabase.LoadAssetAtPath<ZoneEffectDefinition>(path);
            if (effect != null)
                effects.Add(effect);
        }

        return BuildEntriesFromEffects(effects.ToArray());
#else
        return Array.Empty<ZoneSpawnEntry>();
#endif
    }

    static Color GetDefaultColor(int index)
    {
        return index % 2 == 0
            ? new Color(0.32f, 0.58f, 0.95f, 0.5f)
            : new Color(0.8f, 0.8f, 0.86f, 0.5f);
    }

    static ZoneSpawnEntry CreateEntry(ZoneEffectDefinition effect, float weight, Color color, float radius)
    {
        return ZoneSpawnEntry.Create(effect, weight, color, radius);
    }

    bool HasAnyValidSpawnEntries()
    {
        if (spawnTable == null || spawnTable.Length == 0)
            return false;

        for (int i = 0; i < spawnTable.Length; i++)
        {
            if (spawnTable[i].IsValid && spawnTable[i].Weight > 0f)
                return true;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        ResolveReferences();
        float halfExtent = arena != null ? Mathf.Max(2f, arena.HalfExtent - wallPadding) : 8f;
        Gizmos.color = new Color(0.32f, 0.58f, 0.95f, 0.35f);
        Gizmos.DrawWireCube(transform.position, new Vector3(halfExtent * 2f, 0.05f, halfExtent * 2f));
    }

    void LogDebug(string message)
    {
        if (debugLogs)
            Debug.Log($"[GameplayZoneSpawner] {message}", this);
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[GameplayZoneSpawner] {message}", this);
    }
}
