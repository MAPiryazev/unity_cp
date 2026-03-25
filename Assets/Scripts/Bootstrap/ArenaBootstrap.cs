using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class ArenaBootstrap : MonoBehaviour
{
    [SerializeField] float halfExtent = 10f;
    [SerializeField] float wallHeight = 3f;
    [SerializeField] float wallThickness = 0.5f;

    public float HalfExtent => halfExtent;

    void Awake()
    {
        EnsureGameplayHelpers();
        if (transform.Find("Floor") != null)
            return;
        BuildArena();
    }

    void BuildArena()
    {
        float h = wallHeight * 0.5f;
        float span = halfExtent * 2f + wallThickness * 2f;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.layer = LayerMask.NameToLayer("Default");
        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(span, 0.1f, span);

        void AddWall(string name, Vector3 localPosition, Vector3 localScale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.layer = LayerMask.NameToLayer("Default");
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = localScale;
        }

        AddWall(
            "WallNorth",
            new Vector3(0f, h, halfExtent + wallThickness * 0.5f),
            new Vector3(span, wallHeight, wallThickness));

        AddWall(
            "WallSouth",
            new Vector3(0f, h, -halfExtent - wallThickness * 0.5f),
            new Vector3(span, wallHeight, wallThickness));

        AddWall(
            "WallEast",
            new Vector3(halfExtent + wallThickness * 0.5f, h, 0f),
            new Vector3(wallThickness, wallHeight, span));

        AddWall(
            "WallWest",
            new Vector3(-halfExtent - wallThickness * 0.5f, h, 0f),
            new Vector3(wallThickness, wallHeight, span));
    }

    void EnsureGameplayHelpers()
    {
        if (GetComponent<WeaponModuleSpawner>() == null)
            gameObject.AddComponent<WeaponModuleSpawner>();
    }
}
