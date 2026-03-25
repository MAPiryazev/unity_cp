using UnityEngine;

public static class DamageUtility
{
    public static bool TryApplyDamage(Component target, DamageInfo damageInfo)
    {
        if (!TryFindDamageable(target, out var damageable))
            return false;

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
            var behaviours = current.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageable candidate)
                {
                    damageable = candidate;
                    return true;
                }
            }
        }

        return false;
    }
}
