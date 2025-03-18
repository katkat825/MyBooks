import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css',
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    CommonModule,
    MatTableModule,
    MatIconModule,
    ReactiveFormsModule]
})
export class AdminUsersComponent {
  users: any[] = [];
  editingUserId: number | null = null;
  editingField: string | null = null;
  ageCategories: any[] = [];
  roles: string[] = ['Admin', 'Editor', 'User'];

  constructor(private userService: UserService, private fb: FormBuilder) { }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers() {
    this.userService.getUsers().subscribe({
      next: (data) => this.users = data,
      error: (error) => console.error('Error fetching users: ', error)
    });
  }

  startEdit(userId: number, field: string) {
    this.editingUserId = userId;
    this.editingField = field;
  }

  saveEdit(user: any, field: string, event: Event) {
    const target = event.target as HTMLInputElement | HTMLSelectElement;
    const newValue = target.value.trim();

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

  deleteUser(id: number) {
    if (confirm('Are you sure you want to delete this user?')) {
      this.userService.deleteUser(id).subscribe({
        next: () => this.loadUsers(),
        error: (error) => {
          console.error("error deleting user: ", error)
        }
      });
    }
  }
}
