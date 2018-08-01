var commander = require("commander");
var package = require('../../package.json');
var ApiWrapper = require('../api');
var output = require('../output');

commander
    .version(package.version)
    .parse(process.argv);

try {

    var apiWrapper = new ApiWrapper();
    apiWrapper.getOrgs(function (error, result) {
        if (error) {
            output.error(error);
        }
        else {
            var results = [];
            for (var i = 0; i < result.length; i++) {
                results.push(result[i].name);
                results.push(result[i].login);
            }

            output.success(results);
        }
    });
}
catch (error) {
    output.error(error);
}