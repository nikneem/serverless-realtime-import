import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { ImportStatusDto } from 'src/app/shared/models/import-result-dto';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-home-page',
    templateUrl: './home-page.component.html',
    styleUrls: ['./home-page.component.scss']
})
export class HomePageComponent implements OnInit {
    private _hubConnection: HubConnection;

    imports: Array<ImportStatusDto>;
    // messageReceived = new EventEmitter<Message>();
    // connectionEstablished = new EventEmitter<Boolean>();

    // private connectionIsEstablished = false;
    constructor(private messageService: MessageService) {}

    reportStatusComplete(dto: ImportStatusDto) {
        let severity = 'success';
        let message = `Import complete, imported ${dto.Succeeded} users`;
        if (dto.Succeeded > 0 && dto.Failed > 0) {
            severity = 'info';
            message = `Import complete, ${dto.Succeeded} of the ${dto.TotalEntries} users imported, ${dto.Failed} users failed to import`;
        }
        if (dto.Succeeded === 0 && dto.Failed > 0) {
            severity = 'error';
            message = `Import complete, but failed... ${dto.Failed} users of the ${dto.TotalEntries} failed to import`;
        }
        this.messageService.add({
            severity: severity,
            summary: 'Import completed',
            detail: message,
            life: 7500
        });
    }
    reportStartupFailure(reason: string) {
        let severity = 'error';
        this.messageService.add({
            severity: severity,
            summary: 'Import failed',
            detail: reason
        });
    }

    addSingle(totalSucceeded: number) {
        this.messageService.add({
            severity: 'success',
            summary: 'Import completed',
            detail: `A total of ${totalSucceeded} users have been imported`
        });
    }

    addMultiple() {
        this.messageService.addAll([
            {
                severity: 'success',
                summary: 'Service Message',
                detail: 'Via MessageService'
            },
            {
                severity: 'info',
                summary: 'Info Message',
                detail: 'Via MessageService'
            }
        ]);
    }

    clear() {
        this.messageService.clear();
    }

    private createConnection() {
        this._hubConnection = new HubConnectionBuilder()
            .withUrl(`${environment.backend}/api/import`)
            .build();
    }

    private startConnection(): void {
        this._hubConnection
            .start()
            .then(() => {
                console.log('Hub connection started');
            })
            .catch(err => {
                console.log('Error while establishing connection, retrying...');
                setTimeout(function() {
                    this.startConnection();
                }, 5000);
            });
    }

    private registerOnServerEvents(): void {
        this._hubConnection.on('newImport', (data: ImportStatusDto) => {
            console.log('New import message came in');
            if (!data.CompletedOn) {
                this.imports.push(data);
            } else {
                this.reportStartupFailure(data.ErrorMessage);
            }
        });
        this._hubConnection.on('updateImport', (data: ImportStatusDto) => {
            const update = this.imports.find(
                ent => ent.CorrelationId === data.CorrelationId
            );
            if (update) {
                console.log(`Update ${update.CorrelationId} - ${update.TotalEntries} - ${update.Succeeded} - ${update.Failed}`);
                update.CompletedOn = data.CompletedOn;
                update.ErrorMessage = data.ErrorMessage;
                update.Succeeded = data.Succeeded;
                update.Failed = data.Failed;
                if (data.TotalEntries > 0) {
                    const process = Math.round(
                        (100 / data.TotalEntries) *
                            (data.Succeeded + data.Failed)
                    );
                    update.Progress = process;
                }
            } else {
                if (data.TotalEntries > 0) {
                    data.Progress = Math.round(
                        (100 / data.TotalEntries) *
                            (data.Succeeded + data.Failed)
                    );
                }
                this.imports.push(data);
            }
            if (update && update.CompletedOn) {
                const index = this.imports.indexOf(update);
                this.imports.splice(index, 1);
                this.reportStatusComplete(update);
            }
        });
    }

    ngOnInit() {
        this.imports = new Array<ImportStatusDto>();
        this.createConnection();
        this.registerOnServerEvents();
        this.startConnection();
    }
}
