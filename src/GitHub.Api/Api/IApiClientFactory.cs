namespace GitHub.Api
{
    interface IApiClientFactory
    {
        IApiClient Create(UriString repositoryUrl);
        void ClearFromCache(IApiClient client);
    }
}
