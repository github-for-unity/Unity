var commander = require("commander");
var package = require('../../package.json')
var endOfLine = require('os').EOL;

commander
    .version(package.version)
    .parse(process.argv);

var processData = function (postData) {
    var https = require('https');

    var options = {
        hostname: 'central.github.com',
        path: '/api/usage/unity',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    };

    var req = https.request(options, function (res) {
        process.stdout.write('statusCode:', res.statusCode);

        res.on('data', function (d) {
            process.stdout.write(d);
            process.stdout.write(endOfLine);
        });

        res.on('end', function (d) {
            process.exit();
        });
    });

    req.on('error', function (e) {
        console.error(e);
        process.exit(-1);
    });

    req.write(postData);
    req.end();
}

if (process.stdin.isTTY) {
    var readlineSync = require("readline-sync");
    var postData = readlineSync.question();

    processData(postData);
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

        processData(items[0]);
    });
}