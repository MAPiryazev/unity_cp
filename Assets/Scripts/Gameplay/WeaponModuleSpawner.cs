using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponModuleSpawner : MonoBehaviour
{
    [Serializable]
    struct SpawnEntry
    {
        [SerializeField] WeaponModifierDefinition definition;
        [SerializeField] StatWeaponModifierTemplate runtimeModifier;
        [SerializeField] float duration;
        [SerializeField] float weight;
        [SerializeField] Color pickupColor;
        [SerializeField] Vector3 pickupScale;

        public WeaponModifierDefinition Definition => definition;
        public StatWeaponModifierTemplate RuntimeModifier => runtimeModifier;
        public float Duration => Mathf.Max(0.1f, duration);
        public float Weight => Mathf.Max(0f, weight);
        public Color PickupColor => pickupColor.maxColorComponent > 0.001f ? pickupColor : Color.white;
        public Vector3 PickupScale => pickupScale == Vector3.zero ? new Vector3(0.45f, 0.2f, 0.45f) : pickupScale;
        public bool IsValid => definition != null || runtimeModifier.HasEffect;

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
    [SerializeField] Vector2 spawnIntervalRange = new Vector2(10f, 16f);
    [SerializeField] int maxActivePickups = 3;
    [SerializeField] float wallPadding = 2f;
    [SerializeField] float minDistanceFromPlayer = 4f;
    [SerializeField] float minDistanceBetweenPickups = 2.5f;
    [SerializeField] int spawnPositionAttempts = 20;
    [SerializeField] SpawnEntry[] spawnTable = Array.Empty<SpawnEntry>();

    readonly List<WeaponModulePickup> _activePickups = new List<WeaponModulePickup>();
    Coroutine _spawnRoutine;

    void Awake()
    {
        ResolveReferences();
        if (spawnTable == null || spawnTable.Length == 0)
            spawnTable = CreateDefaultSpawnTable();
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
    }

    IEnumerator SpawnLoop()
    {
        if (initialSpawnDelay > 0f)
            yield return new WaitForSeconds(initialSpawnDelay);

        while (enabled)
        {
            CleanupDestroyedPickups();
            if (_activePickups.Count < maxActivePickups && TrySpawnPickup())
                CleanupDestroyedPickups();

            float delay = UnityEngine.Random.Range(
                Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y),
                Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y));
            yield return new WaitForSeconds(Mathf.Max(1f, delay));
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
            entry.PickupColor,
            entry.PickupScale);

        _activePickups.Add(pickup);
        return true;
    }

    bool TryGetSpawnPosition(out Vector3 position)
    {
        ResolveReferences();

        float halfExtent = arena != null ? Mathf.Max(2f, arena.HalfExtent - wallPadding) : 8f;
        for (int attempt = 0; attempt < Mathf.Max(1, spawnPositionAttempts); attempt++)
        {
            position = new Vector3(
                UnityEngine.Random.Range(-halfExtent, halfExtent),
                0f,
                UnityEngine.Random.Range(-halfExtent, halfExtent));

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
        _activePickups.RemoveAll(pickup => pickup == null || !pickup.gameObject.activeInHierarchy);
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
