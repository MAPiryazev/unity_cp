using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ZoneEffectReceiver : MonoBehaviour
{
    readonly Dictionary<int, float> _moveMultipliers = new Dictionary<int, float>();
    readonly Dictionary<int, float> _aimTurnMultipliers = new Dictionary<int, float>();
    readonly Dictionary<int, float> _contactDamageMultipliers = new Dictionary<int, float>();
    readonly Dictionary<int, float> _attackCooldownMultipliers = new Dictionary<int, float>();

    public float MovementMultiplier { get; private set; } = 1f;
    public float AimTurnMultiplier { get; private set; } = 1f;
    public float ContactDamageMultiplier { get; private set; } = 1f;
    public float AttackCooldownMultiplier { get; private set; } = 1f;

    public void SetMovementMultiplier(Object source, float multiplier)
    {
        SetMultiplier(_moveMultipliers, source, multiplier, out var value);
        MovementMultiplier = value;
    }

    public void ClearMovementMultiplier(Object source)
    {
        ClearMultiplier(_moveMultipliers, source, out var value);
        MovementMultiplier = value;
    }

    public void SetAimTurnMultiplier(Object source, float multiplier)
    {
        SetMultiplier(_aimTurnMultipliers, source, multiplier, out var value);
        AimTurnMultiplier = value;
    }

    public void ClearAimTurnMultiplier(Object source)
    {
        ClearMultiplier(_aimTurnMultipliers, source, out var value);
        AimTurnMultiplier = value;
    }

    public void SetContactDamageMultiplier(Object source, float multiplier)
    {
        SetMultiplier(_contactDamageMultipliers, source, multiplier, out var value);
        ContactDamageMultiplier = value;
    }

    public void ClearContactDamageMultiplier(Object source)
    {
        ClearMultiplier(_contactDamageMultipliers, source, out var value);
        ContactDamageMultiplier = value;
    }

    public void SetAttackCooldownMultiplier(Object source, float multiplier)
    {
        SetMultiplier(_attackCooldownMultipliers, source, multiplier, out var value);
        AttackCooldownMultiplier = value;
    }

    public void ClearAttackCooldownMultiplier(Object source)
    {
        ClearMultiplier(_attackCooldownMultipliers, source, out var value);
        AttackCooldownMultiplier = value;
    }

    static void SetMultiplier(Dictionary<int, float> storage, Object source, float multiplier, out float combinedValue)
    {
        combinedValue = 1f;
        if (source == null)
            return;

        storage[source.GetInstanceID()] = Mathf.Max(0.01f, multiplier);
        foreach (var entry in storage)
            combinedValue *= entry.Value;
    }

    static void ClearMultiplier(Dictionary<int, float> storage, Object source, out float combinedValue)
    {
        combinedValue = 1f;
        if (source != null)
            storage.Remove(source.GetInstanceID());

        foreach (var entry in storage)
            combinedValue *= entry.Value;
    }
}
