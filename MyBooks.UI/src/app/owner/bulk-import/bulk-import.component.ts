import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';

import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatTableModule } from '@angular/material/table';

import { IntegrationService } from '../../services/integration.service';
import { BookService } from '../../services/book.service';
import {
  BulkImportService,
  BulkImportStartDto,
  BulkImportFileOverrideDto
} from '../../services/bulk-import.service';

import { BulkImportJobsDialogComponent } from './bulk-import-dialog/bulk-import-dialog.component';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { ToastService } from '../../services/toast.service';
import { ToastComponent } from '../../components/shared/toast.component';
import { GlobalLoadingService, LoadingContext } from '../../services/global-loading.service';

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
    private route: ActivatedRoute,
    private integrationService: IntegrationService,
    private bulkImportService: BulkImportService,
    private bookService: BookService,
    private toastService: ToastService,
    private dialog: MatDialog,
    private globalLoading: GlobalLoadingService,
    private cd: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    this.form = this.fb.group({
      integrationId: [null, Validators.required],
      files: [[]]
    });

    await this.loadIntegrations();
    await this.loadLookups();
    await this.loadFilesFromQuery();
  }

  private async loadIntegrations(): Promise<void> {
    this.integrations = await firstValueFrom(this.integrationService.getIntegrations());

    if (this.integrations.length === 1) {
      this.onIntegrationSelected(this.integrations[0].id);
    }
  }

  private async loadLookups(): Promise<void> {
    this.genres = await firstValueFrom(this.bookService.getGenres());
    this.ageCategories = await firstValueFrom(this.bookService.getAgeCategories());

    this.globalGenreId = this.genres[0]?.id;
    this.globalAgeCategoryId = 3;

    this.originalGlobalGenreId = this.globalGenreId;
    this.originalGlobalAgeCategoryId = this.globalAgeCategoryId;
  }

  private async loadFilesFromQuery(): Promise<void> {
    const idsParam = this.route.snapshot.queryParamMap.get('ids');
    if (!idsParam) return;

    if (!this.selectedIntegrationId) {
      this.toastService.show('No Google Drive integration selected.');
      return;
    }

    const fileIds = idsParam.split(',');

    const existingFileIds = await firstValueFrom(
      this.bulkImportService.getExistingFileIds(this.selectedIntegrationId)
    );

    this.files = fileIds.map(id => ({
      id,
      name: id, // backend resolves real name
      selected: true,
      skipFile: existingFileIds.includes(id),
      overrideGenreId: this.globalGenreId,
      overrideAgeCategoryId: this.globalAgeCategoryId
    }));

    this.newFiles = this.files.filter(f => !f.skipFile);
    this.cd.detectChanges();
  }

  onIntegrationSelected(integrationId: number): void {
    this.selectedIntegrationId = integrationId;
    this.form.get('integrationId')?.setValue(integrationId);
  }

  applyGlobalGenre(): void {
    this.files.forEach(f => {
      if (f.overrideGenreId === this.originalGlobalGenreId) {
        f.overrideGenreId = this.globalGenreId;
      }
    });
    this.originalGlobalGenreId = this.globalGenreId;
  }

  applyGlobalAge(): void {
    this.files.forEach(f => {
      if (f.overrideAgeCategoryId === this.originalGlobalAgeCategoryId) {
        f.overrideAgeCategoryId = this.globalAgeCategoryId;
      }
    });
    this.originalGlobalAgeCategoryId = this.globalAgeCategoryId;
  }

  submit(): void {
    if (this.form.invalid || this.newFiles.length === 0) {
      this.toastService.show('No files selected for import.');
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Metadata Warning',
        message: `Book titles may be extracted from file metadata. If unavailable, filenames will be used.`,
        confirmText: 'Import'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

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
        overrides: overrides.length ? overrides : undefined
      };

      this.globalLoading.show(
        'Setting up your bulk import…',
        LoadingContext.BulkImport
      );

      this.bulkImportService.startBulkImport(dto).subscribe({
        next: () => {
          this.toastService.show('Bulk import running');
          this.resetForm();
          this.globalLoading.hide();
        },
        error: () => {
          this.toastService.show('Failed to start bulk import');
          this.globalLoading.hide();
        }
      });
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
    this.newFiles = [];
  }
}
