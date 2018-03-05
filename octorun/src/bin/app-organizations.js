var commander = require("commander");
var package = require('../../package.json')
var ApiWrapper = require('../api')

commander
    .version(package.version)
    .parse(process.argv);

var apiWrapper = new ApiWrapper();
apiWrapper.getOrgs(function (error, result) {
    if (error) {
        process.stdout.write(error);
        process.exit(-1);
    }
    else {
        process.stdout.write(result);
        process.exit();
    }
});