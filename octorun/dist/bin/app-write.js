"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const commander = require("commander");
const writer_1 = require("../writer");
class Write {
    constructor() {
        this.program = commander;
        this.package = require('../../package.json');
        this.writer = new writer_1.Writer();
    }
    initialize() {
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
    }
}
exports.Write = Write;
let app = new Write();
app.initialize();
