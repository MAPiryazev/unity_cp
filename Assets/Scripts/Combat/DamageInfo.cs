using UnityEngine;

public readonly struct DamageInfo
{
    public DamageInfo(float amount, Vector3 hitPoint, Vector3 direction, GameObject source)
    {
        Amount = amount;
        HitPoint = hitPoint;
        Direction = direction;
        Source = source;
    }

    public float Amount { get; }
    public Vector3 HitPoint { get; }
    public Vector3 Direction { get; }
    public GameObject Source { get; }
}
