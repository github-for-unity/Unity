"use strict";
exports.__esModule = true;
var commander = require("commander");
var authenticator_1 = require("../authenticator");
var Write = (function () {
    function Write() {
        this.program = commander;
        this.package = require('../../package.json');
        this.authenticator = new authenticator_1.Authenticator();
    }
    Write.prototype.initialize = function () {
        this.program
            .version(this.package.version)
            .option('-l, --login')
            .option('-t, --twoFactor')
            .parse(process.argv);
        if (this.program.login) {
            this.authenticator.createAndDeleteExistingApplicationAuthorization();
            process.exit();
        }
        else if (this.program.twoFactor) {
            process.exit();
        }
        this.program.help();
    };
    return Write;
}());
exports.Write = Write;
var app = new Write();
app.initialize();
