import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HomeRoutingModule } from './home-routing.module';
import { HomePageComponent } from './home-page/home-page.component';

import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

@NgModule({
    declarations: [HomePageComponent],
    imports: [CommonModule, HomeRoutingModule, ToastModule],
    providers: [MessageService]
})
export class HomeModule {}
