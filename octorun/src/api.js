var readlineSync = require("readline-sync");
var config = require("./configuration");
var octokitWrapper = require("./octokit");

function ApiWrapper() {
    this.octokit = octokitWrapper.createOctokit();

    if (!config.user || !config.token) {
        throw "User and/or Token missing";
    }

    this.octokit.authenticate({
        type: "oauth",
        token: config.token
    });
}

ApiWrapper.prototype.verifyUser = function (callback) {
    this.octokit.users.get({}, function(error, result){
        callback(error, (!result) ? null : result.data.login);
    });
};

ApiWrapper.prototype.getOrgs = function (callback) {
    var position = { page: 0, per_page: 100 };
    this.octokit.users.getOrgs(position, function (error, result) {
        callback(error, (!result) ? null : result.data.map(function (item) { return item.login; }));
    });
};

module.exports = ApiWrapper;