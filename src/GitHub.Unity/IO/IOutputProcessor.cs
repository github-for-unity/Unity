namespace GitHub.Unity
{
    interface IOutputProcessor
    {
        void LineReceived(string line);
    }
}