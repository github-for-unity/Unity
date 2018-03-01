// polyfill Buffer.from
if (!Buffer.from) {
    Buffer.from = function (data, encoding, length) {
      return new Buffer(data, encoding, length)
    }
}

require("dotenv").config();
require('es6-promise').polyfill();

console.log("NodeJS Path: ", process.argv[0]);

console.log(process.env.OCTOKIT_CLIENT_ID);
console.log(process.env.OCTOKIT_CLIENT_SECRET);

var GitHub = require('octokit-rest-nothing-to-see-here-kthxbye');
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