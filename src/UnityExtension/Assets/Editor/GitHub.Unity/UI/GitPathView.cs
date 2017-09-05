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
        private const string BrowseButton = "...";
        private const string GitInstallTitle = "Git installation";
        private const string GitInstallBrowseTitle = "Select git binary";
        private const string GitInstallPickInvalidTitle = "Invalid Git install";
        private const string GitInstallPickInvalidMessage = "The selected file is not a valid Git install. {0}";
        private const string GitInstallFindButton = "Find install";
        private const string GitInstallPickInvalidOK = "OK";

        [NonSerialized] private bool isBusy;

        public override bool IsBusy
        {
            get { return isBusy; }
        }

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

            EditorGUI.BeginDisabledGroup(IsBusy || Parent.IsBusy || gitExecPath == null);
            {
                // Install path field
                EditorGUI.BeginChangeCheck();
                {
                    GUILayout.BeginHorizontal();
                    gitExecPath = EditorGUILayout.TextField("Path to Git", gitExecPath);
                    if (GUILayout.Button(BrowseButton, EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        var newValue = EditorUtility.OpenFilePanel(GitInstallBrowseTitle, gitInstallPath, extension);

                        if (!string.IsNullOrEmpty(newValue))
                        {
                            isBusy = true;

                            var validateGitInstall = !string.IsNullOrEmpty(newValue);

                            if (validateGitInstall && !GitClient.ValidateGitInstall(newValue.ToNPath()))
                            {
                                EditorUtility.DisplayDialog(GitInstallPickInvalidTitle, 
                                    String.Format(GitInstallPickInvalidMessage, newValue),
                                    GitInstallPickInvalidOK);

                                validateGitInstall = false;
                            }

                            if (validateGitInstall)
                            {
                                gitExecPath = newValue;
                                GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
                                GUI.changed = true;
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Logger.Trace("Setting GitExecPath: " + gitExecPath);

                    Manager.SystemSettings.Set(Constants.GitInstallPathKey, gitExecPath);
                    Environment.GitExecutablePath = gitExecPath.ToNPath();

                    isBusy = false;
                }

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                GUILayout.BeginHorizontal();
                {
                    // Find button - for attempting to locate a new install
                    if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
                    {
                        isBusy = true;

                        var task = new ProcessTask<NPath>(Manager.CancellationToken, new FirstLineIsPathOutputProcessor())
                            .Configure(Manager.ProcessManager, Environment.IsWindows ? "where" : "which", "git")
                            .FinallyInUI((success, ex, path) =>
                            {
                                if (success && !string.IsNullOrEmpty(path))
                                {
                                    Environment.GitExecutablePath = path;
                                    GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
                                }

                                isBusy = false;
                            });
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
