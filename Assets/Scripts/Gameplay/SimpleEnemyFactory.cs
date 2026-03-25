using UnityEngine;

public static class SimpleEnemyFactory
{
    public static GameObject CreateEnemy(Vector3 position, Transform target, EnemyDefinition definition)
    {
        var enemyRoot = new GameObject("Enemy");
        enemyRoot.transform.position = position;
        enemyRoot.layer = ResolveEnemyLayer();

        var collider = enemyRoot.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.radius = 0.45f;
        collider.height = 2f;

        var health = enemyRoot.AddComponent<Health>();
        health.SetMaxHealth(definition != null ? definition.MaxHealth : 25f);

        var controller = enemyRoot.AddComponent<SimpleEnemyController>();
        controller.Initialize(target, definition);

        enemyRoot.AddComponent<WorldHealthBar>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.layer = enemyRoot.layer;
        visual.transform.SetParent(enemyRoot.transform, false);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        visual.transform.localScale = new Vector3(0.9f, 1f, 0.9f);

        var visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Object.Destroy(visualCollider);

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = renderer.material;
            material.color = new Color(0.85f, 0.2f, 0.2f, 1f);
        }

        return enemyRoot;
    }

    static int ResolveEnemyLayer()
    {
        var layer = LayerMask.NameToLayer("Enemy");
        return layer >= 0 ? layer : 0;
    }
}
