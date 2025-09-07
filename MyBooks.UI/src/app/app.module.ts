import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { routes } from './app.routes';
import { AppComponent } from './app.component';
import { BookListComponent } from './components/book-list/book-list.component';
import { BookDetailsComponent } from './components/book-details/book-details.component';
import { BookFormComponent } from './components/book-form/book-form.component';
import { HttpClientModule } from '@angular/common/http';
import { AdminComponent } from './admin/admin.component';
import { AdminGenresComponent } from './admin/admin-genres/admin-genres.component';
import { LoginComponent } from './components/login/login.component';
import { AdminUsersComponent } from './admin/admin-users/admin-users.component';
import { AccountSettingsComponent } from './components/account-settings/account-settings.component';
import { BookViewerComponent } from './components/book-viewer/book-viewer.component';
import { AcceptableUsePolicyComponent } from './components/acceptable-use-policy/acceptable-use-policy.component';
import { ReportAbuseComponent } from './components/report-abuse/report-abuse.component';
import { SignupComponent } from './components/signup/signup.component';
import { AcountComponent } from './owner/acount/acount.component';

@NgModule({
  imports: [
    BrowserModule,
    routes,
    AppComponent,
    BookListComponent,
    BookDetailsComponent,
    BookFormComponent,
    RouterModule.forRoot(routes),
  ],
  providers: [],
  bootstrap: [AppComponent],
  declarations: [
    AdminComponent,
    AdminGenresComponent,
    LoginComponent,
    AdminUsersComponent,
    AccountSettingsComponent,
    BookViewerComponent,
    AcceptableUsePolicyComponent,
    ReportAbuseComponent,
    SignupComponent,
    AcountComponent
  ]
})
export class AppModule { }
