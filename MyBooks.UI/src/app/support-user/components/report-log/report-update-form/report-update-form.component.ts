import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, Router } from '@angular/router';
import { SupportUserService, ReportLog, UpdateReportLogDto } from '../../../../services/support-user.service';
import { ToastService } from '../../../../services/toast.service';
import { ConfirmDialogComponent } from '../../../../components/shared/confirmation.component';
import { MatDialog } from '@angular/material/dialog';

function dateNotInFutureValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const selected = new Date(control.value);
    const today = new Date();
    today.setHours(0,0,0,0); // strip time
    return selected > today ? { futureDate: true } : null;
  };
}

function dateClosedStatusValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const status = group.get('status')?.value;
    const dateClosed = group.get('dateClosed')?.value;

    if (!dateClosed) return null; // nothing to validate if empty

    if (status !== 'Closed' && status !== 'Reopened') {
      return { invalidDateClosed: true };
    }
    return null;
  };
}

@Component({
  selector: 'app-report-update-form',
  standalone: true,
  templateUrl: './report-update-form.component.html',
  styleUrls: ['./report-update-form.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    ConfirmDialogComponent
  ]
})
export class ReportUpdateFormComponent implements OnInit {
  report?: ReportLog;
  form!: FormGroup;
  statusOptions = SupportUserService.statusOptions;
  resolutionOptions = SupportUserService.resolutionOptions;
  today = new Date();

  constructor(
    private fb: FormBuilder,
    private service: SupportUserService,
    private toast: ToastService,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.service.getReportById(+id).subscribe({
        next: (report) => {
          this.report = report;
          this.initForm(report);
        },
        error: () => this.toast.show('Failed to load report.')
      });
    }
  }

  private initForm(report: ReportLog): void {
    const normalizedDateClosed = report.dateClosed
    ? new Date(report.dateClosed).toISOString().split('T')[0] // yyyy-MM-dd
    : '';
    let prevStatus = report.status; 
    let prevResolution = report.resolution;

    this.form = this.fb.group({
      status: [report.status || '', Validators.required],
      resolution: [report.resolution || ''],
      resolutionNotes: [report.resolutionNotes || ''],
      reviewNotes: [report.reviewNotes || ''],
      dateClosed: [normalizedDateClosed || '', dateNotInFutureValidator()],
      targetType: [report.targetType || ''],
      targetId: [report.targetId || null],
      targetCreatedBy: [report.targetCreatedBy || '']
    }, { validators: dateClosedStatusValidator() });

    // set initial resolution availability
    const resCtrl = this.form.get('resolution');
    if(report.status !== 'Closed') {
      resCtrl?.disable();
    } else {
      resCtrl?.enable();
    }

    this.form.get('status')?.valueChanges.subscribe(status => {
      const notesCtrl = this.form.get('resolutionNotes');
      const dateCtrl = this.form.get('dateClosed');

      // confirm first if status change from Closed
      if (prevStatus === 'Closed' && status !== 'Reopened' && status !== 'Closed') {
        const dialogRef = this.dialog.open(ConfirmDialogComponent, {
          data: {
            title: 'Change Closed Report Status',
            itemType: 'Report Log',
            itemSpecific: `#${this.report?.id}`,
            message: `This report is currently <strong>Closed</strong>. Are you sure you want to change it to <strong>${status}</strong>?`,
            confirmText: `Change to ${status}`,
            permanent: false
          }
        });

        dialogRef.afterClosed().subscribe(result => {
          if(result) {
            dateCtrl?.setValue(null);
            this.report!.status = status;
          } else {
            this.form.get('status')?.setValue(prevStatus, { emitEvent: false });
            this.form.get('resolution')?.setValue(prevResolution, { emitEvent: false });
          }
        })
      }

      if (status === 'Closed') {
        resCtrl?.enable();
        resCtrl?.setValidators([Validators.required]);
        notesCtrl?.setValidators([Validators.required, Validators.minLength(3)]);
        if (!dateCtrl?.value) {
          dateCtrl?.setValue(new Date().toISOString().split('T')[0]);
        }
      } else {
        resCtrl?.setValue(null);
        resCtrl?.disable();
        resCtrl?.clearValidators();
        notesCtrl?.clearValidators();
        dateCtrl?.setValue(null);
      }

      resCtrl?.updateValueAndValidity();
      notesCtrl?.updateValueAndValidity();
      dateCtrl?.updateValueAndValidity();
    });
  }

  save(): void {
    if (this.form.invalid || !this.report) {
      this.toast.show('Please fix form errors before saving.');
      return;
    }

    const dto: UpdateReportLogDto = this.form.value;
    this.service.updateReport(this.report.id, dto).subscribe({
      next: () => {
        this.toast.show('Report updated successfully.');
        this.router.navigate(['/support/report-logs']);
      },
      error: () => this.toast.show('Failed to update report.')
    });
  }
}
