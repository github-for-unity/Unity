const readlineSync = require("readline-sync");
const octokit = require('@octokit/rest')({
    timeout: 0, // 0 means no request timeout
    requestMedia: 'application/vnd.github.v3+json',
    headers: {
      'user-agent': 'octokit/rest.js v1.2.3' // v1.2.3 will be current version
    },
  
    // change for custom GitHub Enterprise URL
    //host: 'api.github.com',
    //pathPrefix: '',
    //protocol: 'https',
    //port: 443,
  
    // Node only: advanced request options can be passed as http(s) agent
    //agent: undefined
  });

console.log("NodeJS Path: ", process.argv[0]);

require("dotenv").config();

const clientId = process.env.OCTOKIT_CLIENT_ID;
const clientSecret = process.env.OCTOKIT_CLIENT_SECRET;

const appName = process.env.OCTORUN_APP_NAME | "octorun";
let user = process.env.OCTORUN_USER;
const token = process.env.OCTORUN_TOKEN;

const scopes = ["user", "repo", "gist", "write:public_key"];

if(user != null && token != null)
{

}
else
{
    user = readlineSync.question('User: ');
    
    var pwd = readlineSync.question('Password: ', {
        hideEchoBack: true
    });

    octokit.authenticate({
        type:"basic",
        username:user,
        password:pwd
    });

    octokit.authorization.create({
        scopes: scopes,
        note: appName,
        client_id: clientId,
        client_secret: clientSecret
    }, function(err, res) {

        console.log("err", err, "res", res);

        if(err)
        {
            
        }
        else
        {

        }
    });
}
