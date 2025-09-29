import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { InviteService } from '../../services/invite.service';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css'],
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule]
})
export class ResetPasswordComponent {
  form: FormGroup;
  message = '';
  errorMessage = '';
  loading = false;

  constructor(private fb: FormBuilder, private inviteService: InviteService) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.message = '';
    this.errorMessage = '';

    this.inviteService.resend(this.form.get('email')?.value).subscribe({
      next: () => {
        this.message = 'If this email exists, a reset link has been sent.';
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err.error || 'Failed to request password reset.';
        this.loading = false;
      }
    });
  }
}
