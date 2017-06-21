using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class MetricsInitialization
    {
        [InitializeOnLoadMethod]
        static void Load()
        {
            EditorApplication.update += Initialize;
        }

        private static void Initialize()
        {
            EditorApplication.update -= Initialize;
            UsageTracker.SetMetricsService(new MetricsService(string.Format("GitHub4Unity{0}", AssemblyVersionInformation.Version)));
        }
    }
}