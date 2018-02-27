using System;
using System.Linq;
using Octokit;

namespace OctoRun
{
    static class ApiExceptionExtensions
    {
        const string GithubHeader = "X-GitHub-Request-Id";
        public static bool IsGitHubApiException(this Exception ex)
        {
            var apiex = ex as ApiException;
            return apiex?.HttpResponse?.Headers.ContainsKey(GithubHeader) ?? false;
        }

        public static string FirstErrorMessageSafe(this ApiError apiError)
        {
            if (apiError == null) return null;
            if (apiError.Errors == null) return apiError.Message;
            var firstError = apiError.Errors.FirstOrDefault();
            return firstError == null ? null : firstError.Message;
        }

    }
}