using UnityEngine;
using UnityEngine.InputSystem;

public static class TopDownAimUtility
{
    static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

    public static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }

    public static Ray BuildAimRay(Camera camera, Vector2 screenPixel)
    {
        var rect = camera.pixelRect;
        var normalizedX = (screenPixel.x - rect.x) / rect.width;
        var normalizedY = (screenPixel.y - rect.y) / rect.height;
        if (normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f)
            return camera.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0f));

        return camera.ScreenPointToRay(new Vector3(screenPixel.x, screenPixel.y, 0f));
    }

    public static bool TryGetGroundPointUnderScreenPosition(Camera camera, Vector2 screenPixel, out Vector3 groundPoint)
    {
        groundPoint = default;
        if (camera == null)
            return false;

        var ray = BuildAimRay(camera, screenPixel);
        if (!GroundPlane.Raycast(ray, out float enter))
            return false;

        groundPoint = ray.GetPoint(enter);
        return true;
    }

    public static bool TryGetFlatDirection(Vector3 origin, Vector3 targetPoint, out Vector3 direction)
    {
        direction = targetPoint - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }
}
