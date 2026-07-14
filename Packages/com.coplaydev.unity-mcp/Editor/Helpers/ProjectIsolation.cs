using System;

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Safety policy for this embedded, project-owned package copy.
    /// Machine-global Unity preferences are disabled unless the editor process
    /// was explicitly started with an opt-in environment variable.
    /// </summary>
    internal static class ProjectIsolation
    {
        internal static bool AllowGlobalEditorPrefs => string.Equals(
            Environment.GetEnvironmentVariable("UNITY_MCP_ALLOW_GLOBAL_EDITOR_PREFS"),
            "1",
            StringComparison.Ordinal);
    }
}
