import * as commander from 'commander';
import { Writer } from '../writer';

export class Write {

    private program: commander.CommanderStatic;
    private package: any;
    private writer: Writer;

    constructor() {
        this.program = commander;
        this.package = require('../../package.json');
        this.writer = new Writer();
    }

    public initialize() {
        this.program
            .version(this.package.version)
            .option('-m, --message [value]', 'Say hello!')
            .parse(process.argv);

        if (this.program.message != null) {

            if (typeof this.program.message !== 'string') {
                this.writer.write();
            } else {
                this.writer.write(this.program.message);
            }

            process.exit();
        }

        this.program.help();
    }

}

let app = new Write();
app.initialize();
