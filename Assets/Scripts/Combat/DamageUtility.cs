using UnityEngine;

public static class DamageUtility
{
    /// <summary>Временная отладка: логировать неудачный поиск IDamageable (план диагностики HP HUD).</summary>
    public static bool LogFailedDamageLookup;

    public static bool TryApplyDamage(Component target, DamageInfo damageInfo)
    {
        if (!TryFindDamageable(target, out var damageable))
        {
            if (LogFailedDamageLookup && target != null)
                Debug.LogWarning($"[DamageUtility] No IDamageable on '{target.name}' or its ancestors/descendants.", target);
            return false;
        }

        if(target.TryGetComponent<Rigidbody>(out Rigidbody rb))
		{
            rb.linearVelocity = damageInfo.Direction * 25;
        }

        damageable.ApplyDamage(damageInfo);
        return true;
    }

    static bool TryFindDamageable(Component target, out IDamageable damageable)
    {
        damageable = default;
        if (target == null)
            return false;

        for (Transform current = target.transform; current != null; current = current.parent)
        {
            if (current.TryGetComponent<IDamageable>(out damageable))
                return true;
        }

        // Health на дочернем объекте, а цель урона — родитель (например Player root без Health).
        damageable = target.GetComponentInChildren<IDamageable>(true);
        return damageable != null;
    }
}
