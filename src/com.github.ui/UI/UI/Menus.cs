using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VersionControl.Git;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity.UI
{
    [InitializeOnLoad]
    class Menus : ScriptableObject
    {
#if DEVELOPER_BUILD

        private const string Menu_Window_Git = "Window/GitHub";
        private const string Menu_Window_Git_Command_Line = "Window/GitHub Command Line";
#else
        private const string Menu_Window_Git = "Window/GitHub";
        private const string Menu_Window_Git_Command_Line = "Window/GitHub Command Line";
#endif

        [MenuItem(Menu_Window_Git)]
        public static void Window_Git()
        {
            ShowWindow(EntryPoint.ApplicationManager);
        }

        [MenuItem(Menu_Window_Git_Command_Line)]
        public static void Git_CommandLine()
        {
            EntryPoint.ApplicationManager.ProcessManager.RunCommandLineWindow(NPath.CurrentDirectory);
            EntryPoint.ApplicationManager.UsageTracker.IncrementApplicationMenuMenuItemCommandLine();
        }

#if DEVELOPER_BUILD

        [MenuItem("GitHub/Select Window")]
        public static void Git_SelectWindow()
        {
            var window = Resources.FindObjectsOfTypeAll(typeof(Window)).FirstOrDefault() as Window;
            Selection.activeObject = window;
        }

        [MenuItem("GitHub/Restart")]
        public static void Git_Restart()
        {
            EntryPoint.Restart();
        }
#endif

        public static void ShowWindow(IApplicationManager applicationManager)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            var window = EditorWindow.GetWindow<Window>(type);
            window.InitializeWindow(applicationManager);
            window.Show();
        }

    }
}
