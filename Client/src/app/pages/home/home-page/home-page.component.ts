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
        let message = `Import complete, imported ${dto.succeeded} users`;
        if (dto.succeeded > 0 && dto.failed > 0) {
            severity = 'info';
            message = `Import complete, ${dto.succeeded} of the ${dto.totalEntries} users imported, ${dto.failed} users failed to import`;
        }
        if (dto.succeeded === 0 && dto.failed > 0) {
            severity = 'error';
            message = `Import complete, but failed... ${dto.failed} users of the ${dto.totalEntries} failed to import`;
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
        this._hubConnection.on('updateImport', (data: ImportStatusDto) => {
            const update = this.imports.find(
                ent => ent.correlationId === data.correlationId
            );
            if (update) {
                console.log(`Update ${update.correlationId} - ${update.totalEntries} - ${update.succeeded} - ${update.failed}`);
                update.completedOn = data.completedOn;
                update.errorMessage = data.errorMessage;
                update.succeeded = data.succeeded;
                update.failed = data.failed;
                if (data.totalEntries > 0) {
                    const process = Math.round(
                        (100 / data.totalEntries) *
                            (data.succeeded + data.failed)
                    );
                    update.progress = process;
                }
            } else {
                if (data.totalEntries > 0) {
                    data.progress = Math.round(
                        (100 / data.totalEntries) *
                            (data.succeeded + data.failed)
                    );
                }
                this.imports.push(data);
            }
            if (update && update.completedOn) {
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
