namespace GitHub.Unity
{
    interface IApiClientFactory
    {
        IApiClient Create(UriString repositoryUrl);
        void ClearFromCache(IApiClient client);
    }
}
