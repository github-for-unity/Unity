var endOfLine = require('os').EOL;
var config = require("./configuration");
var octokitWrapper = require("./octokit");

var twoFactorRegex = new RegExp("must specify two-factor authentication otp code", "gi");

var scopes = ["user", "repo", "gist", "write:public_key"];

var handleAuthentication = function (username, password, onSuccess, onFailure, twoFactor) {
    if (!config.clientId || !config.clientSecret) {
        throw "clientId and/or clientSecret missing";
    }

    if (!config.appName) {
        throw "appName missing";
    }

    var octokit = octokitWrapper.createOctokit(config.appName);

    octokit.authenticate({
        type: "basic",
        username: username,
        password: password
    });

    var headers;
    if (twoFactor) {
        headers = {
            "X-GitHub-OTP": twoFactor,
        };
    }

    octokit.authorization.create({
        scopes: scopes,
        note: config.appName,
        client_id: config.clientId,
        client_secret: config.clientSecret,
        headers: headers
    }, function (err, res) {
        if (err) {
            if (twoFactor && err.code && err.code === 422) {
                //Two Factor Enterprise workaround
                onSuccess(password);
            }
            else if (twoFactorRegex.test(err.message)) {
                onSuccess(password, "2fa");
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
    handleAuthentication: handleAuthentication,
};