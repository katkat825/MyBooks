import { Routes } from '@angular/router';
import { BookFormComponent } from './components/book-form/book-form.component';
import { BookListComponent } from './components/book-list/book-list.component';
import { BookDetailsComponent } from './components/book-details/book-details.component';
import { AdminComponent } from './admin/admin.component';
import { LoginComponent } from './components/login/login.component';
import { AccountSettingsComponent } from './components/account-settings/account-settings.component';
import { AdminGuard } from './guards/admin.guard';

export const routes: Routes = [
  { path: '', component: BookListComponent },
  { path: 'login', component: LoginComponent },
  { path: 'create', component: BookFormComponent },
  { path: 'create/:id', component: BookFormComponent },
  { path: 'book/:id', component: BookDetailsComponent },
  { path: 'admin', component: AdminComponent, canActivate: [AdminGuard] },
  { path: 'account', component: AccountSettingsComponent }, 
];
