import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SupportUserService, CreateReportLogDto } from '../../../../services/support-user.service';
import { GlobalLoadingService } from '../../../../services/global-loading.service';
import { ToastService } from '../../../../services/toast.service';
import { CommonModule } from '@angular/common';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-report-create-form',
  standalone: true,
  templateUrl: './report-create-form.component.html',
  styleUrls: ['./report-create-form.component.css'],
  imports: [
    CommonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    ReactiveFormsModule
  ]
})
export class ReportCreateFormComponent implements OnInit {
  form!: FormGroup;

  statusOptions = SupportUserService.statusOptions;
  reportTypes = SupportUserService.reportTypes;
  today = new Date();

  constructor(
    private fb: FormBuilder,
    private supportService: SupportUserService,
    private globalLoading: GlobalLoadingService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      dateReceived: [new Date(), Validators.required],
      status: ['Open', Validators.required],
      reportedBy: ['', Validators.required],
      reportType: ['Abuse', Validators.required],
      description: ['', Validators.required],
      targetType: [''],
      targetId: [''],
      targetCreatedBy: ['']
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.toast.show('Please complete all required fields');
      return;
    }

    this.globalLoading.show('Creating report...');

    const dto: CreateReportLogDto = {
      ...this.form.value,
      status: this.form.value.status ? this.form.value.status : 'Open',
      dateReceived: this.form.value.dateReceived
        ? new Date(this.form.value.dateReceived).toISOString()
        : new Date().toISOString(),
      targetId: this.form.value.targetId ? Number(this.form.value.targetId) : null
    };

    this.supportService.createReport(dto).subscribe({
      next: () => {
        this.globalLoading.hide();
        this.toast.show('Report created successfully');
        this.form.reset({
          dateReceived: new Date(),
          status: 'Open',
          reportType: 'Abuse'
        });
      },
      error: () => {
        this.globalLoading.hide();
        this.toast.show('Failed to create report');
      }
    });
  }
}
