require("dotenv").config();

console.log("NodeJS Path: ", process.argv[0]);

console.log(process.env.OCTOKIT_CLIENT_ID);
console.log(process.env.OCTOKIT_CLIENT_SECRET);

var GitHub = require('@octokit/rest');
var gitHub = new GitHub();

var authParams = {
    client_id: process.env.OCTOKIT_CLIENT_ID,
    client_secret: process.env.OCTOKIT_CLIENT_SECRET,
    scopes: ["user", "repo", "gist", "write:public_key"]
};

gitHub.authorization.getOrCreateAuthorizationForApp(authParams, function (error, result) {
    if (error) {
        console.log("error", error, error.stack);
    }
    else {
        console.log("result", result);
    }

    process.exit();
});