var commander = require("commander");
var package = require('../../package.json');
var ApiWrapper = require('../api');
var output = require('../output');

commander
    .version(package.version)
    .option('-h, --host <host>')
    .parse(process.argv);

try {
    var apiWrapper = new ApiWrapper(commander.host);

    apiWrapper.verifyUser(function (error, result) {
        if (error) {
            output.error(error)
            process.exit();
        }
        else {
            output.success([result.name, result.login])
            process.exit();
        }
    });
}
catch (error) {
    output.error(error)
    process.exit();
}