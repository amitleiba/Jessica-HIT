import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class AlertService {
    private readonly messageService = inject(MessageService);

    danger(detail: string, summary = 'Danger'): void {
        this.messageService.add({
            severity: 'error',
            summary,
            detail,
            life: 5000
        });
    }

    success(detail: string, summary = 'Success'): void {
        this.messageService.add({
            severity: 'success',
            summary,
            detail,
            life: 3000
        });
    }
}
