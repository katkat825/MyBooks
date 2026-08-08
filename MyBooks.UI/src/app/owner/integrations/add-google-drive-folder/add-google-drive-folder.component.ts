import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { IntegrationService } from '../../../services/integration.service';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

declare const gapi: any;
declare const google: any;

@Component({
  selector: 'app-add-google-drive-folder',
  standalone: true,
  templateUrl: './add-google-drive-folder.component.html',
  styleUrls: ['./add-google-drive-folder.component.css'],
  imports: [CommonModule, MatButtonModule]
})
export class AddGoogleDriveFolderComponent {
  selectedFolderName: string | null = null;

  constructor(
    private integrationService: IntegrationService,
    private dialogRef: MatDialogRef<AddGoogleDriveFolderComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { integrationId: number }
  ) {}

  async openGooglePicker(): Promise<void> {
    try {
      const tokenResponse = await firstValueFrom(
        this.integrationService.getAccessToken(this.data.integrationId)
      );

      const accessToken = tokenResponse?.accessToken;
      if (!accessToken) throw new Error('Failed to get access token');

      // load the picker API
      await new Promise<void>((resolve, reject) => {
        gapi.load('picker', { callback: resolve, onerror: reject });
      });

      const view = new google.picker.DocsView(google.picker.ViewId.FOLDERS)
        .setSelectFolderEnabled(true);

      const picker = new google.picker.PickerBuilder()
        .addView(view)
        .setOAuthToken(accessToken)
        .setDeveloperKey(environment.googlePickerApiKey)
        .setAppId(environment.googleCloudProjectNumber)
        .setCallback(async (data: any) => {
          if (data.action === google.picker.Action.PICKED) {
            const picked = data.docs[0];
            const folderId = picked.id;
            this.selectedFolderName = picked.name;

            await firstValueFrom(
              this.integrationService.updateFolders(this.data.integrationId, [folderId])
            );

            this.dialogRef.close(true);
          } else if (data.action === google.picker.Action.CANCEL) {
            this.dialogRef.close(false);
          }
        })
        .build();

      picker.setVisible(true);
    } catch (error) {
      console.error('Error opening Google Picker:', error);
      this.dialogRef.close(false);
    }
  }

  close(): void {
    this.dialogRef.close(false);
  }
}
