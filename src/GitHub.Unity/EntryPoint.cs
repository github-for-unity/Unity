using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [InitializeOnLoad]
    class EntryPoint
    {
        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            Debug.Log("Entry Point");
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            Tasks.Initialize();

            Utility.Initialize();

            Installer.Initialize();

            StatusService.Initialize();

            Tasks.Run();

            ProjectWindowInterface.Initialize();

            Window.Initialize();
        }
    }
}
