namespace GitHub.Api
{
    interface ISharpZipLibHelper
    {
        void ExtractZipFile(string archive, string outFolder);
    }
}