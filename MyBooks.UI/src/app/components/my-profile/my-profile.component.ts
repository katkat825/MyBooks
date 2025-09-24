import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { UserService } from '../../services/user.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-my-profile',
  standalone: true,
  templateUrl: './my-profile.component.html',
  styleUrls: ['./my-profile.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule
  ]
})
export class MyProfileComponent implements OnInit {
  accountForm!: FormGroup;
  loading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private router: Router,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.accountForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: [''] 
    });

    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.accountForm.patchValue({
          firstName: profile.firstName,
          lastName: profile.lastName,
          email: profile.email
        });
      },
      error: (error) => {
        this.errorMessage = 'Failed to load profile.';
        console.error('Error loading profile', error);
      }
    });
  }

  onSubmit() {
    if (this.accountForm.invalid) {
      return;
    }
    this.loading = true;

    // Build update DTO. Only include password if the field is filled.
    const dto: any = {
      firstName: this.accountForm.value.firstName,
      lastName: this.accountForm.value.lastName,
      email: this.accountForm.value.email
    };
    if (this.accountForm.value.password) {
      dto.password = this.accountForm.value.password;
    }

    this.userService.updateProfile(dto).subscribe({
      next: () => {
        this.loading = false;
        this.snackBar.open('Profile updated successfully', 'Close', { duration: 3000 });
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = 'Failed to update profile.';
        console.error('Error updating profile', error);
      }
    });
  }
}
