var commander = require("commander");
var package = require('../../package.json')
var ApiWrapper = require('../api')
var endOfLine = require('os').EOL;

commander
    .version(package.version)
    .parse(process.argv);

try {

    var apiWrapper = new ApiWrapper();
    apiWrapper.getOrgs(function (error, result) {
        if (error) {
            process.stdout.write("error");
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

            for (var i = 0; i < result.length; i++) {
                process.stdout.write(result[i].name);
                process.stdout.write(endOfLine);
                process.stdout.write(result[i].login);
                process.stdout.write(endOfLine);
            }

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