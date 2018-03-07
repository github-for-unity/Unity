var commander = require("commander");
var package = require('../../package.json')
var ApiWrapper = require('../api')
var endOfLine = require('os').EOL;

commander
    .version(package.version)
    .option('-r, --repository <repository>')
    .option('-d, --description <description>')
    .option('-o, --organization <organization>')
    .option('-p, --private')
    .parse(process.argv);

if(!commander.repository)
{
    process.stdout.write("repository required");
    process.stdout.write(endOfLine);
    commander.help();
    process.exit(-1);
    return;
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
                process.stdout.write("error");
                process.stdout.write(endOfLine);

                process.stdout.write("");
                process.stdout.write(endOfLine);

                process.stdout.write("");
                process.stdout.write(endOfLine);

                if (error) {
                    process.stdout.write(error.toString());
                    process.stdout.write(endOfLine);
                }

                process.exit();
            }
            else {
                process.stdout.write("success");
                process.stdout.write(endOfLine);

                process.stdout.write(commander.repository);
                process.stdout.write(endOfLine);

                process.stdout.write(result);
                process.stdout.write(endOfLine);
                process.exit();
            }
        });
}
catch (error) {
    process.stdout.write("error");
    process.stdout.write(endOfLine);

    process.stdout.write("");
    process.stdout.write(endOfLine);

    if (error) {
        process.stdout.write(error.toString());
        process.stdout.write(endOfLine);
    }

    process.exit();
}