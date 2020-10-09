export class ImportStatusDto {
    public correlationId: string;
    public totalEntries: number;
    public succeeded: number;
    public failed: number;
    public progress: number;
    public startedOn: Date;
    public completedOn?: Date;
    public errorMessage: string;

    constructor(init?: Partial<ImportStatusDto>) {
        Object.assign(this, init);
    }
}
