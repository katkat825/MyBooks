import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { IntegrationService } from '../../../services/integration.service';
import { ConfirmDialogComponent } from '../../../components/shared/confirmation.component';
import { AddGoogleDriveFolderComponent } from '../add-google-drive-folder/add-google-drive-folder.component';

@Component({
  selector: 'app-google-drive',
  standalone: true,
  templateUrl: './google-drive.component.html',
  styleUrls: ['./google-drive.component.css'],
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatDialogModule
  ]
})
export class GoogleDriveComponent implements OnInit {
  integrations: any[] = [];

  constructor(
    private http: HttpClient,
    private integrationService: IntegrationService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadIntegrations();
  }

  private loadIntegrations(): void {
    this.integrationService.getIntegrations().subscribe({
      next: (res) => {
        this.integrations = res;

        // Load folders for each integration
        this.integrations.forEach((integration) => {
          this.integrationService.getFolders(integration.id).subscribe({
            next: (folders) => {
              integration.folders = folders.filter(f => f.isSelected);
              this.cdr.detectChanges(); // trigger view update
            },
            error: (err) => console.error('Error loading folders:', err)
          });
        });
      },
      error: (err) => console.error('Error fetching integrations:', err)
    });
  }

  connect(): void {
    this.integrationService.getAuthorizeUrl().subscribe({
      next: (res) => {
        window.location.href = res.url; // hand off to Google OAuth
      },
      error: (err) => console.error('Error getting authorize URL:', err)
    });
  }

  remove(integration: any): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { itemType: 'Integration', itemSpecific: integration.accountEmail }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.integrationService.deleteIntegration(integration.id).subscribe({
          next: () => {
            this.integrations = this.integrations.filter(
              (x) => x.id !== integration.id
            );
          },
          error: (err) => console.error('Error deleting integration:', err)
        });
      }
    });
  }

  openFolderDialog(integration: any): void {
    const dialogRef = this.dialog.open(AddGoogleDriveFolderComponent, {
      width: '400px',
      panelClass: 'folder-dialog',
      data: { integrationId: integration.id }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadIntegrations(); // refresh after folder save
      }
    });
  }
}
