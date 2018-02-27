"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const commander = require("commander");
class App {
    constructor() {
        this.program = commander;
        this.package = require('../../package.json');
    }
    initialize() {
        this.program
            .version(this.package.version)
            .command('write [message]', 'say hello!')
            .parse(process.argv);
    }
}
exports.App = App;
let app = new App();
app.initialize();
