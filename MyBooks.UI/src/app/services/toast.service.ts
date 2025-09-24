import { Injectable } from '@angular/core';
import { ToastComponent } from '../components/shared/toast.component';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toast?: ToastComponent;

  register(toast: ToastComponent) {
    this.toast = toast;
  }

  show(msg: string, duration: number = 3000) {
    this.toast?.show(msg, duration);
  }
}
