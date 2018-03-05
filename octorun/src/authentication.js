var config = require("./configuration");
var octokitWrapper = require("./octokit");

var scopes = ["user", "repo", "gist", "write:public_key"];

var stdIn = process.openStdin();

var awaiter = null;

stdIn.addListener("data", function (d) {
    var content = d.toString().trim();

    if (awaiter) {
        var _awaiter = awaiter;
        awaiter = null;
        _awaiter(content);
    }
});

var handleBasicAuthentication = function (onSuccess, onRequiresTwoFa, onFailure) {
    var username = null;
    var password = null;

    var withPassword = function (input) {
        password = input;

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
                if (err.message === '{"message":"Must specify two-factor authentication OTP code.","documentation_url":"https://developer.github.com/v3/auth#working-with-two-factor-authentication"}') {
                    onRequiresTwoFa();
                    return;
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

    var promptPassword = function () {
        process.stdout.write("Password: ");
        awaiter = withPassword;
    }

    var withUser = function (input) {
        username = input;
        promptPassword();
    }

    var promptUser = function () {
        process.stdout.write("Username: ");
        awaiter = withUser;
    }

    promptUser();
}

var handleTwoFactorAuthentication = function (onSuccess, onFailure) {
    var username = null;
    var password = null;
    var twoFactor = null;

    var withTwoFactor = function (input) {
        twoFactor = input;

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

    var promptTwoFactor = function () {
        process.stdout.write("Two Factor: ");
        awaiter = withTwoFactor;
    }

    var withPassword = function (input) {
        password = input;
        promptTwoFactor();
    }

    var promptPassword = function () {
        process.stdout.write("Password: ");
        awaiter = withPassword;
    }

    var withUser = function (input) {
        username = input;
        promptPassword();
    }

    var promptUser = function () {
        process.stdout.write("Username: ");
        awaiter = withUser;
    }

    promptUser();
}

module.exports = {
    handleBasicAuthentication: handleBasicAuthentication,
    handleTwoFactorAuthentication: handleTwoFactorAuthentication,
};