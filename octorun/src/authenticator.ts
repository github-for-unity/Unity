//const octokit = require('@octokit/rest')

import * as GitHub from 'github';
import { configuration } from './configuration';

export class Authenticator {

    private github: GitHub;

    constructor() {

        //Listed defaults from https://github.com/octokit/rest.js#options

        this.github = new GitHub({
            timeout: 0, // 0 means no request timeout
            //requestMedia: 'application/vnd.github.v3+json',
            headers: {
                'user-agent': 'octokit/rest.js v1.2.3' // v1.2.3 will be current version
            },

            // change for custom GitHub Enterprise URL
            host: 'api.github.com',
            pathPrefix: '',
            protocol: 'https',
            //port: 443,

            // Node only: advanced request options can be passed as http(s) agent
            //agent: undefined
        })
    }

    public async createAndDeleteExistingApplicationAuthorization() {
        const authParams: GitHub.AuthorizationGetOrCreateAuthorizationForAppParams = {
            client_id: configuration.ClientId,
            client_secret: configuration.ClientSecret,
            scopes: ["user", "repo", "gist", "write:public_key"]
        };

        await this.github.authorization.getOrCreateAuthorizationForApp(authParams);
    }
}
