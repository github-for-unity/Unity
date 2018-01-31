using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace GitHub.Unity
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class LocationAttribute : Attribute
    {
        public enum Location { PreferencesFolder, ProjectFolder, LibraryFolder, UserFolder }
        public string filepath { get; set; }
        public LocationAttribute(string relativePath, Location location)
        {
            Guard.ArgumentNotNullOrWhiteSpace(relativePath, "relativePath");

            if (relativePath[0] == '/')
                relativePath = relativePath.Substring(1);

            if (location == Location.PreferencesFolder)
                filepath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
            else if (location == Location.UserFolder)
                filepath = EntryPoint.Environment.UserCachePath.Combine(relativePath).ToString(SlashMode.Forward);
            else if (location == Location.LibraryFolder)
                filepath = EntryPoint.Environment.UnityProjectPath.Combine("Library", "gfu", relativePath);
        }
    }


    class ScriptObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private string filePath = null;
        private NPath nFilePath = null;
        private NPath FilePath
        {
            get
            {
                if (nFilePath == null)
                {
                    if (string.IsNullOrEmpty(filePath))
                        return null;
                    if (filePath == null)
                        filePath = GetFilePath();
                    if (filePath == null)
                        filePath = "";
                    else
                        nFilePath = filePath.ToNPath();
                }
                return nFilePath;
            }
        }

        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                    CreateAndLoad();
                return instance;
            }
        }

        protected ScriptObjectSingleton()
        {
            if (instance != null)
            {
                Logging.Instance.Error("Singleton already exists!");
            }
            else
            {
                instance = this as T;
                System.Diagnostics.Debug.Assert(instance != null);
            }
        }

        private static void CreateAndLoad()
        {
            System.Diagnostics.Debug.Assert(instance == null);

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            }

            if (instance == null)
            {
                var inst = CreateInstance<T>() as ScriptObjectSingleton<T>;
                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.Save(true);
            }

            System.Diagnostics.Debug.Assert(instance != null);
        }

        protected virtual void Save(bool saveAsText)
        {
            if (instance == null)
            {
                Logging.Instance.Error("Cannot save singleton, no instance!");
                return;
            }

            NPath locationFilePath = GetFilePath();
            if (locationFilePath != null)
            {
                locationFilePath.Parent.EnsureDirectoryExists();
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance }, locationFilePath, saveAsText);
            }
        }

        private static NPath GetFilePath()
        {
            var attr = typeof(T).GetCustomAttributes(true)
                                .Select(t => t as LocationAttribute)
                                .FirstOrDefault(t => t != null);
            //Logging.Instance.Debug("FilePath {0}", attr != null ? attr.filepath : null);

            return attr != null ? attr.filepath.ToNPath() : null;
        }
    }
}