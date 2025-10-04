import { Routes } from '@angular/router';
import { BookFormComponent } from './components/book-form/book-form.component';
import { BookListComponent } from './components/book-list/book-list.component';
import { BookDetailsComponent } from './components/book-details/book-details.component';
import { BookViewerComponent } from './components/book-viewer/book-viewer.component';
import { LoginComponent } from './components/login/login.component';
import { MyProfileComponent } from './components/my-profile/my-profile.component';
import { AcceptableUsePolicyComponent } from './components/acceptable-use-policy/acceptable-use-policy.component';
import { AupGuard } from './utilities/aup.guard';
import { OwnerGuard } from './utilities/owner.guard';
import { ReportAbuseComponent } from './components/report-abuse/report-abuse.component';
import { SignupComponent } from './support-user/components/signup/signup.component';
import { AuthGuard } from './utilities/auth.guard';
import { SupportUserGuard } from './utilities/support-user.guard';
import { AccountUsersComponent } from './owner/account-users/account-users.component';
import { GoogleDriveComponent } from './owner/integrations/google-drive/google-drive.component';
import { BulkImportComponent } from './owner/bulk-import/bulk-import.component';
import { CompleteInviteComponent } from './components/complete-invite/complete-invite.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { SupportLayoutComponent } from './support-user/support-layout/support-layout.component';
import { SupportHomeComponent } from './support-user/support-home/support-home.component';
import { TenantsComponent } from './support-user/components/tenants/tenants.component';
import { SupportUsersComponent } from './support-user/components/users/users.component';
import { SupportBooksComponent } from './support-user/components/support-books/support-books.component';
import { BookService } from './services/book.service';
import { SupportUserService } from './services/support-user.service';
import { ReportListComponent } from './support-user/components/report-log/report-list/report-list.component';
import { ReportCreateFormComponent } from './support-user/components/report-log/report-create-form/report-create-form.component';
import { ReportUpdateFormComponent } from './support-user/components/report-log/report-update-form/report-update-form.component';
import { AdminComponent } from './owner/admin/admin.component';
import { ContentReviewComponent } from './support-user/components/content-review/content-review.component';
import { GlobalReviewerGuard } from './utilities/global-reviewer.guard';

export const routes: Routes = [
  { path: '', component: BookListComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'login', component: LoginComponent},
  { path: 'create', component: BookFormComponent, canActivate: [AupGuard, AuthGuard, OwnerGuard] },
  { path: 'create/:id', component: BookFormComponent, canActivate: [AupGuard, AuthGuard, OwnerGuard] },
  { path: 'book/:id', component: BookDetailsComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'profile', component: MyProfileComponent, canActivate:[AuthGuard] },
  { path: 'book-viewer/:fileId', component: BookViewerComponent, canActivate: [AupGuard, AuthGuard], providers: [{ provide: 'ViewerService', useClass: BookService }] },
  { path: 'aup', component: AcceptableUsePolicyComponent},
  { path: 'report-abuse', component: ReportAbuseComponent},
  { path: 'account', canActivate: [OwnerGuard, AupGuard, AuthGuard], 
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      { path: 'users', component: AccountUsersComponent },
      { path: 'integrations', component: GoogleDriveComponent },
      { path: 'bulk-import', component: BulkImportComponent },
      { path: 'genres-series', component: AdminComponent },
    ]
  },
  { path: 'invite/:token', component: CompleteInviteComponent },
  { path: 'reset/:token', component: CompleteInviteComponent },
  { path:'reset-password', component: ResetPasswordComponent },
  { path: 'support/book-viewer/:fileId', component: BookViewerComponent, canActivate: [SupportUserGuard], providers: [{ provide: 'ViewerService', useClass: SupportUserService }] },
  { path: 'support', component: SupportLayoutComponent, canActivate: [SupportUserGuard],
    children: [
      { path: '', component: SupportHomeComponent },
      { path: 'tenants', component: TenantsComponent }, 
      { path: 'tenants/new', component: SignupComponent },
      { path: 'users', component: SupportUsersComponent },
      { path: 'books', component: SupportBooksComponent },
      { path: 'report-logs', component: ReportListComponent },
      { path: 'report-logs/new', component: ReportCreateFormComponent },
      { path: 'report-logs/update/:id', component: ReportUpdateFormComponent }
    ]
  },
  { path: 'global/content-review', component: ContentReviewComponent, canActivate: [GlobalReviewerGuard]}
];
