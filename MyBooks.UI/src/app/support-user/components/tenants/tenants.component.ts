import { Component, OnInit } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CommonModule } from '@angular/common';
import { SupportUserService } from '../../../services/support-user.service';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { GlobalLoadingService } from '../../../services/global-loading.service';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    CommonModule,
    RouterOutlet
  ],
  templateUrl: './tenants.component.html',
  styleUrls: ['./tenants.component.css']
})

export class TenantsComponent implements OnInit {
  tenants: any[] = [];
  displayedColumns: string[] = ['id', 'name', 'billingPlan', 'isActive', 'createdDate', 'actions'];

  constructor(
    private supportService: SupportUserService,
    private router: Router,
    private globalLoading: GlobalLoadingService
  ) {}

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.globalLoading.show("Loading Tenants...");
    this.supportService.getAllTenants().subscribe({
      next: data => {
        this.tenants = data.$values;
        this.globalLoading.hide();
      },
      error: () => {
        this.globalLoading.hide();
        alert("Error loading tenants");
      }
    });
  }

  createAccount(): void {
    this.router.navigate(['/support/tenants/new']);
  }

  jumpIntoAccount(tenantId: number): void {
    this.supportService.impersonateAccount(tenantId).subscribe({
      next: response => {
        localStorage.setItem('token', response.token);
        localStorage.setItem('impersonationLogId', response.logId.toString());
        window.location.href = '/';
      }
    })
  }
}
