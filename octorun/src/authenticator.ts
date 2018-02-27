//const octokit = require('@octokit/rest')

import * as GitHub from '@octokit/rest';

export class Authenticator {

    private github: GitHub;

    constructor(){

        //Listed defaults from https://github.com/octokit/rest.js#options

        this.github = new GitHub({
            timeout: 0, // 0 means no request timeout
            requestMedia: 'application/vnd.github.v3+json',
            headers: {
              'user-agent': 'octokit/rest.js v1.2.3' // v1.2.3 will be current version
            },
          
            // change for custom GitHub Enterprise URL
            host: 'api.github.com',
            pathPrefix: '',
            protocol: 'https',
            port: 443,
          
            // Node only: advanced request options can be passed as http(s) agent
            //agent: undefined
          })
    }

    public authenticate() {

    }
}
