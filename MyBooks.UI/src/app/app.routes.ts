import { Routes } from '@angular/router';
import { BookFormComponent } from './components/book-form/book-form.component';
import { BookListComponent } from './components/book-list/book-list.component';
import { BookDetailsComponent } from './components/book-details/book-details.component';
import { BookViewerComponent } from './components/book-viewer/book-viewer.component';
import { AdminComponent } from './admin/admin.component';
import { LoginComponent } from './components/login/login.component';
import { AccountSettingsComponent } from './components/account-settings/account-settings.component';
import { AcceptableUsePolicyComponent } from './components/acceptable-use-policy/acceptable-use-policy.component';
import { AdminGuard } from './utilities/admin.guard';
import { AupGuard } from './utilities/aup.guard';
import { ReportAbuseComponent } from './components/report-abuse/report-abuse.component';
import { SignupComponent } from './components/signup/signup.component';
import { AuthGuard } from './utilities/auth.guard';

export const routes: Routes = [
  { path: '', component: BookListComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'login', component: LoginComponent},
  { path: 'create', component: BookFormComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'create/:id', component: BookFormComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'book/:id', component: BookDetailsComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'admin', component: AdminComponent, canActivate: [AdminGuard, AupGuard, AuthGuard] },
  { path: 'account', component: AccountSettingsComponent },
  { path: 'book-viewer/:fileId', component: BookViewerComponent, canActivate: [AupGuard, AuthGuard] },
  { path: 'aup', component: AcceptableUsePolicyComponent},
  { path: 'report-abuse', component: ReportAbuseComponent},
  { path: 'signup', component: SignupComponent}
];
