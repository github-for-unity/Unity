// polyfill Buffer.from
if (!Buffer.from) {
    Buffer.from = function (data, encoding, length) {
        return new Buffer(data, encoding, length)
    }
}

require("dotenv").config();
require('es6-promise').polyfill();
var readlineSync = require("readline-sync");
var http = require("http");

console.log("NodeJS Path: ", process.argv[0]);

var clientId = process.env.OCTOKIT_CLIENT_ID;
var clientSecret = process.env.OCTOKIT_CLIENT_SECRET;
var appName = process.env.OCTORUN_APP_NAME | "octorun";
var user = process.env.OCTORUN_USER;
var token = process.env.OCTORUN_TOKEN;

var scopes = ["user", "repo", "gist", "write:public_key"];

var Octokit = require('octokit-rest-nothing-to-see-here-kthxbye');
var createOctokit = function () {
    return Octokit({
        timeout: 0,
        requestMedia: 'application/vnd.github.v3+json',
        headers: {
            'user-agent': 'octokit/rest.js v1.2.3'
        }

        // change for custom GitHub Enterprise URL
        //host: 'api.github.com',
        //pathPrefix: '',
        //protocol: 'https',
        //port: 443
    });
};

var handleBasicAuthentication = function (onSuccess, onRequiresTwoFa, onFailure) {
    var user = readlineSync.question('User: ');

    var pwd = readlineSync.question('Password: ', {
        hideEchoBack: true
    });

    var octokit = createOctokit();

    octokit.authenticate({
        type: "basic",
        username: user,
        password: pwd
    });

    octokit.authorization.create({
        scopes: scopes,
        note: appName,
        client_id: clientId,
        client_secret: clientSecret
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

var handleTwoFactorAuthentication = function (onSuccess, onFailure) {
    var user = readlineSync.question('User: ');

    var pwd = readlineSync.question('Password: ', {
        hideEchoBack: true
    });

    var twofa = readlineSync.question('TwoFactor: ');

    var octokit = createOctokit();

    octokit.authenticate({
        type: "basic",
        username: user,
        password: pwd
    });

    octokit.authorization.create({
        scopes: scopes,
        note: appName,
        client_id: clientId,
        client_secret: clientSecret,
        headers: {
            "X-GitHub-OTP": twofa
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

if (user != null && token != null) {

}
else {
    handleTwoFactorAuthentication(function (token) {
        console.log("token", token);
    }, function (err) {
        console.log("error", error);
    })
}
