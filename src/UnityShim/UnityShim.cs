using System;
using UnityEditor;
namespace GitHub.Unity
{
    public static class UnityShim
    {
        public static event Action<Editor> Editor_finishedDefaultHeaderGUI;
        public static void Raise_Editor_finishedDefaultHeaderGUI(Editor editor)
        {
            if (Editor_finishedDefaultHeaderGUI != null)
                Editor_finishedDefaultHeaderGUI(editor);
        }
    }
}