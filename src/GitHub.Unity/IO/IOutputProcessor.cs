namespace GitHub.Unity
{
    public interface IOutputProcessor
    {
        void LineReceived(string line);
    }
}