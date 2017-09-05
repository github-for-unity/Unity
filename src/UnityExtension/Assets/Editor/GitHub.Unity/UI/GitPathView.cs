using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class GitPathView : Subview
    {
        private const string GitInstallTitle = "Git installation";
        private const string GitInstallBrowseTitle = "Select git binary";
        private const string GitInstallPickInvalidTitle = "Invalid Git install";
        private const string GitInstallPickInvalidMessage = "The selected file is not a valid Git install. {0}";
        private const string GitInstallFindButton = "Find install";
        private const string GitInstallPickInvalidOK = "OK";

        public override void OnGUI()
        {
            string gitExecPath = null;
            string extension = null;
            string gitInstallPath = null;
            if (Environment != null)
            {
                extension = Environment.ExecutableExtension;
                if (Environment.IsWindows)
                {
                    extension = extension.TrimStart('.');
                }

                gitInstallPath = Environment.GitInstallPath;

                if (Environment.GitExecutablePath != null)
                    gitExecPath = Environment.GitExecutablePath.ToString();
            }

            // Install path
            GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(gitExecPath == null);
            {
                // Install path field
                EditorGUI.BeginChangeCheck();
                {
                    //TODO: Verify necessary value for a non Windows OS
                    Styles.PathField(ref gitExecPath,
                        () => EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                            gitInstallPath,
                            extension), ValidateGitInstall);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Logger.Trace("Setting GitExecPath: " + gitExecPath);

                    Manager.SystemSettings.Set(Constants.GitInstallPathKey, gitExecPath);
                    Environment.GitExecutablePath = gitExecPath.ToNPath();
                }

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                GUILayout.BeginHorizontal();
                {
                    // Find button - for attempting to locate a new install
                    if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
                    {
                        var task = new ProcessTask<NPath>(Manager.CancellationToken, new FirstLineIsPathOutputProcessor())
                            .Configure(Manager.ProcessManager, Environment.IsWindows ? "where" : "which", "git")
                            .FinallyInUI((success, ex, path) =>
                            {
                                if (success && !string.IsNullOrEmpty(path))
                                {
                                    Environment.GitExecutablePath = path;
                                    GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
                                }
                            });
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool ValidateGitInstall(string path)
        {
            if (String.IsNullOrEmpty(path))
                return false;
            if (!GitClient.ValidateGitInstall(path.ToNPath()))
            {
                EditorUtility.DisplayDialog(GitInstallPickInvalidTitle, String.Format(GitInstallPickInvalidMessage, path),
                    GitInstallPickInvalidOK);
                return false;
            }

            return true;
        }
    }
}
