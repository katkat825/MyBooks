import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { UserService } from '../../services/user.service';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { GlobalLoadingService } from '../../services/global-loading.service';
import { emailExistsValidator } from '../../validators/email-exists.validator';


@Component({
  selector: 'app-account-users',
  standalone: true,
  templateUrl: './account-users.component.html',
  styleUrls: ['./account-users.component.css'],
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    CommonModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatSelectModule,
    MatCardModule,
    MatTooltipModule,
    MatSnackBarModule,
    ReactiveFormsModule,
    MatDialogModule
  ]
})
export class AccountUsersComponent {
  users: any[] = [];
  userForm!: FormGroup;
  showAddUserForm = false;
  editingUserId: number | null = null;
  editingField: string | null = null;
  ageCategories: any[] = [];
  //roles: string[] = ['Admin', 'Editor', 'User'];
  showInactiveUsers = false;
  currentUserId: number | null = null;
  userUsageStatus!: { activeCount: number; maxCount: number };
  canAddUser: boolean = false;

  constructor(
    private userService: UserService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private globalLoading: GlobalLoadingService
  ) { }

  ngOnInit(): void {
    this.userForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: this.fb.control('', {
        validators: [Validators.required, Validators.email],
        asyncValidators: [emailExistsValidator(this.userService)],
        updateOn: 'blur'
      }),
      role: ['User'],
      ageCategoryId: ['', Validators.required]
    });

    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.currentUserId = profile.id;
        this.loadAgeCategories();
      },
      error: (err) => {
        console.error("Error fetching profile: ", err);
      }
    });

    this.userService.getUserUsageCounts().subscribe({
      next: (status)  => {
        this.userUsageStatus = status;
        this.canAddUser = status.activeCount < status.maxCount;
      },
      error: (err) => {
        console.error("Error loading user usage status: ", err)
      }
    });
  }

  toggleAddUserForm() {
    this.showAddUserForm = !this.showAddUserForm;
    if (!this.showAddUserForm)
      this.userForm.reset();
  }

  toggleInactiveUsers() {
    this.showInactiveUsers = !this.showInactiveUsers;
    this.loadUsers();
  }

  toggleUserStatus(user: any) {
    if (this.currentUserId && user.id === this.currentUserId) {
      alert('You cannot deactivate your own account.');
      return;
    }
    if (user.role === 'Owner' || user.role === 'SuperAdmin') {
      alert('You cannot deactivate the Owner or SuperAdmin account.');
      return;
    }
    if (user.isActive) {
      const dialogRef = this.dialog.open(ConfirmDialogComponent, {
        data: { 
          itemType: 'User', 
          itemSpecific: user.firstName,
          message: `Are you sure you want to deactivate ${user.firstName}?`,
          confirmText: 'Deactivate',
          title: "Deactivate User",
          permanent: false
        }
      });

      dialogRef.afterClosed().subscribe((result) => {
        if(result)
          this.userService.deactivateUser(user.id).subscribe(() => this.loadUsers());
      });
    } 
    
    else {
      const dialogRef = this.dialog.open(ConfirmDialogComponent, {
        data: {
          itemType: 'User',
          itemSpecific: user.firstName,
          message: `Are you sure you want to reactivate ${user.firstName}?`,
          confirmText: 'Reactivate',
          title: "Reactivate User",
          permanent: false
        }
      });

      dialogRef.afterClosed().subscribe((result) => {
        if(result)
          this.userService.reactivateUser(user.id).subscribe(() => this.loadUsers());
      });
    }
  }

  deleteUser(user: any) {
    if (this.currentUserId && user.id === this.currentUserId) {
      alert('You cannot delete your own account.');
      return;
    }
    if (user.role === 'Owner' || user.role === 'SuperAdmin') {
      alert('You cannot delete the Owner or SuperAdmin account.');
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { itemType: 'User', itemSpecific: user.firstName }
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.userService.deleteUser(user.id).subscribe({
          next: () => {
            this.showSavedSnack();
            this.loadUsers();
          },
          error: (err) => {
            console.error("Error deleting user: ", err);
            alert("Failed to delete user.");
          }
        });
      }
    })
  }

  loadUsers() {
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data
          .filter(user => this.showInactiveUsers || user.isActive)
          .map(user => ({
          ...user,
          ageCategoryName: this.getAgeCategoryName(user.ageCategoryId),
          isInactive: !user.isActive
          }))
          .sort((a, b) => a.firstName.localeCompare(b.firstName)); 
        },
      error: (error) => console.error('Error fetching users: ', error)
    });
  }

  loadAgeCategories() {
    this.userService.getAgeCategories().subscribe({
      next: (data) => {
        this.ageCategories = data;
        this.loadUsers();
      },
      error: (error) => console.error("error fetching age categories: ", error)
    });
  }

  getAgeCategoryName(ageCategoryId: number): string {
    const category = this.ageCategories.find(cat => cat.id === ageCategoryId);
    return category ? category.name : 'Unknown';
  }

  addUser() {
    if (this.userForm.invalid) return;

    this.globalLoading.show("Please wait while we create this user...")

    this.userService.createUser(this.userForm.value).subscribe({
      next: () => {
        this.userForm.reset();
        this.showAddUserForm = false;
        this.showSavedSnack();
        this.globalLoading.hide();
        this.loadUsers();
      },
      error: (error) => {
        this.globalLoading.hide();
        console.error("error creating user: ", error);
        alert("Failed to create user");
      },
    });
  }

  startEdit(userId: number, field: string) {
    this.editingUserId = userId;
    this.editingField = field;
  }

  saveEdit(user: any, field: string, event: Event) {
    const target = event.target as HTMLInputElement | HTMLSelectElement;
    let newValue: any = target.value.trim();

    if (field === "ageCategoryId") {
      newValue = Number(newValue);
    }

    if(field === "email"){
      this.userService.checkEmailExists(newValue, user.id).subscribe({
        next: (res) => {
          if(res.exists) {
            alert(`The email address ${newValue} is already in use`);
            return;
          }
          this.applyUpdate(user, field, newValue);
        },
        error: (err) => {
          console.error("Error checking email: ", err);
          alert("Could not validate email address. Change not saved");
          return;
        }
      });
    } else {
      this.applyUpdate(user, field, newValue);
    }
  }

  private applyUpdate(user: any, field: string, newValue: any) {
    console.log(user.id, field, newValue);
    user[field] = newValue;
    const updates = { [field]: newValue };

    this.userService.updateUser(user.id, updates).subscribe({
      next: (response) => {
        console.log("user patch updated successfully:", response);
        this.editingUserId = null;
        this.editingField = null;
        this.showSavedSnack();
        this.loadUsers();
      },
      error: (error) => {
        console.error('error updating user: ', error);
        alert('Failed to update user.');
      }
    })
  }

  private showSavedSnack(){
    this.snackBar.open('Saved', 'Close', {
      duration: 2000,
      horizontalPosition: 'right',
      verticalPosition: 'bottom',
    });
  }
}
