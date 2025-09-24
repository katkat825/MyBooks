import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="visible" class="toast">
      {{ message }}
    </div>
  `,
  styleUrls: ['./toast.component.css']
})
export class ToastComponent {
  message = '';
  visible = false;

  show(msg: string, duration: number = 3000) {
    this.message = msg;
    this.visible = true;

    setTimeout(() => {
      this.visible = false;
    }, duration);
  }
}
