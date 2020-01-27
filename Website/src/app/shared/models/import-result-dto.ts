export class ImportResultDto {
    public TotalSucceeded: number;
    public TotalFailed: number;

    constructor(init?: Partial<ImportResultDto>) {
        Object.assign(this, init);
    }
}
