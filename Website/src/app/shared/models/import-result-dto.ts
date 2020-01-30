export class ImportStatusDto {
    public CorrelationId: string;
    public TotalEntries: number;
    public Succeeded: number;
    public Failed: number;
    public Progress: number;
    public StartedOn: Date;
    public CompletedOn: Date;
    public ErrorMessage: string;

    constructor(init?: Partial<ImportStatusDto>) {
        Object.assign(this, init);
    }
}
