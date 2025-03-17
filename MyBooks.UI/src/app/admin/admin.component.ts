import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { Router, RouterModule } from '@angular/router';
import { AdminGenresComponent } from './admin-genres/admin-genres.component';
import { AdminSeriesComponent } from './admin-series/admin-series.component';
import { AdminUsersComponent } from './admin-users/admin-users.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css'],
  imports: [
    RouterModule,
    MatTabsModule,
    CommonModule,
    AdminSeriesComponent,
    AdminGenresComponent,
    AdminUsersComponent
  ]
})

export class AdminComponent {
  constructor(private router: Router) { }

  currentTab = 'series';

  navigateToSeries() {
    this.router.navigate(['/admin/series']);
  }

  navigateToGenres() {
    this.router.navigate(['/admin/genres']);
  }

  navigateToUsers() {
    this.router.navigate(['/admin/users']);
  }
}
