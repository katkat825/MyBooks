import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SupportUserService } from '../../../services/support-user.service';
import { GlobalLoadingService } from '../../../services/global-loading.service';
import { MatDialog } from '@angular/material/dialog';
import { ToastService } from '../../../services/toast.service';
import { GlobalReviewerDialogComponent } from './global-reviewer-dialog.component';

@Component({
  selector: 'app-support-users',
  standalone: true,
  imports: [
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    CommonModule,
    GlobalReviewerDialogComponent
  ],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class SupportUsersComponent implements OnInit {
  users: any[] = [];
  displayedColumns: string[] = ['id', 'firstName', 'lastName', 'email', 'role', 'isActive', 'actions'];

  constructor(
    private supportService: SupportUserService,
    private globalLoading: GlobalLoadingService,
    private cd: ChangeDetectorRef,
    private dialog: MatDialog,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.globalLoading.show("Loading Users...");
    this.supportService.getAllUsers().subscribe({
      next: data => {
        console.log('support users response in component:', data);
        this.users = data;
        this.cd.detectChanges();
        this.globalLoading.hide();
      },
      error: () => {
        this.globalLoading.hide();
        alert("Error loading users");
      }
    });
  }

  impersonateUser(userId: number): void {
    this.supportService.impersonate(userId).subscribe({
      next: response => {
        localStorage.setItem('token', response.token);
        localStorage.setItem('impersonationLogId', response.logId.toString());
        window.location.href = '/';
      }
    });
  }

  openGlobalReviewerDialog(user: any): void {
    const dialogRef = this.dialog.open(GlobalReviewerDialogComponent, {
      width: '400px',
      data: { userId: user.id, email: user.email }
    });

    dialogRef.afterClosed().subscribe((updated) => {
      if (updated !== undefined) {
        this.toast.show('Reviewer access updated');
      }
    });
  }
}
