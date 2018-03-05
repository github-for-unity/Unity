var config = require("./configuration");
var octokitWrapper = require("./octokit");

var scopes = ["user", "repo", "gist", "write:public_key"];

var stdIn = process.openStdin();

var awaiter = null;

stdIn.addListener("data", function(d){
    var content = d.toString().trim();
    
    if(awaiter)
    {
        var _awaiter = awaiter;
        awaiter = null;
        _awaiter(content);
    }
});

var handleBasicAuthentication = function (onSuccess, onRequiresTwoFa, onFailure) {

    var username = null;
    var password = null;

    var withPassword = function(input) {
        password = input;
    }

    var promptPassword = function(){
        awaiter = withPassword;
    }

    var withUser = function(input) {
        username = input;
        promptPassword();
    }

    var promptUser = function() {
        awaiter = withUser;
    }

    promptUser();


    // var user = readlineSync.question('User: ');

    // var pwd = readlineSync.question('Password: ', {
    //     hideEchoBack: true
    // });

    // var octokit = octokitWrapper.createOctokit();

    // octokit.authenticate({
    //     type: "basic",
    //     username: user,
    //     password: pwd
    // });

    // octokit.authorization.create({
    //     scopes: scopes,
    //     note: config.appName,
    //     client_id: config.clientId,
    //     client_secret: config.clientSecret
    // }, function (err, res) {
    //     if (err) {
    //         if (err.message === '{"message":"Must specify two-factor authentication OTP code.","documentation_url":"https://developer.github.com/v3/auth#working-with-two-factor-authentication"}') {
    //             onRequiresTwoFa();
    //             return;
    //         }
    //         else {
    //             onFailure(err)
    //         }
    //     }
    //     else {
    //         onSuccess(res.data.token);
    //     }
    // });
}

var handleTwoFactorAuthentication = function (onSuccess, onFailure) {
    var user = readlineSync.question('User: ');

    var pwd = readlineSync.question('Password: ', {
        hideEchoBack: true
    });

    var twofa = readlineSync.question('TwoFactor: ');

    var octokit = octokitWrapper.createOctokit();

    octokit.authenticate({
        type: "basic",
        username: user,
        password: pwd
    });

    octokit.authorization.create({
        scopes: scopes,
        note: config.appName,
        client_id: config.clientId,
        client_secret: config.clientSecret,
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

module.exports = {
    handleBasicAuthentication: handleBasicAuthentication,
    handleTwoFactorAuthentication: handleTwoFactorAuthentication,
};