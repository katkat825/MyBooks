import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { InviteService, CompleteInvitationDto } from '../../services/invite.service';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';


@Component({
  selector: 'app-complete-invite',
  standalone: true,
  templateUrl: './complete-invite.component.html',
  styleUrls: ['./complete-invite.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ]
})
export class CompleteInviteComponent implements OnInit {
  form!: FormGroup;
  token!: string;
  email = '';
  loading = true;
  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private inviteService: InviteService
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token') ?? '';

    if (!this.token) {
      this.errorMessage = 'Invalid invitation link.';
      this.loading = false;
      return;
    }

    this.form = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, {validators: this.passwordsMatch});

    this.inviteService.validate(this.token).subscribe({
      next: (info) => {
        this.email = info.email;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err.error || 'Invitation link is invalid or expired.';
        this.loading = false;
      }
    });
  }

  private passwordsMatch(group: FormGroup) {
    const pass = group.get('password')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return pass === confirm ? null : { mismatch: true };
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const dto: CompleteInvitationDto = {
      token: this.token,
      password: this.form.get('password')?.value
    };

    this.inviteService.complete(dto).subscribe({
      next: () => {
        alert('Password set. You can now log in.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.errorMessage = err.error || 'Failed to complete invitation.';
      }
    });
  }
}
