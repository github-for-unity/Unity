var endOfLine = require('os').EOL;
var config = require("./configuration");
var octokitWrapper = require("./octokit");

var scopes = ["user", "repo", "gist", "write:public_key"];

var lockedRegex = new RegExp("number of login attempts exceeded", "gi");
var twoFactorRegex = new RegExp("must specify two-factor authentication otp code", "gi");
var badCredentialsRegex = new RegExp("bad credentials", "gi");

var handleBasicAuthentication = function (username, password, onSuccess, onRequiresTwoFa, onFailure) {
    var octokit = octokitWrapper.createOctokit();

    octokit.authenticate({
        type: "basic",
        username: username,
        password: password
    });

    octokit.authorization.create({
        scopes: scopes,
        note: config.appName,
        client_id: config.clientId,
        client_secret: config.clientSecret
    }, function (err, res) {
        if (err) {
            if (twoFactorRegex.test(err.message)) {
                onRequiresTwoFa();
            }
            else if (lockedRegex.test(err.message)) {
                onFailure("Account locked.")
            }
            else if (badCredentialsRegex.test(err.message)) {
                onFailure("Bad credentials.")
            }
            else {
                onFailure(err)
            }
        }
        else {
            onSuccess(res.data.token);
        }
    });
}

var handleTwoFactorAuthentication = function (username, password, twoFactor, onSuccess, onFailure) {
    var octokit = octokitWrapper.createOctokit();

    octokit.authenticate({
        type: "basic",
        username: username,
        password: password
    });

    octokit.authorization.create({
        scopes: scopes,
        client_id: config.clientId,
        client_secret: config.clientSecret,
        headers: {
            "X-GitHub-OTP": twoFactor,
            "user-agent": config.appName
        }
    }, function (err, res) {
        if (err) {
            if (lockedRegex.test(err.message)) {
                onFailure("Account locked.")
            }
            else if (badCredentialsRegex.test(err.message)) {
                onFailure("Bad credentials.")
            }
            else {
                onFailure(err)
            }
        }
        else {
            onSuccess(res.data.token);
        }
    });
}

module.exports = {
    handleBasicAuthentication: handleBasicAuthentication,
    handleTwoFactorAuthentication: handleTwoFactorAuthentication,
};