namespace GitHub.Unity
{
    interface IApplicationCache
    {
        bool FirstRun { get; }
        string CreatedDate { get; }
        bool UsageIncremented { get; set;  }
    }
}