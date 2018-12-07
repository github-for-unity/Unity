# Using the API

GitHub for Unity provides access to a git client to help users create their own tools to assist in their workflow.

Users can separate the user interface from the API by removing `GitHub.Unity.dll`. All other libraries are required by the API.

## Creating an instance of `GitClient`
```cs
var defaultEnvironment = new DefaultEnvironment();
defaultEnvironment.Initialize(null, NPath.Default, NPath.Default, NPath.Default, Application.dataPath.ToNPath());

var processEnvironment = new ProcessEnvironment(defaultEnvironment);
var processManager = new ProcessManager(defaultEnvironment, processEnvironment, TaskManager.Instance.Token);

var gitClient = new GitClient(defaultEnvironment, processManager, TaskManager.Instance.Token);
```

## Full Example
This example creates a window that has a single button which commits all changes.
```cs
using System;
using System.Globalization;
using GitHub.Unity;
using UnityEditor;
using UnityEngine;

public class CustomGitEditor : EditorWindow
{
    [MenuItem("Window/Custom Git")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(CustomGitEditor));
    }

    [NonSerialized] private GitClient gitClient;

    public void OnEnable()
    {
        InitGitClient();
    }

    private void InitGitClient()
    {
        if (gitClient != null) return;

        Debug.Log("Init GitClient");

        var defaultEnvironment = new DefaultEnvironment();
        defaultEnvironment.Initialize(null, NPath.Default, NPath.Default, 
            NPath.Default, Application.dataPath.ToNPath());

        var processEnvironment = new ProcessEnvironment(defaultEnvironment);
        var processManager = new ProcessManager(defaultEnvironment, processEnvironment, TaskManager.Instance.Token);

        gitClient = new GitClient(defaultEnvironment, processManager, TaskManager.Instance.Token);
    }

    void OnGUI()
    {
        GUILayout.Label("Custom Git Window", EditorStyles.boldLabel);

        if (GUILayout.Button("Commit Stuff"))
        {
            var message = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            var body = string.Empty;

            gitClient.AddAll()
                .Then(gitClient.Commit(message, body))
                .Start();
        }
    }
}
```


