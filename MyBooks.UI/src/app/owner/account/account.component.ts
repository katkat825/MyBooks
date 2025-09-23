import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { AccountUsersComponent } from './account-users/account-users.component';
import { GoogleDriveComponent } from '../integrations/google-drive/google-drive.component';
import { BulkImportComponent } from '../bulk-import/bulk-import.component';


@Component({
  selector: 'app-account',
  standalone: true,
  templateUrl: './account.component.html',
  styleUrl: './account.component.css',
  imports: [
    AccountUsersComponent,
    MatTabsModule,
    RouterModule,
    GoogleDriveComponent,
    BulkImportComponent
  ]
})
export class AcountComponent {
  constructor(private router: Router) {}

  currentTab = 'users'
  
  navigateToUsers() {
    this.router.navigate(['/account/users']);
  }

  navigateToIntegrations() {
    this.router.navigate(['/account/integrations']);
  }

  navigateToBulkImport() {
    this.router.navigate(['/account/bulk-import']);
  }
}
