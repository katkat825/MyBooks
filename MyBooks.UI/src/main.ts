import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { provideHttpClient } from '@angular/common/http';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes), // ✅ This replaces RouterModule.forRoot()
    provideHttpClient()    // ✅ This replaces HttpClientModule
  ]
}).catch(err => console.error(err));
