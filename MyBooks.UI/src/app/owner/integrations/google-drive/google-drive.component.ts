import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { IntegrationService } from '../../../services/integration.service';
import { MatDialogModule, MatDialog } from  '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../../components/shared/confirmation.component';

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
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.integrationService.getIntegrations().subscribe({
      next: (res) => {
        this.integrations = res;
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
      data: {itemType: "Integration", itemSpecific: integration.accountEmail}
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.integrationService.deleteIntegration(integration.id).subscribe({
          next: () => {
            this.integrations = this.integrations.filter(x => x.id !== integration.id);
          },
          error: (err) => console.error('Error deleting integration:', err)
        });
      }
    });    
  }
}
