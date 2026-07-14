using System;
using MCPForUnity.Editor.Constants;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Migrations
{
    /// <summary>
    /// Detects legacy embedded-server preferences and migrates configs to the new uvx/stdio path once.
    /// </summary>
    [InitializeOnLoad]
    internal static class LegacyServerSrcMigration
    {
        private const string ServerSrcKey = EditorPrefKeys.ServerSrc;
        private const string UseEmbeddedKey = EditorPrefKeys.UseEmbeddedServer;

        static LegacyServerSrcMigration()
        {
            // Project isolation patch: user-level legacy preference migration is opt-in only.
            if (!string.Equals(
                    Environment.GetEnvironmentVariable("UNITY_MCP_ALLOW_CLIENT_CONFIG_WRITES"),
                    "1",
                    StringComparison.Ordinal)) return;
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += RunMigrationIfNeeded;
        }

        private static void RunMigrationIfNeeded()
        {
            EditorApplication.delayCall -= RunMigrationIfNeeded;

            bool hasServerSrc = EditorPrefs.HasKey(ServerSrcKey);
            bool hasUseEmbedded = EditorPrefs.HasKey(UseEmbeddedKey);

            if (!hasServerSrc && !hasUseEmbedded)
            {
                return;
            }

            try
            {
                McpLog.Info("Detected legacy embedded MCP server configuration. Updating all client configs...");

                var summary = MCPServiceLocator.Client.ConfigureAllDetectedClients();

                if (summary.FailureCount > 0)
                {
                    McpLog.Warn($"Legacy configuration migration finished with errors ({summary.GetSummaryMessage()}). details:");
                    if (summary.Messages != null)
                    {
                        foreach (var message in summary.Messages)
                        {
                            McpLog.Warn($"  {message}");
                        }
                    }
                    McpLog.Warn("Legacy keys will be removed to prevent migration loop. Please configure failing clients manually.");
                }
                else
                {
                    McpLog.Info($"Legacy configuration migration complete ({summary.GetSummaryMessage()})");
                }

                if (hasServerSrc)
                {
                    EditorPrefs.DeleteKey(ServerSrcKey);
                    McpLog.Info("  ✓ Removed legacy key: MCPForUnity.ServerSrc");
                }

                if (hasUseEmbedded)
                {
                    EditorPrefs.DeleteKey(UseEmbeddedKey);
                    McpLog.Info("  ✓ Removed legacy key: MCPForUnity.UseEmbeddedServer");
                }
            }
            catch (Exception ex)
            {
                McpLog.Error($"Legacy MCP server migration failed: {ex.Message}");
            }
        }
    }
}
