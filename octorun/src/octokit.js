var Octokit = require('octokit-rest-for-node-v0.12');

var createOctokit = function (appName) {
    return Octokit({
        timeout: 0,
        requestMedia: 'application/vnd.github.v3+json',
        headers: {
            'user-agent': appName
        }
    });
};

module.exports = { createOctokit: createOctokit };