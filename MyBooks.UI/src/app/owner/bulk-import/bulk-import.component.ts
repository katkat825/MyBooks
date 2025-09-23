import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { CommonModule } from '@angular/common';
import { IntegrationService } from '../../services/integration.service';
import { BookService } from '../../services/book.service';
import { BulkImportService, BulkImportStartDto, BulkImportFileOverrideDto } from '../../services/bulk-import.service';

@Component({
  selector: 'app-bulk-import',
  templateUrl: './bulk-import.component.html',
  styleUrls: ['./bulk-import.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatTableModule,
    FormsModule
  ]
})
export class BulkImportComponent implements OnInit {
  form!: FormGroup;

  integrations: any[] = [];
  folders: any[] = [];
  files: any[] = []; // holds selection + overrides
  genres: any[] = [];
  ageCategories: any[] = [];
  selectedIntegrationId!: number;

  constructor(
    private fb: FormBuilder,
    private integrationService: IntegrationService,
    private bulkImportService: BulkImportService,
    private bookService: BookService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      integrationId: [null, Validators.required],
      folderId: [null, Validators.required],
      genreId: [null, Validators.required],
      ageCategoryId: [null, Validators.required]
    });

    this.loadIntegrations();
    this.loadLookups();
  }

  private loadIntegrations(): void {
    this.integrationService.getIntegrations().subscribe((integrations: any[]) => {
      this.integrations = integrations;
    });
  }

  private loadLookups(): void {
    this.bookService.getGenres().subscribe((genres: any[]) => this.genres = genres);
    this.bookService.getAgeCategories().subscribe((cats: any[]) => this.ageCategories = cats);
  }

  onIntegrationSelected(integrationId: number): void {
    this.selectedIntegrationId = integrationId;
    this.integrationService.getFolders(integrationId).subscribe((folders: any[]) => {
      this.folders = folders;
    });
  }

  onFolderSelected(folderId: string): void {
    this.integrationService.getImportableFiles(this.selectedIntegrationId, folderId).subscribe((files: any[]) => {
      // keep local UI state (selected, overrides)
      this.files = files.map(f => ({
        id: f.id,
        name: f.name,
        selected: false,
        overrideGenreId: null,
        overrideAgeCategoryId: null
      }));
    });
  }

  submit(): void {
    if (this.form.invalid) return;

    const selected = this.files.filter(f => f.selected);
    if (selected.length === 0) return;

    const fileIds = selected.map(f => f.id);

    const overrides: BulkImportFileOverrideDto[] = selected
      .filter(f => f.overrideGenreId || f.overrideAgeCategoryId)
      .map(f => ({
        fileId: f.id,
        genreId: f.overrideGenreId || undefined,
        ageCategoryId: f.overrideAgeCategoryId || undefined
      }));

    const dto: BulkImportStartDto = {
      fileIds,
      genreId: this.form.value.genreId,
      ageCategoryId: this.form.value.ageCategoryId,
      integrationId: this.form.value.integrationId,
      overrides: overrides.length > 0 ? overrides : undefined
    };

    this.bulkImportService.startBulkImport(dto).subscribe({
      next: () => this.showSnack('Bulk import started'),
      error: (err) => {
        console.error('Error starting bulk import:', err);
        this.showSnack('Failed to start bulk import');
      }
    });
  }

  private showSnack(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 2000,
      horizontalPosition: 'right',
      verticalPosition: 'bottom',
    });
  }

  get noFilesSelected(): boolean {
    return this.files.every(f => !f.selected);
  }
}
