var endOfLine = require('os').EOL;

var outputResult = function (status, results, errors) {
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

        throw "Unsupported result output";
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
                    throw "Unsupported result output";
                }

                process.stdout.write(error);
                process.stdout.write(endOfLine);
            }
        }
        else {
            process.stdout.write(errors);
            process.stdout.write(endOfLine);
        }
    }
}

var outputSuccess = function (results) {
    outputResult("success", results);
}

var outputCustom = function (status, results) {
    outputResult(status, results);
}

var outputError = function (errors) {
    outputResult("error", null, errors);
}

module.exports = {
    success: outputSuccess,
    custom: outputCustom,
    error: outputError
};