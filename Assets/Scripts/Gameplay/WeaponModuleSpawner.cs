using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponModuleSpawner : MonoBehaviour
{
    const float MinDelay = 0.25f;

    [Serializable]
    struct SpawnEntry
    {
        [SerializeField] WeaponModifierDefinition definition;
        [SerializeField] StatWeaponModifierTemplate runtimeModifier;
        [SerializeField] float duration;
        [SerializeField] float weight;
        [SerializeField] Color pickupColor;
        [SerializeField] Vector3 pickupScale;
        [SerializeField] GameObject prefab;

        public WeaponModifierDefinition Definition => definition;
        public StatWeaponModifierTemplate RuntimeModifier => runtimeModifier;
        public float Duration => Mathf.Max(0.1f, duration);
        public float Weight => Mathf.Max(0f, weight);
        public Color PickupColor => pickupColor.maxColorComponent > 0.001f ? pickupColor : Color.white;
        public Vector3 PickupScale => pickupScale == Vector3.zero ? new Vector3(0.45f, 0.2f, 0.45f) : pickupScale;
        public bool IsValid => definition != null || runtimeModifier.HasEffect;
        public GameObject Prefab => prefab;

        public static SpawnEntry Create(
            WeaponModifierDefinition definition,
            StatWeaponModifierTemplate runtimeModifier,
            float duration,
            float weight,
            Color pickupColor,
            Vector3 pickupScale)
        {
            var entry = new SpawnEntry();
            entry.definition = definition;
            entry.runtimeModifier = runtimeModifier;
            entry.duration = duration;
            entry.weight = weight;
            entry.pickupColor = pickupColor;
            entry.pickupScale = pickupScale;
            return entry;
        }
    }

    [SerializeField] ArenaBootstrap arena;
    [SerializeField] Transform playerTarget;
    [SerializeField] float initialSpawnDelay = 6f;
    [SerializeField] float respawnDelayAfterPickup = 4f;
    [SerializeField] Vector2 spawnIntervalRange = new Vector2(10f, 16f);
    [SerializeField] int maxActivePickups = 3;
    [SerializeField] float wallPadding = 2f;
    [SerializeField] float minDistanceFromPlayer = 4f;
    [SerializeField] float minDistanceBetweenPickups = 2.5f;
    [SerializeField] int spawnPositionAttempts = 20;
    [SerializeField] SpawnEntry[] spawnTable = Array.Empty<SpawnEntry>();

    readonly List<WeaponModulePickup> _activePickups = new List<WeaponModulePickup>();
    Coroutine _spawnRoutine;
    float _nextSpawnTime;

    void Awake()
    {
        ResolveReferences();
        ClampConfig();
        if (spawnTable == null || spawnTable.Length == 0)
            spawnTable = CreateDefaultSpawnTable();
        RegisterExistingPickups();
        _nextSpawnTime = Time.time + Mathf.Max(0f, initialSpawnDelay);
    }

    void OnValidate()
    {
        ClampConfig();
    }

    void OnEnable()
    {
        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        for (int i = 0; i < _activePickups.Count; i++)
        {
            if (_activePickups[i] != null)
                _activePickups[i].Consumed -= HandlePickupConsumed;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            CleanupDestroyedPickups();
            if (_activePickups.Count < maxActivePickups && Time.time >= _nextSpawnTime)
            {
                if (TrySpawnPickup())
                    ScheduleNextSpawn(GetRandomSpawnDelay());
                else
                    ScheduleNextSpawn(1f);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    bool TrySpawnPickup()
    {
        ResolveReferences();
        if (!TryGetSpawnPosition(out var spawnPosition))
            return false;

        var entry = ChooseSpawnEntry();
        if (!entry.IsValid)
            return false;

        var pickupObject = new GameObject($"WeaponPickup_{_activePickups.Count + 1}");
        pickupObject.transform.SetParent(transform, false);
        pickupObject.transform.position = spawnPosition;

        var trigger = pickupObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.75f;

        var pickup = pickupObject.AddComponent<WeaponModulePickup>();
        pickup.Configure(
            entry.Definition,
            entry.RuntimeModifier,
            entry.Duration,
            entry.Prefab,
            entry.PickupScale);

        RegisterPickup(pickup);
        return true;
    }

    bool TryGetSpawnPosition(out Vector3 position)
    {
        ResolveReferences();

        float hx = arena != null ? Mathf.Max(2f, arena.HalfExtentX - wallPadding) : 8f;
        float hz = arena != null ? Mathf.Max(2f, arena.HalfExtentZ - wallPadding) : 8f;
        for (int attempt = 0; attempt < Mathf.Max(1, spawnPositionAttempts); attempt++)
        {
            position = new Vector3(
                UnityEngine.Random.Range(-hx, hx),
                0f,
                UnityEngine.Random.Range(-hz, hz));

            if (playerTarget != null)
            {
                Vector3 fromPlayer = position - playerTarget.position;
                fromPlayer.y = 0f;
                if (fromPlayer.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
                    continue;
            }

            bool overlapsAnotherPickup = false;
            for (int i = 0; i < _activePickups.Count; i++)
            {
                var existingPickup = _activePickups[i];
                if (existingPickup == null)
                    continue;

                Vector3 delta = position - existingPickup.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude < minDistanceBetweenPickups * minDistanceBetweenPickups)
                {
                    overlapsAnotherPickup = true;
                    break;
                }
            }

            if (!overlapsAnotherPickup)
                return true;
        }

        position = Vector3.zero;
        return false;
    }

    SpawnEntry ChooseSpawnEntry()
    {
        float totalWeight = 0f;
        for (int i = 0; i < spawnTable.Length; i++)
        {
            if (!spawnTable[i].IsValid)
                continue;

            totalWeight += spawnTable[i].Weight;
        }

        if (totalWeight <= 0.0001f)
            return default;

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float accumulatedWeight = 0f;
        SpawnEntry fallback = default;
        bool hasFallback = false;
        for (int i = 0; i < spawnTable.Length; i++)
        {
            if (!spawnTable[i].IsValid)
                continue;

            fallback = spawnTable[i];
            hasFallback = true;
            accumulatedWeight += spawnTable[i].Weight;
            if (randomValue <= accumulatedWeight)
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

    void CleanupDestroyedPickups()
    {
        for (int i = _activePickups.Count - 1; i >= 0; i--)
        {
            var pickup = _activePickups[i];
            if (pickup != null && pickup.gameObject.activeInHierarchy)
                continue;

            UnregisterPickup(pickup);
        }
    }

    void RegisterPickup(WeaponModulePickup pickup)
    {
        if (pickup == null || _activePickups.Contains(pickup))
            return;

        pickup.Consumed += HandlePickupConsumed;
        _activePickups.Add(pickup);
    }

    void UnregisterPickup(WeaponModulePickup pickup)
    {
        if (pickup != null)
            pickup.Consumed -= HandlePickupConsumed;

        _activePickups.Remove(pickup);
    }

    void RegisterExistingPickups()
    {
        var existingPickups = GetComponentsInChildren<WeaponModulePickup>(true);
        for (int i = 0; i < existingPickups.Length; i++)
            RegisterPickup(existingPickups[i]);
    }

    void HandlePickupConsumed(WeaponModulePickup pickup)
    {
        UnregisterPickup(pickup);
        ScheduleNextSpawn(respawnDelayAfterPickup);
    }

    void ScheduleNextSpawn(float delay)
    {
        _nextSpawnTime = Time.time + Mathf.Max(MinDelay, delay);
    }

    float GetRandomSpawnDelay()
    {
        return UnityEngine.Random.Range(
            Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y),
            Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y));
    }

    void ClampConfig()
    {
        initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
        respawnDelayAfterPickup = Mathf.Max(MinDelay, respawnDelayAfterPickup);
        maxActivePickups = Mathf.Max(1, maxActivePickups);
        wallPadding = Mathf.Max(0f, wallPadding);
        minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
        minDistanceBetweenPickups = Mathf.Max(0f, minDistanceBetweenPickups);
        spawnPositionAttempts = Mathf.Max(1, spawnPositionAttempts);
    }

    static SpawnEntry[] CreateDefaultSpawnTable()
    {
        return new[]
        {
            SpawnEntry.Create(
                definition: null,
                runtimeModifier: WeaponModifierPresets.GetDefaultTemplate(0),
                duration: 10f,
                weight: 1.25f,
                pickupColor: WeaponModifierPresets.GetDefaultColor(0),
                pickupScale: new Vector3(0.42f, 0.18f, 0.42f)),
            SpawnEntry.Create(
                definition: null,
                runtimeModifier: WeaponModifierPresets.GetDefaultTemplate(1),
                duration: 12f,
                weight: 1f,
                pickupColor: WeaponModifierPresets.GetDefaultColor(1),
                pickupScale: new Vector3(0.45f, 0.2f, 0.45f)),
            SpawnEntry.Create(
                definition: null,
                runtimeModifier: WeaponModifierPresets.GetDefaultTemplate(2),
                duration: 9f,
                weight: 0.8f,
                pickupColor: WeaponModifierPresets.GetDefaultColor(2),
                pickupScale: new Vector3(0.5f, 0.18f, 0.5f))
        };
    }
}
