import { Component, Input, Output, EventEmitter, OnInit, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-bulk-import-table',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatCheckboxModule,
    MatSelectModule,
    MatFormFieldModule,
    FormsModule,
    MatIconModule
  ],
  templateUrl: './bulk-import-table.component.html',
  styleUrls: ['./bulk-import-table.component.css']
})
export class BulkImportTableComponent {
  @Input() files: any[] = [];
  @Input() genres: any[] = [];
  @Input() ageCategories: any[] = [];
  @Input() globalGenreId!: number;
  @Input() globalAgeCategoryId!: number;

  @Output() filesChange = new EventEmitter<any[]>();
  @Output() globalGenreChange = new EventEmitter<number>();
  @Output() globalAgeChange = new EventEmitter<number>();

  private originalGlobalGenreId: number | null = null;
  private originalGlobalAgeCategoryId: number | null = null;

  ngOnInit(): void {
    this.originalGlobalGenreId = this.globalGenreId;
    this.originalGlobalAgeCategoryId = this.globalAgeCategoryId;

    this.applyGlobalAge();
    this.applyGlobalGenre();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['files'] && this.files?.length > 0) {
      this.applyGlobalAge();
      this.applyGlobalGenre();
    }
  }

  isAllSelected(): boolean {
    return this.files.length > 0 && this.files.every(f => f.selected);
  }

  isIndeterminate(): boolean {
    return this.files.some(f => f.selected) && !this.isAllSelected();
  }

  toggleAllSelection(event: any): void {
    const checked = event.checked;
    this.files.forEach(f => (f.selected = checked));
    this.filesChange.emit(this.files);
  }

  toggleFileSelection(file: any): void {
    file.selected = !file.selected;
    this.filesChange.emit(this.files);
  }

  applyGlobalGenre(): void {
    this.files.forEach(f => {
      if (f.overrideGenreId == null || f.overrideGenreId === this.originalGlobalGenreId) {
        f.overrideGenreId = this.globalGenreId;
      }
    });

    this.filesChange.emit(this.files);
    this.globalGenreChange.emit(this.globalGenreId);
    this.originalGlobalGenreId = this.globalGenreId;
  }

  applyGlobalAge(): void {
    this.files.forEach(f => {
      if (f.overrideAgeCategoryId == null || f.overrideAgeCategoryId === this.originalGlobalAgeCategoryId) {
        f.overrideAgeCategoryId = this.globalAgeCategoryId;
      }
    });

    this.filesChange.emit(this.files);
    this.globalAgeChange.emit(this.globalAgeCategoryId);
    this.originalGlobalAgeCategoryId = this.globalAgeCategoryId;
  }
}
