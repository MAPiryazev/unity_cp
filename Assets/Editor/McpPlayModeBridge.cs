using UnityEditor;

public static class McpPlayModeBridge
{
    [MenuItem("Tools/MCP/Toggle Play Mode")]
    public static void TogglePlayMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/MCP/Enter Play Mode")]
    public static void EnterPlayMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/MCP/Exit Play Mode")]
    public static void ExitPlayMode()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.isPlaying = false;
    }
}
