import { Component, OnInit } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { HttpClient } from '@angular/common/http';
import { BulkImportService } from '../../services/bulk-import.service';

@Component({
  selector: 'app-bulk-import-jobs-dialog',
  standalone: true,
  templateUrl: './bulk-import-dialog.component.html',
  styleUrls: ['./bulk-import-dialog.component.css'],
  imports: [CommonModule, MatDialogModule, MatTableModule]
})
export class BulkImportJobsDialogComponent implements OnInit {
  jobs: any[] = [];

  constructor(
    private http: HttpClient,
    public dialogRef: MatDialogRef<BulkImportJobsDialogComponent>,
    private bulkImportService: BulkImportService
  ) {}

  ngOnInit(): void {
    this.bulkImportService.getJobs().subscribe({
      next: (res) => (this.jobs = res),
      error: (err) => console.error('error fetching jobs:', err)
    });
  }
}
