var commander = require("commander");
var package = require('../../package.json')
var authentication = require('../authentication')

commander
    .version(package.version)
    .option('-t, --twoFactor')
    .parse(process.argv);

if (commander.twoFactor) {
    authentication.handleTwoFactorAuthentication(function (token) {
        process.stdout.write(token);
        process.exit();
    }, function (err) {
        process.stdout.write(err);
        process.exit();
    });
}
else {
    authentication.handleBasicAuthentication(function (token) {
        process.stdout.write(token);
        process.exit();
    }, function () {
        process.stdout.write("Must specify two-factor authentication OTP code.");
        process.exit();
    }, function (err) {
        process.stdout.write(err);
        process.exit();
    });
}