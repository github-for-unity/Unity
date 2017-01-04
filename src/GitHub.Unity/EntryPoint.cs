using System;
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
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        static void Initialize()
        {
            EditorApplication.update -= Initialize;

            Tasks.Initialize();

            Utility.Initialize();

            Installer.Initialize();

            Tasks.Run();

            ProjectWindowInterface.Initialize();

            Window.Initialize();
        }
    }
}