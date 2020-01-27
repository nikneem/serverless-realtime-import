import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { ImportResultDto } from 'src/app/shared/models/import-result-dto';

@Component({
    selector: 'app-home-page',
    templateUrl: './home-page.component.html',
    styleUrls: ['./home-page.component.scss']
})
export class HomePageComponent implements OnInit {
    private _hubConnection: HubConnection;
    // messageReceived = new EventEmitter<Message>();
    // connectionEstablished = new EventEmitter<Boolean>();

    // private connectionIsEstablished = false;
    constructor(private messageService: MessageService) {}

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
            .withUrl('http://localhost:7071/api/import')
            .build();
    }

    private startConnection(): void {
        this._hubConnection
            .start()
            .then(() => {
                //                this.connectionIsEstablished = true;
                console.log('Hub connection started');
                //                this.connectionEstablished.emit(true);
            })
            .catch(err => {
                console.log('Error while establishing connection, retrying...');
                setTimeout(function() {
                    this.startConnection();
                }, 5000);
            });
    }

    private registerOnServerEvents(): void {
        this._hubConnection.on(
            'importCompleteSummary',
            (data: ImportResultDto) => {
                this.addSingle(data.TotalSucceeded);
            }
        );
    }

    ngOnInit() {
        this.createConnection();
        this.registerOnServerEvents();
        this.startConnection();
    }
}
