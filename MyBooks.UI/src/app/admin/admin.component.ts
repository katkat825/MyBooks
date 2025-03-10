import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatTab, MatTabsModule } from '@angular/material/tabs';
import { CommonModule } from '@angular/common';
import { AdminSeriesComponent } from './admin-series/admin-series.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css'],
  imports: [
    RouterModule,
    MatTabsModule,
    CommonModule,
    AdminSeriesComponent
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
}
