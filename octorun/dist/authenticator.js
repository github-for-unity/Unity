"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const GitHub = require("@octokit/rest");
class Authenticator {
    constructor() {
        this.github = new GitHub({
            timeout: 0,
            requestMedia: 'application/vnd.github.v3+json',
            headers: {
                'user-agent': 'octokit/rest.js v1.2.3'
            },
            host: 'api.github.com',
            pathPrefix: '',
            protocol: 'https',
            port: 443,
        });
    }
    authenticate() {
    }
}
exports.Authenticator = Authenticator;
