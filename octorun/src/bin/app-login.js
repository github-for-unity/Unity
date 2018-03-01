var commander = require("commander");
var package = require('../../package.json')
var authentication = require('../authentication')

commander
    .version(package.version)
    .option('-t, --twoFactor')
    .parse(process.argv);

if (commander.twoFactor) {
    authentication.handleTwoFactorAuthentication(function (token) {
        console.log(token);
        process.exit();
    }, function () {
        console.log("Must specify two-factor authentication OTP code.");
        process.exit();
    }, function (err) {
        console.log(err);
        process.exit(-1);
    });
}
else {
    authentication.handleBasicAuthentication(function (token) {
        console.log(token);
        process.exit();
    }, function (err) {
        console.log(err);
        process.exit(-1);
    });
}