using UnityEngine;
using UnityEngine.UI;

public readonly struct RuntimeHealthBarReferences
{
    public RuntimeHealthBarReferences(Transform root, Canvas canvas, Image fillImage)
    {
        Root = root;
        Canvas = canvas;
        FillImage = fillImage;
    }

    public Transform Root { get; }
    public Canvas Canvas { get; }
    public Image FillImage { get; }
}

public static class HealthBarFactory
{
    public static RuntimeHealthBarReferences CreateWorldBar(
        Transform parent,
        string rootName,
        Vector3 localOffset,
        Vector2 size,
        float billboardScale,
        float pixelsPerUnit,
        Color backgroundColor,
        Color fillColor)
    {
        var root = new GameObject(rootName).transform;
        root.SetParent(parent, false);
        root.localPosition = localOffset;
        root.localScale = Vector3.one * billboardScale;

        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(root, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = pixelsPerUnit;

        var fill = CreateBarVisual(canvasGo.transform, backgroundColor, fillColor, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        canvas.enabled = false;
        return new RuntimeHealthBarReferences(root, canvas, fill);
    }

    public static RuntimeHealthBarReferences CreateScreenBar(
        Transform parent,
        string rootName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color backgroundColor,
        Color fillColor,
        Vector2 fillInsetMin,
        Vector2 fillInsetMax)
    {
        var root = new GameObject(rootName).transform;
        root.SetParent(parent, false);

        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        root.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.gameObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("BarPanel");
        panel.transform.SetParent(root, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var fill = CreateBarVisual(panel.transform, backgroundColor, fillColor, fillInsetMin, fillInsetMax);
        canvas.enabled = true;
        return new RuntimeHealthBarReferences(root, canvas, fill);
    }

    public static void ApplyFill(Image fillImage, float current, float max, Color lowColor, Color highColor)
    {
        if (fillImage == null || max <= 0.0001f)
            return;

        var normalized = Mathf.Clamp01(current / max);
        fillImage.fillAmount = normalized;
        fillImage.color = Color.Lerp(lowColor, highColor, normalized);
    }

    static Image CreateBarVisual(Transform parent, Color backgroundColor, Color fillColor, Vector2 insetMin, Vector2 insetMax)
    {
        var backgroundGo = new GameObject("Background");
        backgroundGo.transform.SetParent(parent, false);
        var background = backgroundGo.AddComponent<Image>();
        background.sprite = UiSprites.White;
        background.color = backgroundColor;
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, insetMin, insetMax);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(parent, false);
        var fill = fillGo.AddComponent<Image>();
        fill.sprite = UiSprites.White;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.color = fillColor;
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, insetMin, insetMax);
        return fill;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
