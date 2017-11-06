namespace GitHub.Unity
{
    static class OctokitExtensions
    {
        public static GitHubUser ToGitHubUser(this Octokit.User user)
        {
            return new GitHubUser() {
                Name = user.Name,
                Login = user.Login,
            };
        }

        public static GitHubRepository ToGitHubRepository(this Octokit.Repository repository)
        {
            return new GitHubRepository {
                Name = repository.Name,
                CloneUrl = repository.CloneUrl
            };
        }
    }
}