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
            Debug.Log("MetricsInitialization.Load");
            EditorApplication.update += Initialize;
        }

        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            Debug.Log("MetricsInitialization.Initialize");

            UsageTracker.SetMetricsService(new MetricsService(string.Format("GitHub4Unity{0}", AssemblyVersionInformation.Version)));
        }
    }
}