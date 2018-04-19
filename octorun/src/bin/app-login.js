var commander = require("commander");
var package = require('../../package.json');
var authentication = require('../authentication');
var endOfLine = require('os').EOL;
var output = require('../output');

commander
    .version(package.version)
    .option('-t, --twoFactor')
    .parse(process.argv);

var handleAuthentication = function (username, password, twoFactor) {
    authentication.handleAuthentication(username, password, function (token, status) {
        if (status) {
            output.custom(status, token);
        }
        else {
            output.success(token);
        }
    }, function (error) {
        output.error(error);
    }, twoFactor);
}

var encoding = 'utf-8';
if (commander.twoFactor) {
    if (process.stdin.isTTY) {
        var readlineSync = require("readline-sync");
        var username = readlineSync.question('User: ');
        var password = readlineSync.question('Password: ', {
            hideEchoBack: true
        });

        var twoFactor = readlineSync.question('Two Factor: ');

        try {
            handleAuthentication(username, password, twoFactor);
        }
        catch (error) {
            output.error(error);
        }
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

            try {
                handleAuthentication(items[0], items[1], items[2]);
            }
            catch (error) {
                output.error(error);
                process.exit();
            }
        });
    }
}
else {
    if (process.stdin.isTTY) {
        var readlineSync = require("readline-sync");

        var username = readlineSync.question('User: ');
        var password = readlineSync.question('Password: ', {
            hideEchoBack: true
        });

        try {
            handleAuthentication(username, password);
        }
        catch (error) {
            output.error(error);
            process.exit();
        }
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

            try {
                handleAuthentication(items[0], items[1]);
            }
            catch (error) {
                output.error(error);
            }
        });
    }
}