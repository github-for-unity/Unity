"use strict";
exports.__esModule = true;
var commander = require("commander");
var App = (function () {
    function App() {
        this.program = commander;
        this.package = require('../../package.json');
    }
    App.prototype.initialize = function () {
        this.program
            .version(this.package.version)
            .command('login [-h|-2fa]', 'Authenticate')
            .command('write [message]', 'say hello!')
            .parse(process.argv);
    };
    return App;
}());
exports.App = App;
var app = new App();
app.initialize();
