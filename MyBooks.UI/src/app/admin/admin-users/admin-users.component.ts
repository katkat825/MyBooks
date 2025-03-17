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
  editForm!: FormGroup;
  editingUser: any = null;
  createForm!: FormGroup;
  addingUser: boolean = false;

  constructor(private userService: UserService, private fb: FormBuilder) { }

  ngOnInit(): void {
    this.loadUsers();
    this.editForm = this.fb.group({
      fname: ['', Validators.required],
      lname: ['', Validators.required],
      email: ['', Validators.required, Validators.email],
      password: ['', Validators.required],
      role: ['', Validators.required],
      ageCategoryId: ['', Validators.required]
    });
    this.createForm = this.fb.group({
      fname: ['', Validators.required],
      lname: ['', Validators.required],
      email: ['', Validators.required, Validators.email],
      password: ['', Validators.required],
      role: ['', Validators.required],
      ageCategoryId: ['', Validators.required]
    });
  }

  loadUsers() {
    this.userService.getUsers().subscribe({
      next: (data) => this.users = data,
      error: (error) => console.error('Error fetching users: ', error)
    });
  }

  startEdit(user: any) {
    this.editingUser = user;
    this.editForm.patchValue({
      email: user.email,
      password: user.password
    });
  }

  cancelEdit() {
    this.editingUser = null;
    this.editForm.reset();
  }

  saveEdit() {
    if (!this.editingUser) return;

    const updatedUser = {
      ...this.editingUser,
      email: this.editForm.value.email,
      password: this.editForm.value.password
    };

    this.userService.updateUser(updatedUser.id, updatedUser).subscribe({
      next: () => {
        this.loadUsers();
        this.cancelEdit();
      },
      error: (error) => {
        console.error("error updating user: ", error);
        alert("Failed to update user.");
      }
    });
  }

  cancelCreate() {
    this.addingUser = false;
    this.createForm.reset();
  }

  addUser() {
    this.addingUser = true;
    this.createForm.reset();
  }

  saveCreate() {
    if (this.createForm.invalid) {
      alert("Please fill in all required fields.");
      return;
    }

    const newUser = { email: this.createForm.value.email.trim() };

    this.userService.createUser(newUser).subscribe({
      next: (createdUser) => {
        this.users.push(createdUser);
        this.addingUser = false;
        this.createForm.reset();
        this.loadUsers();
      },
      error: (error) => {
        console.error("error adding user: ", error);
        alert("Failed to add user.");
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
