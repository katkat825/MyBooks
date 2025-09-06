import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SignupRequest, SignupResponse, SignupService } from '../../services/signup.service';
import { TenantContextService } from '../../services/tenant-context.service';
import { AbstractControl, AsyncValidatorFn } from '@angular/forms';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-signup',
  standalone: false,
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.css'],
  imports: [MatProgressSpinnerModule]
})
export class SignupComponent {
  isSubmitting = false;
  errorMessage: string | null = null;
  form!: FormGroup;
  
  constructor(
    private fb: FormBuilder, 
    private signupService: SignupService,
    private tenantContextService: TenantContextService) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      subdomain: ['', [Validators.required, Validators.pattern('^[a-zA-Z0-9-]+$')], [this.subdomainAvailableValidator()]],
      ownerEmail: ['', [Validators.required, Validators.email]],
      ownerPassword: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required]
    });
  }

  subdomainAvailableValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      if (!control.value)
        return of(null);

      return this.tenantContextService.checkSubdomainAvailability(control.value).pipe(
        map(res => (res.available ? null : {subdomainTaken: true})),
        catchError(() => of(null)) // avoid blocking form if API errors
      )
    }
  }

  submit(){
    if (this.form.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = null;

    const payload: SignupRequest = this.form.value as SignupRequest;

    this.signupService.createTenant(payload).subscribe({
      next: (resp: SignupResponse) => {
        window.location.href = resp.portalUrl;
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Signup failed';
        this.isSubmitting = false;
      }
    })
  }
}
