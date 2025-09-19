import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { IntegrationService } from '../../../services/integration.service';

@Component({
  selector: 'app-add-google-drive-folder',
  standalone: true,
  templateUrl: './add-google-drive-folder.component.html',
  styleUrls: ['./add-google-drive-folder.component.css'],
  imports: [CommonModule, MatListModule, MatButtonModule, MatIconModule]
})
export class AddGoogleDriveFolderComponent implements OnInit {
  folders: any[] = [];
  selected: Set<string> = new Set();

  constructor(
    private integrationService: IntegrationService,
    private dialogRef: MatDialogRef<AddGoogleDriveFolderComponent>,
    private cd: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public data: { integrationId: number }
  ) {}

  ngOnInit(): void {
    this.integrationService.getFolders(this.data.integrationId).subscribe({
      next: (res) => {
        this.folders = res;
        res.filter(f => f.isSelected).forEach(f => this.selected.add(f.id));
        this.cd.detectChanges();
      },
      error: (err) => console.error("Error loading folders:", err)
    });
  }

  toggleSelection(folderId: string): void {
    if (this.selected.has(folderId)) {
      this.selected.delete(folderId);
    } else {
      this.selected.add(folderId);
    }
  }

  save(): void {
    this.integrationService.updateFolders(
      this.data.integrationId,
      Array.from(this.selected)
    ).subscribe({
      next: () => this.dialogRef.close(true),
      error: (err) => console.error("Error updating folders:", err)
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
