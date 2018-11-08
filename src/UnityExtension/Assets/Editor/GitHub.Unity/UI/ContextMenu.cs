using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    public class ContextMenu
    {
        [MenuItem("Assets/Git/History", false)]
        private static void GitFileHistory()
        {
            if (Selection.assetGUIDs != null)
            {
                int maxWindowsToOpen = 10;
                int windowsOpened = 0;
                foreach(var guid in Selection.assetGUIDs)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    FileHistoryWindow.OpenWindow(assetPath);
                    windowsOpened++;
                    if (windowsOpened >= maxWindowsToOpen)
                    {
                        break;
                    }
                }
            }
        }

        [MenuItem("Assets/Git/History", true)]
        private static bool GitFileHistoryValidation()
        {
            return Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;
        }
    }
}