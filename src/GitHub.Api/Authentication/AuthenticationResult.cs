namespace GitHub.Unity
{
    enum AuthenticationResult
    {
        /// <summary>
        /// Could not authenticate using the credentials provided.
        /// </summary>
        CredentialFailure,
        /// <summary>
        /// The two factor authentication challenge failed.
        /// </summary>
        VerificationFailure,
        /// <summary>
        /// The given remote Uri is not an enterprise Uri.
        /// </summary>
        EnterpriseServerNotFound,
        /// <summary>
        /// Aaaawwww yeeeaah
        /// </summary>
        Success
    }
}
