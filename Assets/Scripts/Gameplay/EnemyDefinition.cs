using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Enemies/Enemy Definition", fileName = "EnemyDefinition")]
public sealed class EnemyDefinition : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] float maxHealth = 25f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float contactDamage = 10f;

    [Header("Visual")]
    [Tooltip("Tint color applied to the primitive material when no Material override is set.")]
    [SerializeField] GameObject prefab;

    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;
    public GameObject Prefab => prefab;

    void OnEnable() => ClampValues();
    void OnValidate() => ClampValues();

    void ClampValues()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        contactDamage = Mathf.Max(0f, contactDamage);
    }
}
