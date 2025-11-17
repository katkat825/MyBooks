import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { EmailService } from '../../services/email.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-report-abuse',
  templateUrl: './report-abuse.component.html',
  styleUrls: ['./report-abuse.component.css'],
  standalone: true,
  imports: [
    ReactiveFormsModule,  
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    CommonModule
  ]
})
export class ReportAbuseComponent implements OnInit {
  form!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private emailService: EmailService,
    private toast: ToastService
  ) {}

  ngOnInit() {
    this.form = this.fb.group({
      description: ['', Validators.required],
      contactEmail: ['']
    });
  }

  submitReport() {
    if (this.form.invalid) return;

    this.isSubmitting = true;

    this.emailService.sendViolationReport(this.form.value).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toast.show('Report submitted successfully');
        this.form.reset()
      },
      error: () => {
        this.isSubmitting = false;
        alert('Failed to submit report.');
      }
    });
  }
}
