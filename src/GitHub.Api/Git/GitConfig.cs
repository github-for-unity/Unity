using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    [Serializable]
    public struct ConfigRemote
    {
        public static ConfigRemote Default = new ConfigRemote(String.Empty, String.Empty);

        public string name;
        public string url;

        public ConfigRemote(string name, string url)
        {
            this.name = name;
            this.url = url;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + (url?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is ConfigRemote)
                return Equals((ConfigRemote)other);
            return false;
        }

        public bool Equals(ConfigRemote other)
        {
            return
                String.Equals(name, other.name) &&
                String.Equals(url, other.url)
                ;
        }

        public static bool operator ==(ConfigRemote lhs, ConfigRemote rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ConfigRemote lhs, ConfigRemote rhs)
        {
            return !(lhs == rhs);
        }

        public string Name => name;

        public string Url => url;

        public override string ToString()
        {
            return $"{{Remote {Name} {Url}}}";
        }
    }

    [Serializable]
    public struct ConfigBranch
    {
        public static ConfigBranch Default = new ConfigBranch(String.Empty);

        public string name;
        public ConfigRemote remote;

        public ConfigBranch(string name)
        {
            this.name = name;
            remote = ConfigRemote.Default;
        }

        public ConfigBranch(string name, ConfigRemote? remote)
        {
            this.name = name;
            this.remote = remote ?? ConfigRemote.Default;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + remote.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is ConfigBranch)
                return Equals((ConfigBranch)other);
            return false;
        }

        public bool Equals(ConfigBranch other)
        {
            return
                String.Equals(name, other.name) &&
                remote.Equals(other.remote)
                ;
        }

        public static bool operator ==(ConfigBranch lhs, ConfigBranch rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ConfigBranch lhs, ConfigBranch rhs)
        {
            return !(lhs == rhs);
        }

        public bool IsTracking => Remote.HasValue;

        public string Name => name;

        public ConfigRemote? Remote => Equals(remote, ConfigRemote.Default) ? (ConfigRemote?) null : remote;

        public override string ToString()
        {
            return $"{{Branch {Name} {Remote?.ToString() ?? "Untracked"}}}";
        }
    }

    public interface IGitConfig
    {
        void Reset();
        IEnumerable<ConfigBranch> GetBranches();
        IEnumerable<ConfigRemote> GetRemotes();
        ConfigRemote? GetRemote(string remote);
        ConfigBranch? GetBranch(string branch);
        bool TryGet<T>(string section, string key, out T value);
        string GetString(string section, string key);
        float GetFloat(string section, string key);
        int GetInt(string section, string key);
        void Set<T>(string section, string key, T value);
        void SetString(string section, string key, string value);
        void SetFloat(string section, string key, float value);
        void SetInt(string section, string key, int value);
    }

    class GitConfig : IGitConfig
    {
        private readonly ConfigFileManager manager;
        private SectionParser sectionParser;
        private Dictionary<string, Section> sections;
        private Dictionary<string, Dictionary<string, Section>> groups;

        public GitConfig(string filePath)
        {
            manager = new ConfigFileManager(filePath.ToNPath());
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
                .Select(x => new ConfigBranch(x.Key, GetRemote(x.Value.TryGetString("remote"))));
        }

        public IEnumerable<ConfigRemote> GetRemotes()
        {
            return groups
                .Where(x => x.Key == "remote")
                .SelectMany(x => x.Value)
                .Where(x => x.Value.TryGetString("url") != null)
                .Select(x => new ConfigRemote(x.Key, x.Value.TryGetString("url")));
        }

        public ConfigRemote? GetRemote(string remote)
        {
            return groups
                .Where(x => x.Key == "remote")
                .SelectMany(x => x.Value)
                .Where(x => x.Key == remote && x.Value.TryGetString("url") != null)
                .Select(x => new ConfigRemote(x.Key,x.Value.GetString("url")) as ConfigRemote?)
                .FirstOrDefault();
        }

        public ConfigBranch? GetBranch(string branch)
        {
            return groups
                .Where(x => x.Key == "branch")
                .SelectMany(x => x.Value)
                .Where(x => x.Key == branch)
                .Select(x => new ConfigBranch(x.Key,GetRemote(x.Value.TryGetString("remote"))) as ConfigBranch?)
                .FirstOrDefault();
        }

        public bool TryGet<T>(string section, string key, out T value)
        {
            value = default(T);
            Section sect = null;
            var ret = sections.TryGetValue(section, out sect);
            if (ret)
            {
                if (value is float)
                    value = (T)(object)sect.GetFloat(key);
                else if (value is int)
                    value = (T)(object)sect.GetInt(key);
                else
                    value = (T)(object)sect.GetString(key);
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
                int result = 0;
                int.TryParse(value, out result);
                return result;
            }

            public float GetFloat(string key)
            {
                var value = this[key];
                float result = 0F;
                float.TryParse(value, out result);
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
            private static readonly Regex CommentPattern = new Regex(@"^[;#].*", RegexOptions.Compiled);
            private static readonly Regex SectionPattern = new Regex(@"^\[(.*)\]$", RegexOptions.Compiled);
            private static readonly Regex PairPattern = new Regex(@"([\S][^=]+)[\s]*=[\s]*(.*)", RegexOptions.Compiled);
            private static readonly Regex GroupSectionPattern = new Regex(@"(.*?(?=""))", RegexOptions.Compiled);

            private readonly ConfigFileManager manager;

            private Section loadedSection;

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

                foreach (var managerLine in manager.Lines)
                {
                    ParseLine(managerLine);
                }
            }

            private void ParseLine(string line)
            {
                if (SectionPattern.IsMatch(line))
                {
                    LoadOrCreateSectionFromLine(line);
                }
                else if (PairPattern.IsMatch(line))
                {
                    AddKeyValuePairToLoadedSectionFromLine(line);
                }
            }

            private void LoadOrCreateSectionFromLine(string line)
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

                if (!Sections.TryGetValue(sectionKey, out loadedSection))
                {
                    loadedSection = new Section(sectionKey, description);

                    Sections.Add(sectionKey, loadedSection);

                    if (groupKey != null)
                    {
                        Dictionary<string, Section> groupSection;
                        if (!GroupSections.TryGetValue(groupKey, out groupSection))
                        {
                            groupSection = new Dictionary<string, Section>();

                            GroupSections.Add(groupKey, groupSection);
                        }

                        if (!groupSection.ContainsKey(description))
                        {
                            groupSection.Add(description, loadedSection);
                        }
                    }
                }
            }

            private void AddKeyValuePairToLoadedSectionFromLine(string line)
            {
                var match = PairPattern.Match(line);
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value;

                loadedSection.Add(key, value);
            }

            private void EnsureFileBeginsWithSection()
            {
                var first = manager.Lines.SkipWhile(l => String.IsNullOrEmpty(l) || CommentPattern.IsMatch(l)).FirstOrDefault();
                if (first == null || !SectionPattern.IsMatch(first))
                    throw new ArgumentException(string.Format("{0} - Beginning line is not a valid section heading", first));
            }

            public Dictionary<string, Section> Sections { get; private set; }
            public Dictionary<string, Dictionary<string, Section>> GroupSections { get; private set; }
        }

        class ConfigFileManager
        {
            private static readonly string[] emptyContents = new string[0];

            public ConfigFileManager(NPath filePath)
            {
                FilePath = filePath;
            }

            private bool WriteAllText(string contents)
            {
                try
                {
                    FilePath.WriteAllText(contents);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            private string[] ReadAllLines()
            {
                try
                {
                    return FilePath.ReadAllLines();
                }
                catch
                {
                    return emptyContents;
                }
            }

            public void Refresh()
            {
                Lines = ReadAllLines();
            }

            public bool Save(string contents)
            {
                return WriteAllText(contents);
            }

            public NPath FilePath { get; private set; }
            public string[] Lines { get; private set; }
        }
    }
}