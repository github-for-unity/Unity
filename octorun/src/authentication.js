var endOfLine = require('os').EOL;
var config = require("./configuration");
var octokitWrapper = require("./octokit");

var scopes = ["user", "repo", "gist", "write:public_key"];

var lockedRegex = new RegExp("number of login attempts exceeded", "gi");
var twoFactorRegex = new RegExp("must specify two-factor authentication OTP code", "gi");

var handleBasicAuthentication = function (username, password, onSuccess, onRequiresTwoFa, onLocked, onFailure) {
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
            else {
                onFailure(err)
            }
        }
        else {
            onSuccess(res.data.token);
        }
    });
}

var handleTwoFactorAuthentication = function (username, password, twoFactor, onSuccess, onLocked,  onFailure) {
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
        client_secret: config.clientSecret,
        headers: {
            "X-GitHub-OTP": twoFactor
        }
    }, function (err, res) {
        if (err) {
            onFailure(err)
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