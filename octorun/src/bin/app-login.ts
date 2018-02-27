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
            .parse(process.argv);

        if (this.program.message != null) {

            // if (typeof this.program.message !== 'string') {
            //     this.writer.write();
            // } else {
            //     this.writer.write(this.program.message);
            // }

            process.exit();
        }

        this.program.help();
    }

}

let app = new Write();
app.initialize();
