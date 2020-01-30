import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HomeRoutingModule } from './home-routing.module';
import { HomePageComponent } from './home-page/home-page.component';

import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ProgressBarModule } from 'primeng/progressbar';
@NgModule({
    declarations: [HomePageComponent],
    imports: [CommonModule, HomeRoutingModule, ToastModule, ProgressBarModule],
    providers: [MessageService]
})
export class HomeModule {}
