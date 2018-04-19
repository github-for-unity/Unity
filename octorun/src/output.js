var endOfLine = require('os').EOL;

var outputResult = function (status, results, errors, preventExit) {
    process.stdout.write(status);
    process.stdout.write(endOfLine);

    if (!results) {
        process.stdout.write("");
        process.stdout.write(endOfLine);
    }
    else {
        if (typeof results === 'string') {
            process.stdout.write(results);
            process.stdout.write(endOfLine);
        }
        else if (Array.isArray(results)) {
            for (var resultIndex = 0; resultIndex < results.length; resultIndex++) {
                var result = results[resultIndex];
                if (typeof result !== 'string') {
                    throw "Unsupported result output";
                }

                process.stdout.write(result);
                process.stdout.write(endOfLine);
            }
        }
        else if (results.toString) {
            process.stdout.write(results.toString());
            process.stdout.write(endOfLine);
        }
        else {
            throw "Unsupported result output";
        }
    }

    if (errors) {
        if (typeof errors === 'string') {
            process.stdout.write(errors);
            process.stdout.write(endOfLine);
        }
        else if (Array.isArray(errors)) {
            for (var errorIndex = 0; errorIndex < errors.length; errorIndex++) {
                var error = errors[errorIndex];
                if (typeof error !== 'string') {
                    throw "Unsupported error output";
                }

                process.stdout.write(error);
                process.stdout.write(endOfLine);
            }
        }
        else if (errors.toString) {
            process.stdout.write(errors.toString());
            process.stdout.write(endOfLine);
        }
        else {
            process.stdout.write(errors);
            process.stdout.write(endOfLine);
        }
    }

    if (!preventExit) {
        process.exit();
    }
}

var outputSuccess = function (results) {
    outputResult("success", results);
}

var outputCustom = function (status, results, preventExit) {
    outputResult(status, results, undefined, preventExit);
}

var outputError = function (errors) {
    outputResult("error", null, errors);
}

module.exports = {
    success: outputSuccess,
    custom: outputCustom,
    error: outputError
};