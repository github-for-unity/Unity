
var commander = require('commander');
var package = require('../../package.json');
var output = require('../output');
var config = require("../configuration");
var querystring = require('querystring');

commander
    .version(package.version)
    .option('-h, --host <host>')
    .parse(process.argv);

var host = commander.host;
var port = 443;
var scheme = 'https';

var valid = host && config.clientId && config.clientSecret && config.token;
if (valid) {
    var https = require(scheme);

    var postData = querystring.stringify({
        client_id: config.clientId,
        client_secret: config.clientSecret,
        code: config.token
      });

    var options = {
        protocol: scheme + ':',
        hostname: host,
        port: port,
        path: '/login/oauth/access_token',
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Content-Length': postData.length
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

    req.write(postData);

    req.end();
}
else {
    commander.help();
    process.exit(-1);
}