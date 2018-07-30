var commander = require("commander");
var package = require('../../package.json');
var ApiWrapper = require('../api');
var output = require('../output');

commander
    .version(package.version)
    .parse(process.argv);

try {
    var apiWrapper = new ApiWrapper();

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