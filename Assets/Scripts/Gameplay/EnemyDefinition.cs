using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Enemies/Enemy Definition", fileName = "EnemyDefinition")]
public sealed class EnemyDefinition : ScriptableObject
{
    [SerializeField] float maxHealth = 25f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float contactDamage = 10f;

    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        contactDamage = Mathf.Max(0f, contactDamage);
    }
}
