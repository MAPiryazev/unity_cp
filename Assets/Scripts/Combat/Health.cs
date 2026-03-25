using System;
using UnityEngine;

/// <summary>Здоровье сущности; UI подписывается на <see cref="HealthChanged"/>.</summary>
[DefaultExecutionOrder(-100)]
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 50f;
    [Tooltip("Для игрока обычно выключить — объект не удаляется при 0 HP.")]
    [SerializeField] bool destroyGameObjectOnDeath = true;

    float _current;

    public float CurrentHealth => _current;
    public float MaxHealth => maxHealth;
    public bool IsAlive => _current > 0f;

    public event Action<float, float> HealthChanged;
    public event Action<DamageInfo, float, float> Damaged;
    public event Action<DamageInfo> Died;

    void Awake()
    {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        _current = maxHealth;
    }

    void Start() => HealthChanged?.Invoke(_current, maxHealth);

    public void TakeDamage(float amount)
    {
        ApplyDamage(new DamageInfo(amount, transform.position, Vector3.zero, null));
    }

    public void ApplyDamage(DamageInfo damageInfo)
    {
        if (damageInfo.Amount <= 0f || _current <= 0f)
            return;

        _current = Mathf.Max(0f, _current - damageInfo.Amount);
        HealthChanged?.Invoke(_current, maxHealth);
        Damaged?.Invoke(damageInfo, _current, maxHealth);

        if (_current > 0f)
            return;

        Died?.Invoke(damageInfo);
        if (destroyGameObjectOnDeath)
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Take 10 damage")]
    void DebugTake10() => TakeDamage(10f);
#endif
}
