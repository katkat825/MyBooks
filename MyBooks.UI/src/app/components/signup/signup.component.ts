import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SignupRequest, SignupResponse, SignupService } from '../../services/signup.service';
import { AbstractControl, AsyncValidatorFn } from '@angular/forms';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReactiveFormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { NgIf } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    NgIf
  ],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.css'],
})
export class SignupComponent {
  isSubmitting = false;
  errorMessage: string | null = null;
  form!: FormGroup;
  
  constructor(
    private fb: FormBuilder, 
    private signupService: SignupService,
    private router: Router) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required]
    });
  }

  submit(){
    if (this.form.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = null;

    const payload: SignupRequest = {
      ...this.form.value,
      billingPlanId: 1
    }

    this.signupService.createTenant(payload).subscribe({
      next: (resp: SignupResponse) => {
        this.isSubmitting = false;
        this.errorMessage = null;
        console.log("tenant created successfully: " + resp.tenantId);
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Signup failed';
        this.isSubmitting = false;
      }
    })
  }
}
