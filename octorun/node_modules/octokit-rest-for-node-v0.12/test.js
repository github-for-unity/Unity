var assert = require('assert')
var octokit = require('./build')()

if (!process.env.GH_TOKEN) {
  throw new Error('must set GH_TOKEN')
}

octokit.authenticate({
  type: 'token',
  token: process.env.GH_TOKEN
})

console.log('octokit.repos.get()')
octokit.repos.get({
  owner: 'octokit',
  repo: 'rest.js'
})

.then(function () {
  console.log('ok')

  console.log('octokit.authorization.getOrCreateAuthorizationForApp()')
  return octokit.authorization.getOrCreateAuthorizationForApp({
    client_secret: 'abcdabcdabcdabcdabcdabcdabcdabcdabcdabcd'
  })

  .catch(function (error) {
    assert(error.code, 403)
  })
})

.catch(function (error) {
  console.log(error.stack)
  process.exit(1)
})
