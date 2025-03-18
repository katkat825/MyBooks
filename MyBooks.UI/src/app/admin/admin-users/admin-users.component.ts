import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
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

  constructor(private userService: UserService, private fb: FormBuilder) { }

  ngOnInit(): void {
    this.userForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      role: ['', Validators.required],
      ageCategoryId: ['', Validators.required]
    });
    this.loadAgeCategories();
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
    if (user.isActive) {
      if (confirm('Are you sure you want to deactivate ${user.firstname}?')) {
        this.userService.deactivateUser(user.id).subscribe(() => this.loadUsers());
      }
    } else {
      if (confirm('Are you sure you want to reactivate ${user.firstname}?')) {
        this.userService.reactivateUser(user.id).subscribe(() => this.loadUsers());
      }
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
        }));
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
        this.loadUsers();
      },
      error: (error) => {
        console.error('error updating user: ', error);
        alert('Failed to update user.');
      }
    })
  }
}
