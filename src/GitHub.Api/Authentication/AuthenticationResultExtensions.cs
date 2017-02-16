namespace GitHub.Unity
{

    static class AuthenticationResultExtensions
    {
        public static bool IsFailure(this AuthenticationResult result)
        {
            return result != AuthenticationResult.Success;
        }

        public static bool IsSuccess(this AuthenticationResult result)
        {
            return result == AuthenticationResult.Success;
        }
    }
}
