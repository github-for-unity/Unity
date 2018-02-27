"use strict";
exports.__esModule = true;
var commander = require("commander");
var writer_1 = require("../writer");
var Write = (function () {
    function Write() {
        this.program = commander;
        this.package = require('../../package.json');
        this.writer = new writer_1.Writer();
    }
    Write.prototype.initialize = function () {
        this.program
            .version(this.package.version)
            .option('-m, --message [value]', 'Say hello!')
            .parse(process.argv);
        if (this.program.message != null) {
            if (typeof this.program.message !== 'string') {
                this.writer.write();
            }
            else {
                this.writer.write(this.program.message);
            }
            process.exit();
        }
        this.program.help();
    };
    return Write;
}());
exports.Write = Write;
var app = new Write();
app.initialize();
