# Authenticating to GitHub

## How to sign in to GitHub

1. Open the **GitHub** window by going to the top level **Window** menu and selecting **GitHub**, as shown below.

    <img src="images/github-menu-item.png" alt="GitHub menu item in the Window menu" width="500px"/>

1. Click the **Sign in** button at the top right of the window.

    <img src="images/github-sign-in-button.png" alt="GitHub menu item in the Window menu" width="500px"/>

1. In the **Authenticate** dialog, enter your username or email and password

   <img src="images/github-authenticate.png" alt="GitHub menu item in the Window menu" width="350px"/>

      If your account requires Two Factor Authentication, you will be prompted for your auth code.

      <img src="images/github-two-factor.png" alt="GitHub menu item in the Window menu" width="350px"/>

You will need to create a GitHub account before you can sign in, if you don't have one already.

- For more information on creating a GitHub account, see "[Signing up for a new GitHub account](https://help.github.com/articles/signing-up-for-a-new-github-account/)".

### Personal access tokens

If the sign in operation above fails, you can manually create a personal access token and use it as your password.

The scopes for the personal access token are: `user`, `repo`.
- *user* scope: Grants access to the user profile data. We currently use this to display your avatar and check whether your plans lets you publish private repositories.
- *repo* scope: Grants read/write access to code, commit statuses, invitations, collaborators, adding team memberships, and deployment statuses for public and private repositories and organizations. This is needed for all git network operations (push, pull, fetch), and for getting information about the repository you're currently working on.

For more information on creating personal access tokens, see "[Creating a personal access token for the command line](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line).

For more information on authenticating with SAML single sign-on, see "[About authentication with SAML single sign-on](https://help.github.com/articles/about-authentication-with-saml-single-sign-on)."
