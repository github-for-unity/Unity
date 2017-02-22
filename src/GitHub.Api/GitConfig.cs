using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    public struct ConfigRemote
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1}", Name, Url);
        }
    }

    public struct ConfigBranch
    {
        public string Name { get; set; }
        public ConfigRemote? Remote { get; set; }
        public bool IsTracking => Remote.HasValue;

        public override string ToString()
        {
            return String.Format("{0} tracking:{1} remote:{2}", Name, IsTracking, Remote);
        }
    }

    class GitConfig
    {
        private readonly ConfigFileManager manager;
        private SectionParser sectionParser;
        private Dictionary<string, Section> sections;
        private Dictionary<string, Dictionary<string, Section>> groups;

        public GitConfig(string filePath)
        {
            manager = new ConfigFileManager(filePath);
            Reset();
        }

        public void Reset()
        {
            manager.Refresh();
            sectionParser = new SectionParser(manager);
            sections = sectionParser.Sections;
            groups = sectionParser.GroupSections;
        }

        public IEnumerable<ConfigBranch> GetBranches()
        {
            return groups
                .Where(x => x.Key == "branch")
                .SelectMany(x => x.Value)
                .Select(x => new ConfigBranch
                {
                    Name = x.Key,
                    Remote = GetRemote(x.Value.TryGetString("remote"))
                });
        }

        public IEnumerable<ConfigRemote> GetRemotes()
        {
            return groups
                .Where(x => x.Key == "remote")
                .SelectMany(x => x.Value)
                .Select(x => new ConfigRemote
                {
                    Name = x.Key,
                    Url = x.Value.GetString("url")
                });
        }

        public ConfigRemote? GetRemote(string remote)
        {
            return groups
                .Where(x => x.Key == "remote")
                .SelectMany(x => x.Value)
                .Where(x => x.Key == remote)
                .Select(x => new ConfigRemote
                {
                    Name = x.Key,
                    Url = x.Value.GetString("url")
                } as ConfigRemote?)
                .FirstOrDefault();
        }

        public ConfigBranch? GetBranch(string branch)
        {
            return groups
                .Where(x => x.Key == "branch")
                .SelectMany(x => x.Value)
                .Where(x => x.Key == branch)
                .Select(x => new ConfigBranch
                {
                    Name = x.Key,
                    Remote = GetRemote(x.Value.TryGetString("remote"))
                } as ConfigBranch?)
                .FirstOrDefault();
        }

        public bool TryGet<T>(string section, string key, out T value)
        {
            value = default(T);
            Section sect = null;
            var ret = sections.TryGetValue(section, out sect);
            if (ret)
            {
                if (value is string)
                    value = (T)(object)sect.GetString(key);
                else if (value is float)
                    value = (T)(object)sect.GetFloat(key);
                else
                    value = (T)(object)sect.GetInt(key);
            }
            return ret;
        }

        public string GetString(string section, string key)
        {
            return sections[section].GetString(key);
        }

        public float GetFloat(string section, string key)
        {
            return sections[section].GetFloat(key);
        }

        public int GetInt(string section, string key)
        {
            return sections[section].GetInt(key);
        }

        public void Set<T>(string section, string key, T value)
        {
            if (value is string)
                SetString(section, key, (string)(object)value);
            else if (value is float)
                SetFloat(section, key, (float)(object)value);
            else
                SetInt(section, key, (int)(object)value);
        }

        public void SetString(string section, string key, string value)
        {
            SetAndWrite(section, key, value);
        }

        public void SetFloat(string section, string key, float value)
        {
            SetString(section, key, value.ToString());
        }

        public void SetInt(string section, string key, int value)
        {
            SetString(section, key, value.ToString());
        }

        private void SetAndWrite(string section, string key, string value)
        {
            var s = sections[section];
            if (s.ContainsKey(key) && s.GetString(key) == value) return;

            s.SetString(key, value);
            var sb = new StringBuilder();
            sections.All(kvp => {
                sb.AppendFormat("{0}{1}", kvp.Value.ToString(), Environment.NewLine);
                return true;
            });
            manager.Save(sb.ToString());
        }

        class Section : Dictionary<string, string>
        {
            public Section(string name, string description = null)
            {
                Name = name;
                Description = description;
            }

            public string TryGetString(string key)
            {
                if (ContainsKey(key))
                    return this[key];
                return null;
            }

            public string GetString(string key)
            {
                return this[key];
            }

            public int GetInt(string key)
            {
                var value = this[key];
                var result = 0;
                var success = int.TryParse(value, out result);
                return result;
            }

            public float GetFloat(string key)
            {
                var value = this[key];
                var result = 0F;
                var success = float.TryParse(value, out result);
                return result;
            }

            public void SetString(string key, string value)
            {
                this[key] = value;
            }

            public void SetInt(string key, int value)
            {
                SetString(key, value.ToString());
            }

            public void SetFloat(string key, float value)
            {
                SetString(key, value.ToString());
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendFormat("[{0}]\r\n", Name);
                foreach (var kvp in this)
                    sb.AppendFormat("{0}: {1}\r\n", kvp.Key, kvp.Value);

                return sb.ToString();
            }

            public string Name { get; private set; }
            public string Description { get; private set; }
        }

        class SectionParser
        {
            private static readonly Regex SectionPattern = new Regex(@"^\[(.*)\]$", RegexOptions.Compiled);
            private static readonly Regex PairPattern = new Regex(@"([\S][^=]+)[\s]*=[\s]*(.*)", RegexOptions.Compiled);
            private static readonly Regex GroupSectionPattern = new Regex(@"(.*?(?=""))", RegexOptions.Compiled);
            private readonly ConfigFileManager manager;

            public SectionParser(ConfigFileManager manager)
            {
                this.manager = manager;
                EnsureFileBeginsWithSection();
                InitSections();
            }

            private void InitSections()
            {
                Sections = new Dictionary<string, Section>();
                GroupSections = new Dictionary<string, Dictionary<string, Section>>();
                manager.Lines.All(l => {
                    ParseLine(l);
                    return true;
                });
            }

            private void ParseLine(string line)
            {
                if (SectionPattern.IsMatch(line))
                    InitNewSectionFromLine(line);
                if (PairPattern.IsMatch(line))
                    AddKeyValuePairToLastSectionFromLine(line);
            }

            private void InitNewSectionFromLine(string line)
            {
                var match = SectionPattern.Match(line);
                var sectionKey = match.Groups[1].Value.Trim();
                string description = null;
                string groupKey = null;
                if (GroupSectionPattern.IsMatch(sectionKey))
                {
                    var match1 = GroupSectionPattern.Match(sectionKey);
                    groupKey = match1.Value.Trim();
                    match1 = match1.NextMatch().NextMatch();
                    description = match1.Value.Trim();
                }
                Section section = null;
                if (!Sections.TryGetValue(sectionKey, out section))
                {
                    section = new Section(sectionKey, description);
                    Sections.Add(sectionKey, section);
                }
                if (groupKey != null)
                {
                    if (!GroupSections.ContainsKey(groupKey))
                        GroupSections.Add(groupKey, new Dictionary<string, Section>());
                    if (!GroupSections[groupKey].ContainsKey(description))
                        GroupSections[groupKey].Add(description, section);
                }
            }

            private void AddKeyValuePairToLastSectionFromLine(string line)
            {
                var match = PairPattern.Match(line);
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value;
                Sections[Sections.Last().Key].Add(key, value);
            }

            private void EnsureFileBeginsWithSection()
            {
                if (!SectionPattern.IsMatch(manager.Lines[0]))
                    throw new ArgumentException(string.Format("{0} - Beginning line is not a valid section heading", manager.Lines[0]));
            }

            public Dictionary<string, Section> Sections { get; private set; }
            public Dictionary<string, Dictionary<string, Section>> GroupSections { get; private set; }
        }

        class ConfigFileManager
        {
            private static string[] emptyContents = new string[0];
            private static Func<string, string[]> fileReadAllLines = s => { try { return File.ReadAllLines(s); } catch { return emptyContents; } };
            private static Func<string, bool> fileExists = s => { try { return File.Exists(s); } catch { return false; } };
            private static Func<string, string, bool> fileWriteAllText = (file, contents) => { try { File.WriteAllText(file, contents); } catch { return false; } return true; };

            public ConfigFileManager(string filePath)
            {
                FilePath = filePath;
            }

            public void Refresh()
            {
                Lines = fileReadAllLines(FilePath);
            }

            public bool Save(string contents)
            {
                return fileWriteAllText(FilePath, contents);
            }

            public string FilePath { get; private set; }
            public string[] Lines { get; private set; }
        }
    }
}