using System;

namespace GitHub.Unity
{
    class LocksOutputProcessor : BaseOutputListProcessor<GitLock>
    {
        public override void LineReceived(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                //Do Nothing
                return;
            }

            try
            {
                var locks = line.FromJson<GitLock[]>(lowerCase: true);
                foreach (var lck in locks)
                {
                    RaiseOnEntry(lck);
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex, $"Failed to parse lock line {line}");
            }
        }
    }
}
