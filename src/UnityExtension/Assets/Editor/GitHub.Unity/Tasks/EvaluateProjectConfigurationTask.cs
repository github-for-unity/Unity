using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GitHub.Unity
{
    [Flags]
    enum ProjectSettingsEvaluation
    {
        None = 0,
        EditorSettingsMissing = 1 << 0,
        BadVCSSettings = 1 << 1,
        BinarySerialization = 1 << 2,
        MixedSerialization = 1 << 3
    }

    enum GitIgnoreRuleEffect
    {
        Require = 0,
        Disallow = 1
    }

    abstract class ProjectConfigurationIssue
    {}

    class ProjectSettingsIssue : ProjectConfigurationIssue
    {
        public ProjectSettingsIssue(ProjectSettingsEvaluation evaluation)
        {
            Evaluation = evaluation;
        }

        public bool WasCaught(ProjectSettingsEvaluation evaluation)
        {
            return (Evaluation & evaluation) != 0;
        }

        public ProjectSettingsEvaluation Evaluation { get; protected set; }
    }

    class GitIgnoreException : ProjectConfigurationIssue
    {
        public GitIgnoreException(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; protected set; }
    }

    class GitIgnoreIssue : ProjectConfigurationIssue
    {
        public GitIgnoreIssue(string file, string line, string description)
        {
            File = file;
            Line = line;
            Description = description;
        }

        public string File { get; protected set; }
        public string Line { get; protected set; }
        public string Description { get; protected set; }
    }

    struct GitIgnoreRule
    {
        private const string CountKey = "GitIgnoreRuleCount";
        private const string EffectKey = "GitIgnoreRule{0}Effect";
        private const string FileKey = "GitIgnoreRule{0}File";
        private const string LineKey = "GitIgnoreRule{0}Line";
        private const string TriggerTextKey = "GitIgnoreRule{0}TriggerText";

        public static bool TryLoad(int index, out GitIgnoreRule result)
        {
            result = new GitIgnoreRule();

            int effect;
            if (!int.TryParse(EntryPoint.ApplicationManager.LocalSettings.Get(String.Format(EffectKey, index), "-1"), out effect) || effect < 0)
            {
                return false;
            }

            result.Effect = (GitIgnoreRuleEffect)effect;

            result.FileString = EntryPoint.ApplicationManager.LocalSettings.Get(String.Format(FileKey, index));

            try
            {
                result.File = new Regex(result.FileString);
            }
            catch (ArgumentException)
            {
                result.File = null;
            }

            result.LineString = EntryPoint.ApplicationManager.LocalSettings.Get(String.Format(LineKey, index));

            try
            {
                result.Line = new Regex(result.LineString);
            }
            catch (ArgumentException)
            {
                result.Line = null;
            }

            result.TriggerText = EntryPoint.ApplicationManager.LocalSettings.Get(String.Format(TriggerTextKey, index));

            return true;
        }

        public static void Save(int index, GitIgnoreRuleEffect effect, string file, string line, string triggerText)
        {
            EntryPoint.ApplicationManager.LocalSettings.Set(String.Format(EffectKey, index), ((int)effect).ToString());
            EntryPoint.ApplicationManager.LocalSettings.Set(String.Format(FileKey, index), file);
            EntryPoint.ApplicationManager.LocalSettings.Set(String.Format(LineKey, index), line);
            EntryPoint.ApplicationManager.LocalSettings.Set(String.Format(TriggerTextKey, index), triggerText);
        }

        public static void New()
        {
            Save(Count, GitIgnoreRuleEffect.Require, "", "", "");
            EntryPoint.ApplicationManager.LocalSettings.Set(CountKey, (Count + 1));
        }

        public static void Delete(int index)
        {
            EntryPoint.ApplicationManager.LocalSettings.Unset(String.Format(EffectKey, index));
            EntryPoint.ApplicationManager.LocalSettings.Unset(String.Format(FileKey, index));
            EntryPoint.ApplicationManager.LocalSettings.Unset(String.Format(LineKey, index));
            EntryPoint.ApplicationManager.LocalSettings.Unset(String.Format(TriggerTextKey, index));

            var count = Count;
            for (; index < count; ++index)
            {
                EntryPoint.ApplicationManager.LocalSettings.Rename(String.Format(EffectKey, index), String.Format(EffectKey, index - 1));
                EntryPoint.ApplicationManager.LocalSettings.Rename(String.Format(FileKey, index), String.Format(FileKey, index - 1));
                EntryPoint.ApplicationManager.LocalSettings.Rename(String.Format(LineKey, index), String.Format(LineKey, index - 1));
                EntryPoint.ApplicationManager.LocalSettings.Rename(String.Format(TriggerTextKey, index), String.Format(TriggerTextKey, index - 1));
            }

            EntryPoint.ApplicationManager.LocalSettings.Set(CountKey, (count - 1));
        }

        public override string ToString()
        {
            return String.Format("{0} \"{1}\" in \"{2}\": {3}", Effect, Line, File, TriggerText);
        }

        public static int Count
        {
            get { return Mathf.Max(0, EntryPoint.ApplicationManager.LocalSettings.Get(CountKey, 0)); }
        }

        public GitIgnoreRuleEffect Effect { get; private set; }
        public string FileString { get; private set; }
        public string LineString { get; private set; }
        public Regex File { get; private set; }
        public Regex Line { get; private set; }
        public string TriggerText { get; private set; }
    }

    class EvaluateProjectConfigurationTask : DataTaskBase<ProjectConfigurationIssue, List<ProjectConfigurationIssue>>
    {
        private const string GitIgnoreFilePattern = ".gitignore";
        private const string VCSPropertyName = "m_ExternalVersionControlSupport";
        private const string SerializationPropertyName = "m_SerializationMode";
        private const string VisibleMetaFilesValue = "Visible Meta Files";
        private const string HiddenMetaFilesValue = "Hidden Meta Files";

        private const int ThreadSyncDelay = 100;

        public static string EditorSettingsPath = "ProjectSettings/EditorSettings.asset";

        public static void RegisterCallback(Action<IEnumerable<ProjectConfigurationIssue>> callback)
        {
        }

        public static void UnregisterCallback(Action<IEnumerable<ProjectConfigurationIssue>> callback)
        {
        }

        public static Object LoadEditorSettings()
        {
            return InternalEditorUtility.LoadSerializedFileAndForget(EditorSettingsPath).FirstOrDefault();
        }

        public EvaluateProjectConfigurationTask(CancellationToken token)
            : base(token)
        {
        }

        protected override List<ProjectConfigurationIssue> RunWithReturn(bool success)
        {
            RaiseOnStart();

            foreach (var issue in EvaluateLocalConfiguration())
                RaiseOnData(issue);

            // Git config
            foreach (var issue in EvaluateGitIgnore())
                RaiseOnData(issue);

            RaiseOnEnd();

            return Result;
        }

        private IEnumerable<ProjectConfigurationIssue> EvaluateLocalConfiguration()
        {
            var result = ProjectSettingsEvaluation.None;

            var settingsAsset = LoadEditorSettings();
            if (settingsAsset == null)
            {
                result |= ProjectSettingsEvaluation.EditorSettingsMissing;
                yield break;
            }

            var settingsObject = new SerializedObject(settingsAsset);

            var vcsSetting = settingsObject.FindProperty(VCSPropertyName).stringValue;
            if (!vcsSetting.Equals(VisibleMetaFilesValue) && !vcsSetting.Equals(HiddenMetaFilesValue))
            {
                result |= ProjectSettingsEvaluation.BadVCSSettings;
            }

            var serializationSetting = (SerializationSetting)settingsObject.FindProperty(SerializationPropertyName).intValue;
            if (serializationSetting == SerializationSetting.ForceBinary)
            {
                result |= ProjectSettingsEvaluation.BinarySerialization;
            }
            else if (serializationSetting == SerializationSetting.Mixed)
            {
                result |= ProjectSettingsEvaluation.MixedSerialization;
            }

            if (result != ProjectSettingsEvaluation.None)
            {
                yield return new ProjectSettingsIssue(result);
            }
        }

        private IEnumerable<ProjectConfigurationIssue> EvaluateGitIgnore()
        {
            // Read rules
            var rules = new List<GitIgnoreRule>(GitIgnoreRule.Count);
            for (var index = 0; index < rules.Capacity; ++index)
            {
                GitIgnoreRule rule;
                if (GitIgnoreRule.TryLoad(index, out rule))
                {
                    rules.Add(rule);
                }
            }

            if (!rules.Any())
            {
                yield break;
            }

            // Read gitignore files
            GitIgnoreFile[] files;
            files =
                Directory.GetFiles(Utility.GitRoot, GitIgnoreFilePattern, SearchOption.AllDirectories)
                            .Select(p => new GitIgnoreFile(p))
                            .ToArray();

            if (files.Length < 1)
            {
                yield break;
            }

            // Evaluate each rule
            for (var ruleIndex = 0; ruleIndex < rules.Count; ++ruleIndex)
            {
                var rule = rules[ruleIndex];
                for (var fileIndex = 0; fileIndex < files.Length; ++fileIndex)
                {
                    var file = files[fileIndex];
                    // Check against all files with matching path
                    if (rule.File == null || !rule.File.IsMatch(file.Path))
                    {
                        continue;
                    }

                    // Validate all lines in that file
                    for (var lineIndex = 0; lineIndex < file.Contents.Length; ++lineIndex)
                    {
                        var line = file.Contents[lineIndex];
                        var match = rule.Line != null && rule.Line.IsMatch(line);

                        if (rule.Effect == GitIgnoreRuleEffect.Disallow && match)
                            // This line is not allowed
                        {
                            yield return new GitIgnoreIssue(file.Path, line, rule.TriggerText);
                        }
                        else if (rule.Effect == GitIgnoreRuleEffect.Require)
                            // If the line is required, see if we're there
                        {
                            if (match)
                                // We found it! No sense in searching further in this file.
                            {
                                break;
                            }
                            else if (lineIndex == file.Contents.Length - 1)
                                // We reached the last line without finding it
                            {
                                yield return new GitIgnoreIssue(file.Path, string.Empty, rule.TriggerText);
                            }
                        }
                    }
                }
            }
        }

        public string Name { get { return "Project Evaluation"; } }

        private enum SerializationSetting
        {
            Mixed = 0,
            ForceBinary = 1,
            ForceText = 2
        }

        private struct GitIgnoreFile
        {
            public string Path { get; private set; }
            public string[] Contents { get; private set; }

            public GitIgnoreFile(string path)
            {
                Path = path.Substring(Utility.GitRoot.Length + 1);
                Contents = File.ReadAllLines(path).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
            }

            public override string ToString()
            {
                return String.Format("{0}:{1}{2}", Path, Environment.NewLine, string.Join(Environment.NewLine, Contents));
            }
        }
    }
}
