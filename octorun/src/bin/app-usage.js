var commander = require("commander");
var package = require('../../package.json')
var endOfLine = require('os').EOL;
var fs = require('fs');
var util = require('util');

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
                process.stdout.write("Success");
                process.stdout.write(endOfLine);
                process.stdout.write(d);
                process.stdout.write(endOfLine);
            }
            else {
                process.stdout.write("Error");
                process.stdout.write(endOfLine);
                process.stdout.write(d);
                process.stdout.write(endOfLine);
            }
        });

        res.on('end', function (d) {
            process.exit(success ? 0 : -1);
        });
    });

    req.on('error', function (error) {
        process.stdout.write("Error");
        process.stdout.write(endOfLine);
        
        if (error) {
            process.stdout.write(error.toString());
            process.stdout.write(endOfLine);
        }

        process.exit(-1);
    });

    req.write(fileContents);
    req.end();
}
else {
    commander.help();
    process.exit(-1);
}