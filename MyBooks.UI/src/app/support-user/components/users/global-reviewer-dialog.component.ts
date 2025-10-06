import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { SupportUserService } from '../../../services/support-user.service';
import { ToastService } from '../../../services/toast.service';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-global-reviewer-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <h2 mat-dialog-title>GlobalReviewer Access</h2>
    <div mat-dialog-content>
      <p><strong>{{ data.email }}</strong></p>
      <p *ngIf="isLoading">Checking access...</p>
      <p *ngIf="!isLoading">
        Current status:
        <strong>{{ hasAccess ? 'Active' : 'Not Granted' }}</strong>
      </p>
    </div>
    <div mat-dialog-actions align="end">
      <button mat-button (click)="close()">Close</button>
      <button
        mat-flat-button
        color="{{ hasAccess ? 'warn' : 'primary' }}"
        (click)="toggleAccess()"
        [disabled]="isLoading"
      >
        <mat-icon>{{ hasAccess ? 'block' : 'verified_user' }}</mat-icon>
        {{ hasAccess ? 'Revoke' : 'Grant' }}
      </button>
    </div>
  `,
  styles: [`
    h2 { margin-bottom: 8px; }
    [mat-dialog-content] { min-width: 300px; }
  `]
})
export class GlobalReviewerDialogComponent implements OnInit {
  isLoading = false;
  hasAccess = false;

  constructor(
    private dialogRef: MatDialogRef<GlobalReviewerDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { userId: number; email: string },
    private supportService: SupportUserService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadStatus();
  }

  loadStatus(): void {
    this.isLoading = true;
    this.supportService.getGlobalReviewers().subscribe({
      next: (list) => {
        this.hasAccess = list.some((r: any) => r.userId === this.data.userId && r.isActive);
        this.isLoading = false;
      },
      error: () => (this.isLoading = false)
    });
  }

  toggleAccess(): void {
    this.isLoading = true;
    const req = this.hasAccess
      ? this.supportService.revokeGlobalReviewerAccess(this.data.userId)
      : this.supportService.grantGlobalReviewerAccess(this.data.userId);

    req.subscribe({
      next: () => {
        this.toast.show(this.hasAccess ? 'Access revoked' : 'Access granted');
        this.hasAccess = !this.hasAccess;
        this.isLoading = false;
      },
      error: () => {
        this.toast.show('Operation failed');
        this.isLoading = false;
      }
    });
  }

  close(): void {
    this.dialogRef.close(this.hasAccess);
  }
}
