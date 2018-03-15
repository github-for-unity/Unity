require("dotenv").config({silent: true});

var clientId = process.env.OCTOKIT_CLIENT_ID;
var clientSecret = process.env.OCTOKIT_CLIENT_SECRET;
var appName = process.env.OCTOKIT_USER_AGENT;
var user = process.env.OCTORUN_USER;
var token = process.env.OCTORUN_TOKEN;

module.exports = {
    clientId: clientId,
    clientSecret: clientSecret,
    appName: appName,
    user: user,
    token: token
};