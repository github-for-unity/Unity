var commander = require("commander");
var package = require('../../package.json')
var ApiWrapper = require('../api')

commander
    .version(package.version)
    .option('-r, --repository <value>')
    .option('-d, --description <value>')
    .option('-o, --organization <value>')
    .option('-p, --private')
    .parse(process.argv);

if(!commander.repository)
{
    process.stdout.write("repository required");
    commander.help();
    process.exit(-1);
    return;
}

var private = false;
if (commander.private) {
    private = true;
}

var apiWrapper = new ApiWrapper();

apiWrapper.publish(commander.repository, commander.description, private, commander.organization,
    function (error, result) {
        if (error) {
            process.stdout.write(error);
            process.exit(-1);
        }
        else {
            process.stdout.write(result);
            process.exit();
        }
    });