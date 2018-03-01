var commander = require("commander");
var package = require('../../package.json')
var readlineSync = require("readline-sync");
var endOfLine = require('os').EOL;

commander
    .version(package.version)
    .parse(process.argv);

var postData = readlineSync.question();

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
    console.log('statusCode:', res.statusCode);

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