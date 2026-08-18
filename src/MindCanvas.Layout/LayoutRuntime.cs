namespace MindCanvas.Layout;

public static class LayoutRuntime
{
    private static string _currentId = "logic-right";

    public static string CurrentId
    {
        get => _currentId;
        set => _currentId = value is "logic-right" or "mindmap-balanced" or "logic-down"
            ? value
            : "logic-right";
    }

    public static Guid? FocusRootNodeId { get; set; }

    public static void ResetFocus() => FocusRootNodeId = null;
}
