"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const commander = require("commander");
const authenticator_1 = require("../authenticator");
class Write {
    constructor() {
        this.program = commander;
        this.package = require('../../package.json');
        this.authenticator = new authenticator_1.Authenticator();
    }
    initialize() {
        this.program
            .version(this.package.version)
            .parse(process.argv);
        if (this.program.message != null) {
            process.exit();
        }
        this.program.help();
    }
}
exports.Write = Write;
let app = new Write();
app.initialize();
