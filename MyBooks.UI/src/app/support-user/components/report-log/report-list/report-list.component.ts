import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';
import { SupportUserService, ReportLog } from '../../../../services/support-user.service';
import { GlobalLoadingService, LoadingContext } from '../../../../services/global-loading.service';

@Component({
  selector: 'app-report-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './report-list.component.html',
  styleUrls: ['./report-list.component.css']
})
export class ReportListComponent implements OnInit {
  reports: ReportLog[] = [];
  displayedColumns: string[] = [
    'id',
    'status',
    'actions',
    'dateReceived',
    'reportedBy',
    'description',
    'targetType',
    'targetId',
    'targetCreatedBy',
    'dateClosed',
    'resolution',
    'resolutionNotes'
  ];

  constructor(
    private supportService: SupportUserService,
    private router: Router,
    private loadingService: GlobalLoadingService
  ) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loadingService.show('Loading reports...', LoadingContext.Default);
    this.supportService.getAllReports().subscribe({
      next: (data) => {
        this.reports = data;
        this.loadingService.hide();
      },
      error: (err) => {
        console.error('Failed to load reports:', err);
        this.loadingService.hide();
      }
    });
  }

  viewReport(report: ReportLog): void {
    this.router.navigate(['/support/report-logs/details', report.id]);
  }

  createReport(): void {
    this.router.navigate(['/support/report-logs/new']);
  }
}
