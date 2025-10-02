import { Component, OnInit } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CommonModule } from '@angular/common';
import { SupportUserService } from '../../../services/support-user.service';
import { Router } from '@angular/router';
import { GlobalLoadingService } from '../../../services/global-loading.service';
import { ConfirmDialogComponent } from '../../../components/shared/confirmation.component';
import { MatDialog } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ToastService } from '../../../services/toast.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatSlideToggleModule,
    CommonModule,
    FormsModule
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
    private globalLoading: GlobalLoadingService,
    private toast: ToastService,
    private dialog: MatDialog,
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
        this.toast.show("Error loading tenants");
      }
    });
  }

  createAccount(): void {
    this.router.navigate(['/support/tenants/new']);
  }

  toggleActive(tenant: any): void {
    const newValue = !tenant.isActive;
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Change Active Status',
        itemType: 'Tenant',
        itemSpecific: `${tenant.id}`,
        message: `Are you sure you want to change the active status for ${tenant.id}?`,
        confirmText: 'Change It',
        permenant: false
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if(result) {
        this.supportService.toggleTenantActiveStatus(tenant.id, newValue).subscribe({
          next: () => {
            tenant.isActive = newValue;
            this.toast.show('Tenant updated successfully');
          },
          error: err => {
            console.error('Failed to update tenant status', err);
            this.toast.show('Failed to update tenant status');
          }
        });
      } else {
        this.loadTenants();
      }
    });
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
