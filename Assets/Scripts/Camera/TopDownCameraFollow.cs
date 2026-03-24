using UnityEngine;

public sealed class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 29.333334f, 0f);
    [Tooltip("SmoothDamp time — меньше = быстрее догоняет, больше = мягче.")]
    [SerializeField] float smoothTime = 0.12f;
    [Tooltip("Фиксировать наклон top-down (90° по X), чтобы не уезжал при других скриптах.")]
    [SerializeField] bool lockTopDownRotation = true;

    Vector3 _smoothVelocity;

    void Start()
    {
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        var desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _smoothVelocity, smoothTime);

        if (lockTopDownRotation)
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
