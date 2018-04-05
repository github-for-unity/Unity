var commander = require("commander");
var package = require('../../package.json')
var ApiWrapper = require('../api')
var endOfLine = require('os').EOL;
var output = require('../output');

commander
    .version(package.version)
    .option('-r, --repository <repository>')
    .option('-d, --description <description>')
    .option('-o, --organization <organization>')
    .option('-p, --private')
    .parse(process.argv);

if(!commander.repository)
{
    commander.help();
    process.exit(-1);
}

var private = false;
if (commander.private) {
    private = true;
}
    
try {
    var apiWrapper = new ApiWrapper();

    apiWrapper.publish(commander.repository, commander.description, private, commander.organization,
        function (error, result) {
            if (error) {
                output.error(error);
            }
            else {
                output.success(result);
            }
        });
}
catch (error) {
    output.error(error);
}