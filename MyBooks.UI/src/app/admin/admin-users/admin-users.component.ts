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

@Component({
  selector: 'app-admin-users',
  standalone: true,
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.css'],
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
    ReactiveFormsModule]
})
export class AdminUsersComponent {
  users: any[] = [];
  userForm!: FormGroup;
  showAddUserForm = false;
  editingUserId: number | null = null;
  editingField: string | null = null;
  ageCategories: any[] = [];
  roles: string[] = ['Admin', 'Editor', 'User'];
  showInactiveUsers = false;
  currentUserId: number | null = null;

  constructor(
    private userService: UserService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.userForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      role: ['', Validators.required],
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
    })
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
      if (confirm(`Are you sure you want to deactivate ${user.firstName}?`)) {
        this.userService.deactivateUser(user.id).subscribe(() => this.loadUsers());
      }
    } else {
      if (confirm(`Are you sure you want to reactivate ${user.firstName}?`)) {
        this.userService.reactivateUser(user.id).subscribe(() => this.loadUsers());
      }
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

    if (confirm(`Are you sure you want to delete ${user.firstName}?`)) {
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

    this.userService.createUser(this.userForm.value).subscribe({
      next: () => {
        this.userForm.reset();
        this.showAddUserForm = false;
        this.showSavedSnack();
        this.loadUsers();
      },
      error: (error) => {
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
