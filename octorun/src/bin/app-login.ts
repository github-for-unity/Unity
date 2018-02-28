import * as commander from 'commander';
import { Authenticator } from '../authenticator';

export class Write {

    private program: commander.CommanderStatic;
    private package: any;
    private authenticator: Authenticator;

    constructor() {
        this.program = commander;
        this.package = require('../../package.json');
        this.authenticator = new Authenticator();
    }

    public initialize() {
        this.program
            .version(this.package.version)
            .option('-l, --login')
            .option('-t, --twoFactor')
            .parse(process.argv);

        if (this.program.login) {

            this.authenticator.createAndDeleteExistingApplicationAuthorization()

            process.exit();
        }
        else if (this.program.twoFactor) {

            process.exit();
        }

        this.program.help();
    }

}

let app = new Write();
app.initialize();
