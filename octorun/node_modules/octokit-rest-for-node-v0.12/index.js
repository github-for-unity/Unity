// Buffer.from polyfill
if (!Buffer.from) {
  Buffer.from = function (data, encoding, length) {
    return new Buffer(data, encoding, length)
  }
}

module.exports = require('@octokit/rest')
