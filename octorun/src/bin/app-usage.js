var commander = require("commander");
var package = require('../../package.json')
var endOfLine = require('os').EOL;
var fs = require('fs');
var util = require('util');
var https = require('https');

commander
    .version(package.version)
    .option('-h, --host <host>')
    .option('-p, --port <port>')
    .parse(process.argv);

var host = commander.host;
var port = 443;

if (commander.port) {
    port = commander.port;
}

var fileContents = null;
if (commander.args.length == 1) {
    var filePath = commander.args[0];

    if (fs.existsSync(filePath)) {
        fileContents = fs.readFileSync(filePath, 'utf8');
    }
}

if (fileContents && host && util.isNumber(port)) {
    var options = {
        protocol: "https:",
        hostname: host,
        port: port,
        path: '/api/usage/unity',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    };

    var req = https.request(options, function (res) {
        res.on('data', function (d) {
            process.stdout.write(d);
            process.stdout.write(endOfLine);
        });

        res.on('end', function (d) {
            process.exit(res.statusCode == 200 ? 0 : -1);
        });
    });

    req.on('error', function (e) {
        console.log(e);
        process.exit(-1);
    });

    req.write(fileContents);
    req.end();
}
else {
    commander.help();
    process.exit(-1);
}