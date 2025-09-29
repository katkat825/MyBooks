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
import { AdminUsersComponent } from './owner/account/admin-users/account-users.component';
import { AccountSettingsComponent } from './components/account-settings/account-settings.component';
import { BookViewerComponent } from './components/book-viewer/book-viewer.component';
import { AcceptableUsePolicyComponent } from './components/acceptable-use-policy/acceptable-use-policy.component';
import { ReportAbuseComponent } from './components/report-abuse/report-abuse.component';
import { SignupComponent } from './components/signup/signup.component';
import { AcountComponent } from './owner/account/account.component';
import { GoogleDriveComponent } from './owner/integrations/google-drive/google-drive.component';
import { GoogleDriveFolderComponentComponent } from './owner/integrations/google-drive-folder.component/google-drive-folder.component.component';
import { GoogleDriveFolderComponent } from './owner/integrations/google-drive-folder/google-drive-folder.component';
import { AddGoogleDriveFolderComponent } from './owner/integrations/add-google-drive-folder/add-google-drive-folder.component';
import { BulkImportComponentComponent } from './owner/bulk-import.component/bulk-import.component.component';
import { BulkImportComponent } from './owner/bulk-import/bulk-import.component';
import { BulkImportTableComponent } from './owner/bulk-import/bulk-import-table/bulk-import-table.component';
import { CompleteInviteComponent } from './components/complete-invite/complete-invite.component';

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
    AcountComponent,
    GoogleDriveComponent,
    GoogleDriveFolderComponentComponent,
    GoogleDriveFolderComponent,
    AddGoogleDriveFolderComponent,
    BulkImportComponentComponent,
    BulkImportComponent,
    BulkImportTableComponent,
    CompleteInviteComponent
  ]
})
export class AppModule { }
