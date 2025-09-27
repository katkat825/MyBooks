import { Component, OnInit } from '@angular/core';
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

import { IntegrationService } from '../../services/integration.service';
import { BookService } from '../../services/book.service';
import { BulkImportService, BulkImportStartDto, BulkImportFileOverrideDto } from '../../services/bulk-import.service';
import { BulkImportJobsDialogComponent } from './bulk-import-dialog/bulk-import-dialog.component';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { ToastService } from '../../services/toast.service';
import { ToastComponent } from '../../components/shared/toast.component';
import { GlobalLoadingService } from '../../services/global-loading.service';

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
  genres: any[] = [];
  ageCategories: any[] = [];
  selectedIntegrationId!: number;

  globalGenreId!: number;
  globalAgeCategoryId!: number;

  private originalGlobalGenreId: number | null = null;
  private originalGlobalAgeCategoryId: number | null = null;

  breadcrumb: { id: string | null, name: string }[] = [];

  constructor(
    private fb: FormBuilder,
    private integrationService: IntegrationService,
    private bulkImportService: BulkImportService,
    private bookService: BookService,
    private toastService: ToastService,
    private dialog: MatDialog,
    private globalLoading: GlobalLoadingService
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
      }
    });
    this.bookService.getAgeCategories().subscribe((cats: any[]) => {
      this.ageCategories = cats;
      if (!this.globalAgeCategoryId) {
        this.globalAgeCategoryId = 3;
      }
    });
  }

  private loadFolderContents(folderId: string | null): void {
    this.integrationService.getImportableFiles(this.selectedIntegrationId, folderId ? folderId : undefined).subscribe({
      next: (files: any[]) => {
      this.files = files.map(f => ({
        id: f.id,
        name: f.name,
        isFolder: f.isFolder,
        selected: false,
        overrideGenreId: null,
        overrideAgeCategoryId: null
      }))
      .sort((a, b) => {
        if (a.isFolder && !b.isFolder) return -1;
        if (!a.isFolder && b.isFolder) return 1;
        return a.name.localeCompare(b.name);
      });

      this.applyGlobalAge();
      this.applyGlobalGenre();
    },
      error: err => {
        console.error('Error loading folder contents:', err);
        this.toastService.show('Failed to load folder contents');
      }
    });
  }

  private addFileToForm(file: any): void {
    const filesArray = this.form.get('files') as FormArray;
    filesArray.push(this.fb.group({
      fileId: [file.id],
      fileName: [file.name],
      genreId: [this.globalGenreId, Validators.required],
      ageCategoryId: [this.globalAgeCategoryId, Validators.required]
    }));
  }

  private removeFileFromForm(fileId: string): void {
    const filesArray = this.form.get('files') as FormArray;
    const index = filesArray.controls.findIndex(ctrl => ctrl.get('fileId')?.value === fileId);
    if (index !== -1) {
      filesArray.removeAt(index);
    }
  }

  onIntegrationSelected(integrationId: number): void {
    this.selectedIntegrationId = integrationId;
    this.form.get('integrationId')?.setValue(integrationId);
    this.breadcrumb = [{ id: null, name: 'My Drive' }];
    this.loadFolderContents(null);
  }

  onFolderClick(folder: any): void {
    const last = this.breadcrumb[this.breadcrumb.length - 1];
    if (last && last.id === folder.id) return; // already here
    this.breadcrumb.push({ id: folder.id, name: folder.name });
    this.loadFolderContents(folder.id);
  }

  navigateTo(index: number, event: Event): void {
    event.preventDefault();
    const crumb = this.breadcrumb[index];
    this.breadcrumb = this.breadcrumb.slice(0, index + 1);
    this.loadFolderContents(crumb.id);
  }

  // table helpers
  isAllSelected(): boolean {
    const fileItems = this.files.filter(f => !f.isFolder);
    if (fileItems.length === 0) return false;
    return fileItems.length > 0 && fileItems.every(f => f.selected);
  }

  isIndeterminate(): boolean {
    const fileItems = this.files.filter(f => !f.isFolder);
    if (fileItems.length === 0) return false;
    return fileItems.some(f => f.selected) && !this.isAllSelected();
  }

  toggleAllSelection(event: any): void {
    const checked = event.checked;
    this.files.forEach(f => {
      if (!f.isFolder) {
        f.selected = checked;
        if (!checked) {
          this.removeFileFromForm(f.id);
        } else {
          this.addFileToForm(f);
        }
      }
    });
  }

  toggleFileSelection(file: any): void {
    if (file.isFolder) {
      this.onFolderClick(file);
    } else {
      file.selected = !file.selected;
      if (file.selected) {
        this.addFileToForm(file);
      } else {
        this.removeFileFromForm(file.id);
      }
    }
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

    const selected = this.files.filter(f => f.selected);
    if (selected.length === 0) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Metadata Warning',
        message: `We will try to extract the book title from each PDF. 
                However, many PDFs do not contain this information in a readable form. 
                If that happens, the book title in this app will default to the filename.
                <br/><br/>
                Click the red Import button to proceed`,
        confirmText: 'Import',
        permanent: false
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const fileIds = selected.map(f => f.id);

        const overrides: BulkImportFileOverrideDto[] = selected
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
          fileIds,
          genreId: this.globalGenreId,
          ageCategoryId: this.globalAgeCategoryId,
          integrationId: this.selectedIntegrationId,
          overrides: overrides.length > 0 ? overrides : undefined
        };

        this.globalLoading.show("Setting up your bulk import... Don't leave this page until setup finishes");
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

  get noFilesSelected(): boolean {
    return this.files.every(f => !f.selected);
  }

  private resetForm(): void {
    this.form.reset();
    this.breadcrumb = [];
    this.files = [];
  }
}
