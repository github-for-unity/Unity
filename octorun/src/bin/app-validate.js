var commander = require("commander");
var package = require('../../package.json')
var endOfLine = require('os').EOL;
var ApiWrapper = require('../api')

commander
    .version(package.version)
    .parse(process.argv);

try {
    var apiWrapper = new ApiWrapper();

    apiWrapper.verifyUser(function (error, result) {
        if (error) {
            process.stdout.write("Error");
            process.stdout.write(error);
            process.exit();
        }
        else {
            process.stdout.write("Success");
            process.stdout.write(endOfLine);
            process.stdout.write(result);
            process.exit();
        }
    });
}
catch (error) {
    process.stdout.write("Error");
    process.stdout.write(endOfLine);

    if (error) {
        process.stdout.write(error.toString());
        process.stdout.write(endOfLine);
    }

    process.exit();
}