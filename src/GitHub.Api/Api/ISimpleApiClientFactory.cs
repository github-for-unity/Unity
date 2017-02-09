namespace GitHub.Api
{
    public interface ISimpleApiClientFactory
    {
        ISimpleApiClient Create(UriString repositoryUrl);
        void ClearFromCache(ISimpleApiClient client);
    }
}
