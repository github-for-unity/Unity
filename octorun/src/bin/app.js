
var commander = require("commander");
var package = require('../../package.json')

commander
    .version(package.version)
    .command('login [-t]', 'Authenticate')
    .parse(process.argv);