require("dotenv").config();

var clientId = process.env.OCTOKIT_CLIENT_ID;
var clientSecret = process.env.OCTOKIT_CLIENT_SECRET;
var appName = process.env.OCTORUN_APP_NAME | "octorun";
var user = process.env.OCTORUN_USER;
var token = process.env.OCTORUN_TOKEN;

module.exports = {
    clientId: clientId,
    clientSecret: clientSecret,
    appName: appName,
    user: user,
    token: token,
};