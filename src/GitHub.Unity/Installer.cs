using UnityEngine;
using UnityEditor;
using System;


namespace GitHub.Unity
{
    class Installer : ScriptableObject
    {
        const string PackageName = "GitHub extensions";
        const string
            QueryTitle = "Embed " + PackageName + "?",
            QueryMessage = "This package has no project dependencies and so can either run embedded in your Unity install or remain in your assets folder.\n\nWould you like to embed it?",
            QueryOK = "Embed",
            QueryCancel = "Cancel",
            ErrorTitle = "Installer error",
            ErrorMessage = "An error occured during installation:\n{0}",
            ErrorOK = "OK";

        public static void Initialize()
        {
            if (Utility.IsDevelopmentBuild)
            {
                return;
            }

            // Detect install path
            string selfPath;
            Installer instance = FindObjectOfType(typeof(Installer)) as Installer;
            if (instance == null)
            {
                instance = CreateInstance<Installer>();
            }
            MonoScript script = MonoScript.FromScriptableObject(instance);
            if (script == null)
            {
                selfPath = string.Empty;
            }
            else
            {
                selfPath = AssetDatabase.GetAssetPath(script);
            }
            DestroyImmediate(instance);

            if (string.IsNullOrEmpty(selfPath))
            // If we cannot self-locate then forget the whole thing
            {
                return;
            }

            if (EditorUtility.DisplayDialog(QueryTitle, QueryMessage, QueryOK, QueryCancel))
            // Perform move
            {
                MoveFrom(Application.dataPath + selfPath.Substring("Assets".Length, selfPath.LastIndexOf('/') - "Assets".Length));
            }

            // Self-delete
            AssetDatabase.DeleteAsset(selfPath);
        }


        static void MoveFrom(string path)
        {
            try
            {
                Debug.LogFormat("Installer move from '{0}'", path);
                // TODO: Create the necessary structure and perform the actual move of files into it from the given install path
            }
            catch (Exception e)
            {
                Failure(e.ToString());
            }
        }


        static void Failure(string error)
        {
            EditorUtility.DisplayDialog(ErrorTitle, string.Format(ErrorMessage, error), ErrorOK);
        }
    }
}

