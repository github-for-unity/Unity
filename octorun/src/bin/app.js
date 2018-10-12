
var commander = require("commander");
var package = require('../../package.json')

commander
    .version(package.version)
    .command('login', 'Authenticate')
    .command('validate', 'Validate Current User')
    .command('organizations', 'Get Organizations')
    .command('publish', 'Publish')
    .command('usage', 'Usage')
    .command('meta', 'Get Server Meta Data')
    .parse(process.argv);