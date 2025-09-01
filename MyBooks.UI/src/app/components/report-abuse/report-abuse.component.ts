import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

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
    private http: HttpClient,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    const url = this.route.snapshot.queryParamMap.get('url') || '';
    this.form = this.fb.group({
      pageUrl: [window.location.href],
      description: ['', Validators.required],
      contactEmail: ['']
    });
  }

  submitReport() {
    if (this.form.invalid) return;

    this.isSubmitting = true;
    const token = localStorage.getItem('token');
    const headers = { Authorization: `Bearer ${token}` };

    this.http.post('/api/email/reportabuse', this.form.value, { headers }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.form.reset({ pageUrl: window.location.href });
        alert('Report submitted successfully.');
      },
      error: () => {
        this.isSubmitting = false;
        alert('Failed to submit report.');
      }
    });
  }
}
