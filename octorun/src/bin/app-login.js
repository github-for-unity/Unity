var commander = require("commander");
var package = require('../../package.json');
var authentication = require('../authentication');
var endOfLine = require('os').EOL;
var output = require('../output');

commander
    .version(package.version)
    .option('-t, --twoFactor')
    .parse(process.argv);

var encoding = 'utf-8';

if (commander.twoFactor) {
    var handleTwoFactorAuthentication = function (username, password, token) {
        authentication.handleTwoFactorAuthentication(username, password, token, function (token) {
            output.success(token);
            process.exit();
        }, function (error) {
            output.error(error);
            process.exit();
        });
    }

    if (process.stdin.isTTY) {
        var readlineSync = require("readline-sync");
        var username = readlineSync.question('User: ');
        var password = readlineSync.question('Password: ', {
            hideEchoBack: true
        });

        var twoFactor = readlineSync.question('Two Factor: ');

        handleTwoFactorAuthentication(username, password, twoFactor);
    }
    else {
        var data = '';
        process.stdin.setEncoding(encoding);

        process.stdin.on('readable', function () {
            var chunk;
            while (chunk = process.stdin.read()) {
                data += chunk;
            }
        });

        process.stdin.on('end', function () {
            var items = data.toString()
                .split(/\r?\n/)
                .filter(function (item) { return item; });

            handleTwoFactorAuthentication(items[0], items[1], items[2]);
        });
    }
}
else {
    var handleBasicAuthentication = function (username, password) {
        authentication.handleBasicAuthentication(username, password,
            function (token) {
                output.success(token);
                process.exit();
            }, function () {
                output.custom("2fa", password);
                process.exit();
            }, function (error) {
                output.error(error);
                process.exit();
            });
    }

    if (process.stdin.isTTY) {
        var readlineSync = require("readline-sync");

        var username = readlineSync.question('User: ');
        var password = readlineSync.question('Password: ', {
            hideEchoBack: true
        });

        handleBasicAuthentication(username, password);
    }
    else {
        var data = '';
        process.stdin.setEncoding(encoding);

        process.stdin.on('readable', function () {
            var chunk;
            while (chunk = process.stdin.read()) {
                data += chunk;
            }
        });

        process.stdin.on('end', function () {
            var items = data.toString()
                .split(/\r?\n/)
                .filter(function (item) { return item; });

            handleBasicAuthentication(items[0], items[1]);
        });
    }
}