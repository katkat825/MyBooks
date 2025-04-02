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

export const routes: Routes = [
  { path: '', component: BookListComponent, canActivate: [AupGuard] },
  { path: 'login', component: LoginComponent},
  { path: 'create', component: BookFormComponent, canActivate: [AupGuard] },
  { path: 'create/:id', component: BookFormComponent, canActivate: [AupGuard] },
  { path: 'book/:id', component: BookDetailsComponent, canActivate: [AupGuard] },
  { path: 'admin', component: AdminComponent, canActivate: [AdminGuard, AupGuard] },
  { path: 'account', component: AccountSettingsComponent },
  { path: 'book-viewer/:fileId', component: BookViewerComponent, canActivate: [AupGuard] },
  { path: 'aup', component: AcceptableUsePolicyComponent},
];
