var commander = require("commander");
var package = require('../../package.json')
var endOfLine = require('os').EOL;
var fs = require('fs');
var util = require('util');
var output = require('../output');

commander
    .version(package.version)
    .option('-s, --scheme <scheme>')
    .option('-h, --host <host>')
    .option('-p, --port <port>')
    .parse(process.argv);

var host = commander.host;
var port = 443;
var scheme = 'https';

if (commander.port) {
    port = parseInt(commander.port);
}

if (commander.scheme) {
    scheme = commander.scheme;
}

var fileContents = null;
if (commander.args.length == 1) {
    var filePath = commander.args[0];

    if (fs.existsSync(filePath)) {
        fileContents = fs.readFileSync(filePath, 'utf8');
    }
}

if (fileContents && host) {
    var https = require(scheme);
    var options = {
        protocol: scheme + ':',
        hostname: host,
        port: port,
        path: '/api/usage/unity',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    };

    var req = https.request(options, function (res) {
        var success = res.statusCode == 200;

        res.on('data', function (d) {
            if (success) {
                output.custom("success", d, true);
            }
            else {
                output.custom("error", "", true);
            }
        });

        res.on('end', function (d) {
            process.exit();
        });
    });

    req.on('error', function (error) {
        output.error(error);
    });

    req.write(fileContents);
    req.end();
}
else {
    commander.help();
    process.exit(-1);
}