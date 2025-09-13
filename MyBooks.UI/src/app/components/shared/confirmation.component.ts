import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  template: `
    <div class="dialog">
      <h2 mat-dialog-title class="dialog-title">Delete {{ data.itemType }}</h2>
      <mat-dialog-content class="dialog-message">        
        Are you sure you want to delete the {{ data.itemType.toLowerCase() }} 
        <strong>{{ data.itemSpecific }}</strong>?
        <br/><br/>
        <span class="dialog-warning">This action cannot be undone.</span>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button (click)="onNoClick()">Cancel</button>
        <button mat-button class="warn-btn" (click)="onYesClick()">Delete {{ data.itemType }}</button>
      </mat-dialog-actions>
    </div>
  `,
  imports: [MatButtonModule, MatDialogModule]
})
export class ConfirmDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { itemType: string; itemSpecific: string }
  ) {}

  onNoClick(): void {
    this.dialogRef.close(false);
  }

  onYesClick(): void {
    this.dialogRef.close(true);
  }
}
