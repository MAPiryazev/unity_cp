using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class GameplayZone : MonoBehaviour
{
    [SerializeField] ZoneEffectDefinition effect;
    [SerializeField] bool affectTriggers;

    // Keeps track of every GameObject that received OnZoneEntered so we can
    // reliably call OnZoneExited when the zone is destroyed (Unity skips
    // OnTriggerExit for colliders that are destroyed while overlapping).
    readonly HashSet<GameObject> _trackedTargets = new HashSet<GameObject>();

    public ZoneEffectDefinition Effect => effect;

    public void Configure(ZoneEffectDefinition effectDefinition, bool shouldAffectTriggers = false)
    {
        effect = effectDefinition;
        affectTriggers = shouldAffectTriggers;
    }

    void Reset()
    {
        EnsureTriggerCollider();
    }

    void OnValidate()
    {
        EnsureTriggerCollider();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CanAffect(other))
            return;

        effect?.OnZoneEntered(other.gameObject, gameObject);
        _trackedTargets.Add(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (!CanAffect(other))
            return;

        effect?.OnZoneExited(other.gameObject, gameObject);
        _trackedTargets.Remove(other.gameObject);
    }

    void OnDestroy()
    {
        if (effect == null || _trackedTargets.Count == 0)
            return;

        foreach (var target in _trackedTargets)
        {
            if (target != null)
                effect.OnZoneExited(target, gameObject);
        }

        _trackedTargets.Clear();
    }

    bool CanAffect(Collider other)
    {
        if (other == null)
            return false;

        return affectTriggers || !other.isTrigger;
    }

    void EnsureTriggerCollider()
    {
        var trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }
}
