import { ChangeDetectorRef, Component, OnInit, NgZone } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule, FormArray } from '@angular/forms';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatTableModule } from '@angular/material/table';
import { firstValueFrom } from 'rxjs';

import { IntegrationService } from '../../services/integration.service';
import { BookService } from '../../services/book.service';
import { BulkImportService, BulkImportStartDto, BulkImportFileOverrideDto } from '../../services/bulk-import.service';
import { BulkImportJobsDialogComponent } from './bulk-import-dialog/bulk-import-dialog.component';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { ToastService } from '../../services/toast.service';
import { ToastComponent } from '../../components/shared/toast.component';
import { GlobalLoadingService, LoadingContext } from '../../services/global-loading.service';
import { environment } from '../../../environments/environment';

declare const gapi: any;
declare const google: any;

@Component({
  selector: 'app-bulk-import',
  templateUrl: './bulk-import.component.html',
  styleUrls: ['./bulk-import.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    ToastComponent,
    MatDividerModule,
    MatMenuModule,
    MatTableModule
  ]
})
export class BulkImportComponent implements OnInit {
  form!: FormGroup;
  integrations: any[] = [];
  files: any[] = [];
  newFiles: any[] = [];
  genres: any[] = [];
  ageCategories: any[] = [];
  selectedIntegrationId!: number;

  globalGenreId!: number;
  globalAgeCategoryId!: number;

  private originalGlobalGenreId: number | null = null;
  private originalGlobalAgeCategoryId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private integrationService: IntegrationService,
    private bulkImportService: BulkImportService,
    private bookService: BookService,
    private toastService: ToastService,
    private dialog: MatDialog,
    private globalLoading: GlobalLoadingService,
    private cd: ChangeDetectorRef,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      integrationId: [null, Validators.required],
      files: this.fb.array([])
    });

    this.loadIntegrations();
    this.loadLookups();
  }

  private loadIntegrations(): void {
    this.integrationService.getIntegrations().subscribe((integrations: any[]) => {
      this.integrations = integrations;

      // auto-select if only one
      if (this.integrations.length === 1) {
        this.onIntegrationSelected(this.integrations[0].id);
      }
    });
  }

  private loadLookups(): void {
    this.bookService.getGenres().subscribe((genres: any[]) => {
      this.genres = genres;
      if (this.genres.length > 0 && !this.globalGenreId) {
        this.globalGenreId = this.genres[0].id;
        this.originalGlobalGenreId = this.globalGenreId;
      }
    });
    this.bookService.getAgeCategories().subscribe((cats: any[]) => {
      this.ageCategories = cats;
      if (!this.globalAgeCategoryId) {
        this.globalAgeCategoryId = 3;
        this.originalGlobalAgeCategoryId = this.globalAgeCategoryId;
      }
    });
  }

  async openGooglePicker(): Promise<void> {
    if (!this.selectedIntegrationId) {
      this.toastService.show('Please select a Google Drive integration first.');
      return;
    }

    try {
      const tokenResponse = await firstValueFrom(
        this.integrationService.getAccessToken(this.selectedIntegrationId)
      );
      const accessToken = tokenResponse?.accessToken;
      if (!accessToken) throw new Error('Failed to get access token');

      // load the picker API
      await new Promise<void>((resolve, reject) => {
        gapi.load('picker', { callback: resolve, onerror: reject });
      });

      const view = new google.picker.DocsView(google.picker.ViewId.FOLDERS)
        .setIncludeFolders(true)
        .setSelectFolderEnabled(true);

      const picker = new google.picker.PickerBuilder()
        .addView(view)
        .setOAuthToken(accessToken)
        .setCallback(async (data: any) => {
          if(data.action === google.picker.Action.PICKED) {
            const folder = data.docs[0];
            const folderId = folder?.id || folder?.resourceId || folder?.docId;

            if (!folderId) {
              console.log('could not determine folder id from picker', folder);
              return;
            }

            await new Promise<void>((resolve, reject) => {
              gapi.load('client', { callback: resolve, onerror: reject });
            });

            await new Promise<void>((resolve, reject) => {
              gapi.client.load('drive', 'v3', {
                callback: resolve, onerror: reject
              });
            });
            
            await gapi.client.drive.files.get({
              fileId: folderId,
              fields: 'id'
            });

            await firstValueFrom(
              this.integrationService.updateFolders(
                this.selectedIntegrationId,
                [folder.id]
              )
            );

            this.toastService.show(`${folder.length} folders connected`);
            this.loadFilesFromFolders();
          }
        })
        .build();

      picker.setVisible(true);

    } catch (error) {
      console.error('Error opening Google Picker:', error);
      this.toastService.show('Error opening Google Picker');
    }
  }

  onIntegrationSelected(integrationId: number): void {
    this.selectedIntegrationId = integrationId;
    this.form.get('integrationId')?.setValue(integrationId);
  }

  applyGlobalGenre(): void {
    this.files.forEach(f => {
      if (f.overrideGenreId == null || f.overrideGenreId === this.originalGlobalGenreId) {
        f.overrideGenreId = this.globalGenreId;
      }
    });
    this.originalGlobalGenreId = this.globalGenreId;
  }

  applyGlobalAge(): void {
    this.files.forEach(f => {
      if (f.overrideAgeCategoryId == null || f.overrideAgeCategoryId === this.originalGlobalAgeCategoryId) {
        f.overrideAgeCategoryId = this.globalAgeCategoryId;
      }
    });
    this.originalGlobalAgeCategoryId = this.globalAgeCategoryId;
  }

  submit(): void {
    if (this.form.invalid) return;
    if (this.files.length === 0) {
      this.toastService.show('No files selected for import.');
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Metadata Warning',
        message: `We will try to extract the book title from each PDF. 
                However, many PDFs do not contain this information in a readable form. 
                If that happens, the book title in this app will default to the filename.
                <br/><br/>
                Click the red Import button to proceed.`,
        confirmText: 'Import',
        permanent: false
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const overrides: BulkImportFileOverrideDto[] = this.files
          .filter(f =>
            f.overrideGenreId !== this.globalGenreId ||
            f.overrideAgeCategoryId !== this.globalAgeCategoryId
          )
          .map(f => ({
            fileId: f.id,
            genreId: f.overrideGenreId,
            ageCategoryId: f.overrideAgeCategoryId
          }));

        const dto: BulkImportStartDto = {
          fileIds: this.newFiles.map(f => f.id),
          genreId: this.globalGenreId,
          ageCategoryId: this.globalAgeCategoryId,
          integrationId: this.selectedIntegrationId,
          overrides: overrides.length > 0 ? overrides : undefined
        };

        this.globalLoading.show("Setting up your bulk import... Don't leave this page until setup finishes", LoadingContext.BulkImport);
        this.bulkImportService.startBulkImport(dto).subscribe({
          next: () => {
            this.toastService.show('Bulk import now running in the background');
            this.resetForm();
            this.globalLoading.hide();
          },
          error: err => {
            console.error('Error starting bulk import:', err);
            this.toastService.show('Failed to start bulk import');
            this.globalLoading.hide();
          }
        });
      }
    });
  }

  openJobsDialog(): void {
    this.dialog.open(BulkImportJobsDialogComponent, {
      width: '600px',
      panelClass: 'bulk-import-dialog'
    });
  }

  private resetForm(): void {
    this.form.reset();
    this.files = [];
  }

  private loadFilesFromFolders(): void {
    this.integrationService
      .getFiles(this.selectedIntegrationId)
      .subscribe(files => {
        this.files = files.map((f: any) => ({
          id: f.id,
          selected: true,
          skipFile: false,
          overrideGenreId: this.globalGenreId,
          overrideAgeCategoryId: this.globalAgeCategoryId
        }));

        this.newFiles = this.files;
        this.cd.detectChanges();
      });
  }
}
