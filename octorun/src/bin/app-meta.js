var commander = require('commander');
var package = require('../../package.json');
var output = require('../output');

commander
    .version(package.version)
    .option('-h, --host <host>')
    .parse(process.argv);

var host = commander.host;
var port = 443;
var scheme = 'https';

if (host) {
    var https = require(scheme);
    var options = {
        protocol: scheme + ':',
        hostname: host,
        port: port,
        path: '/api/v3/meta',
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    };

    var req = https.request(options, function (res) {
        var success = res.statusCode == 200;

        if(!success) {
            output.error(res.statusCode);
        } else {
            res.on('data', function (d) {
                output.custom("success", d, true);    
            });
    
            res.on('end', function (d) {
                process.exit();
            });
        }
    });

    req.on('error', function (error) {
        output.error(error);
    });

    req.end();
}
else {
    commander.help();
    process.exit(-1);
}