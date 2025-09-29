import { Routes } from '@angular/router';
import { BookFormComponent } from './components/book-form/book-form.component';
import { BookListComponent } from './components/book-list/book-list.component';
import { BookDetailsComponent } from './components/book-details/book-details.component';
import { BookViewerComponent } from './components/book-viewer/book-viewer.component';
import { AdminComponent } from './admin/admin.component';
import { LoginComponent } from './components/login/login.component';
import { MyProfileComponent } from './components/my-profile/my-profile.component';
import { AcceptableUsePolicyComponent } from './components/acceptable-use-policy/acceptable-use-policy.component';
import { AdminGuard } from './utilities/admin.guard';
import { AupGuard } from './utilities/aup.guard';
import { OwnerGuard } from './utilities/owner.guard';
import { ReportAbuseComponent } from './components/report-abuse/report-abuse.component';
import { SignupComponent } from './components/signup/signup.component';
import { AuthGuard } from './utilities/auth.guard';
import { RemovedGuard } from './utilities/removed.guard';
import { AccountUsersComponent } from './owner/account-users/account-users.component';
import { GoogleDriveComponent } from './owner/integrations/google-drive/google-drive.component';
import { BulkImportComponent } from './owner/bulk-import/bulk-import.component';
import { CompleteInviteComponent } from './components/complete-invite/complete-invite.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';

export const routes: Routes = [
  { path: '', component: BookListComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'login', component: LoginComponent},
  { path: 'create', component: BookFormComponent, canActivate: [AupGuard, AuthGuard, OwnerGuard] },
  { path: 'create/:id', component: BookFormComponent, canActivate: [AupGuard, AuthGuard, OwnerGuard] },
  { path: 'book/:id', component: BookDetailsComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'admin', component: AdminComponent, canActivate: [AdminGuard, AupGuard, AuthGuard] },
  { path: 'profile', component: MyProfileComponent },
  { path: 'book-viewer/:fileId', component: BookViewerComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'aup', component: AcceptableUsePolicyComponent},
  { path: 'report-abuse', component: ReportAbuseComponent},
  { path: 'signup', component: SignupComponent, canActivate: [RemovedGuard]},
  { path: 'account', canActivate: [OwnerGuard, AupGuard, AuthGuard], 
    children: [
      { path: 'users', component: AccountUsersComponent },
      { path: 'integrations', component: GoogleDriveComponent },
      { path: 'bulk-import', component: BulkImportComponent },
      { path: '', redirectTo: 'users', pathMatch: 'full' }
    ]
  },
  { path: 'invite/:token', component: CompleteInviteComponent },
  { path: 'reset/:token', component: CompleteInviteComponent },
  { path:'reset-password', component: ResetPasswordComponent }
];
