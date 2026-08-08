import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule, MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { IntegrationService } from '../../../services/integration.service';
import { ConfirmDialogComponent } from '../../../components/shared/confirmation.component';
import { AddGoogleDriveFolderComponent } from '../add-google-drive-folder/add-google-drive-folder.component';
import { environment } from '../../../../environments/environment';
import { firstValueFrom } from 'rxjs';

declare const gapi: any;
declare const google: any;

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
  selectedFolderName: string | null = null;
  selectedFiles: any[] = [];
  selectedIntegrationId!: number;

  constructor(
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

  async openGooglePicker(integration: any): Promise<void> {
    try {
      const tokenResponse = await firstValueFrom(
        this.integrationService.getAccessToken(integration.id)
      );

      const accessToken = tokenResponse?.accessToken;
      if (!accessToken) throw new Error('Failed to get access token');

      // load the picker API
      await new Promise<void>((resolve, reject) => {
        gapi.load('picker', { callback: resolve, onerror: reject });
      });

      // configure the picker view for PDFs and EPUBs
      const view = new google.picker.DocsView()
        .setMimeTypes('application/pdf,application/epub+zip')
        .setIncludeFolders(true)
        .setSelectFolderEnabled(false);

      // build the picker
      const picker = new google.picker.PickerBuilder()
        .enableFeature(google.picker.Feature.MULTISELECT_ENABLED)
        .addView(view)
        .setOAuthToken(accessToken)
        .setDeveloperKey(environment.googlePickerApiKey)
        .setAppId(environment.googleCloudProjectNumber)
        .setCallback(async (data: any) => {
          if (data.action === google.picker.Action.PICKED) {
            // filter for actual files only
            const pickedFiles = data.docs.filter(
              (d: any) => d.mimeType !== 'application/vnd.google-apps.folder'
            );

            if (pickedFiles.length === 0) return;

            // store picked files for the bulk import table
            this.selectedFiles = pickedFiles.map((f: any) => ({
              id: f.id,
              name: f.name
            }));

            // store the integration ID for the import call
            this.selectedIntegrationId = integration.id;

            // force Angular to update the view so the import table appears
            this.cdr.detectChanges();
          }
        })
        .build();

      picker.setVisible(true);
    } catch (error) {
      console.error('Error opening Google Picker:', error);
      alert('Error opening Google Picker');
    }
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
