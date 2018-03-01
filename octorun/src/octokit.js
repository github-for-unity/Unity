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

module.exports = { createOctokit: createOctokit };