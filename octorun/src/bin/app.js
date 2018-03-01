
var commander = require("commander");
var package = require('../../package.json')

commander
    .version(package.version)
    .command('login [-t]', 'Authenticate')
    .command('validate', 'Validate Current User')
    .command('organizations', 'Get Organizations')
    .parse(process.argv);