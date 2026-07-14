using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GenericGachaRPG.Editor
{
    /// <summary>
    /// Starts the MCP for Unity stdio bridge without using the package's global
    /// Codex configurator or HTTP server. Runtime state stays under _ProjectTools.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityMcpProjectBootstrap
    {
        private const string BridgeTypeName =
            "MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost, MCPForUnity.Editor";

        private const int MaxAttempts = 40;
        private static int attempts;
        private static double nextAttemptAt;

        static UnityMcpProjectBootstrap()
        {
            ConfigureProcessIsolation();
            ScheduleStart();
        }

        [MenuItem("Tools/Generic Gacha RPG/Ensure Unity MCP Bridge")]
        private static void EnsureBridgeFromMenu()
        {
            ScheduleStart();
        }

        private static void ScheduleStart()
        {
            attempts = 0;
            nextAttemptAt = EditorApplication.timeSinceStartup + 0.25d;
            EditorApplication.update -= TryStartWhenReady;
            EditorApplication.update += TryStartWhenReady;
        }

        private static void TryStartWhenReady()
        {
            if (EditorApplication.timeSinceStartup < nextAttemptAt)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                nextAttemptAt = EditorApplication.timeSinceStartup + 0.5d;
                return;
            }

            try
            {
                ConfigureProcessIsolation();

                Type bridgeType = Type.GetType(BridgeTypeName, throwOnError: false);
                if (bridgeType == null)
                {
                    RetryOrStop("MCPForUnity.Editor assembly is not ready.");
                    return;
                }

                PropertyInfo isRunningProperty = bridgeType.GetProperty(
                    "IsRunning",
                    BindingFlags.Public | BindingFlags.Static);
                bool isRunning = isRunningProperty != null &&
                    Convert.ToBoolean(isRunningProperty.GetValue(null));

                if (!isRunning)
                {
                    MethodInfo startMethod = bridgeType.GetMethod(
                        "Start",
                        BindingFlags.Public | BindingFlags.Static);
                    if (startMethod == null)
                    {
                        RetryOrStop("MCP stdio bridge Start method was not found.");
                        return;
                    }

                    startMethod.Invoke(null, null);
                    isRunning = isRunningProperty != null &&
                        Convert.ToBoolean(isRunningProperty.GetValue(null));
                }

                if (!isRunning)
                {
                    RetryOrStop("MCP stdio bridge did not enter the running state.");
                    return;
                }

                EditorApplication.update -= TryStartWhenReady;
                Debug.Log("[GenericGachaRPG][UNITY_MCP_BRIDGE_READY] Project-isolated stdio bridge is running.");
            }
            catch (TargetInvocationException exception)
            {
                RetryOrStop(exception.InnerException?.Message ?? exception.Message);
            }
            catch (Exception exception)
            {
                RetryOrStop(exception.Message);
            }
        }

        private static void ConfigureProcessIsolation()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string statusDirectory = Path.Combine(
                projectRoot,
                "_ProjectTools",
                "runtime",
                "UnityMCPStatus");

            Directory.CreateDirectory(statusDirectory);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_STATUS_DIR",
                statusDirectory,
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_PROJECT_ISOLATED",
                "1",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_PROJECT_SCOPED_TOOLS",
                "1",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_CLIENT_CONFIG_WRITES",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_HTTP",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_SETUP_UI",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_UPDATE_CHECKS",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_EDITOR_PREF_THROTTLE",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_GLOBAL_EDITOR_PREFS",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_ALLOW_STDIO_AUTOSTART",
                "0",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "UNITY_MCP_DISABLE_TELEMETRY",
                "1",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "MCP_DISABLE_TELEMETRY",
                "1",
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                "DISABLE_TELEMETRY",
                "1",
                EnvironmentVariableTarget.Process);
        }

        private static void RetryOrStop(string reason)
        {
            attempts++;
            if (attempts < MaxAttempts)
            {
                nextAttemptAt = EditorApplication.timeSinceStartup + 0.5d;
                return;
            }

            EditorApplication.update -= TryStartWhenReady;
            Debug.LogWarning($"[GenericGachaRPG][UNITY_MCP_BRIDGE_FAILED] {reason}");
        }
    }
}
