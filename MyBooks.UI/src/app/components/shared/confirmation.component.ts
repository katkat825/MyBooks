import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  template: `
    <div class="dialog">
      <h2 mat-dialog-title class="dialog-title">
        {{ data.title || ('Delete ' + data.itemType) }}
      </h2>
      <mat-dialog-content class="dialog-message">
        <span [innerHTML]="message"></span>
        <br/><br/>
        <span class="dialog-warning" *ngIf="permanent">This action cannot be undone.</span>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button (click)="onNoClick()">{{ data.cancelText || 'Cancel' }}</button>
        <button mat-button class="warn-btn" (click)="onYesClick()">
          {{ data.confirmText || ('Delete ' + data.itemType) }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
  imports: [MatButtonModule, MatDialogModule, CommonModule]
})
export class ConfirmDialogComponent {
  message: string; 
  permanent: boolean = true;

  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { 
      itemType: string; 
      itemSpecific: string;
      message?: string;
      title?: string;
      confirmText?: string;
      cancelText?: string;
      permanent?: boolean;
    }
  ) {
    this.message = data.message ||
      `Are you sure you want to delete the ${data.itemType.toLowerCase()} <strong>${data.itemSpecific}</strong>?`;
    
    this.permanent = data.permanent ?? true;
  }

  onNoClick(): void {
    this.dialogRef.close(false);
  }

  onYesClick(): void {
    this.dialogRef.close(true);
  }
}
