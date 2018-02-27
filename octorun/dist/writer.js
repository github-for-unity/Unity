"use strict";
exports.__esModule = true;
var Writer = (function () {
    function Writer() {
    }
    Writer.prototype.write = function (message) {
        if (message === void 0) { message = "Hello World!"; }
        console.log(message);
    };
    return Writer;
}());
exports.Writer = Writer;
