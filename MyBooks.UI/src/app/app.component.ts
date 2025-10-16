import { Component, Renderer2 } from '@angular/core';
import { RouterOutlet, RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from './services/user.service';
import { isTokenExpired } from './utilities/auth-utilities';
import { ViewChild } from '@angular/core';
import { ToastComponent } from './components/shared/toast.component';
import { ToastService } from './services/toast.service';
import { GlobalLoadingService } from './services/global-loading.service';
import { catchError, map, Observable, of } from 'rxjs';
import { SupportUserService } from './services/support-user.service';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  imports: [RouterOutlet,
    CommonModule,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    RouterModule,
    ToastComponent,
    MatProgressSpinnerModule,
    MatDividerModule
  ]
})
export class AppComponent {
  @ViewChild(ToastComponent) toast!: ToastComponent;

  isDarkTheme = false;
  isContrastTheme = false;
  isLightTheme = true;
  userRole: string = '';
  currentUrl = window.location.href;
  isSupportUser: boolean = false;
  canAccessGlobalReviewer = false;
  currentYear = new Date().getFullYear();

  globalLoading$: Observable<boolean>;
  globalMessage$: Observable<string>;
  globalFunMessage$: Observable<string>;

  constructor(
    private renderer: Renderer2, 
    private router: Router, 
    public userService: UserService,
    private toastService: ToastService,
    private loadingService: GlobalLoadingService,
    private supportService: SupportUserService
  ) { 
    this.globalLoading$ = this.loadingService.isVisible$;
    this.globalMessage$ = this.loadingService.message$;
    this.globalFunMessage$ = this.loadingService.funMessage$;
    this.isSupportUser = this.userService.isImpersonating;
  }

  ngOnInit() { 
    const publicRoutes = ['/login', '/signup', '/aup'];

    if (publicRoutes.some(r => this.router.url.startsWith(r))) return;

    const savedTheme = localStorage.getItem('theme');
    this.setTheme(savedTheme || 'light');
    
    this.userService.loadProfile();
  }

  ngAfterViewInit() {
    console.log('Registering toast...');
    this.toastService.register(this.toast);
  }

  setTheme(theme: string) {
    this.isDarkTheme = theme === 'dark';
    this.isContrastTheme = theme === 'contrast';
    this.isLightTheme = theme === 'light';
    localStorage.setItem('theme', theme);
    this.updateTheme();
  }

  updateTheme() {
    this.renderer.removeClass(document.body, 'dark-mode');
    this.renderer.removeClass(document.body, 'high-contrast-mode')
    if (this.isDarkTheme) {
      this.renderer.addClass(document.body, 'dark-mode');
    } else if (this.isContrastTheme) {
      this.renderer.addClass(document.body, 'high-contrast-mode');
    } else
      this.renderer.addClass(document.body, 'light');
  }

  isAuthenticated(): boolean {
    return !!localStorage.getItem('token');
  }

  endSupportSession() {
    const token = localStorage.getItem('token');
    const originalToken = localStorage.getItem('originalToken');

    if (this.userService.isImpersonating && token && originalToken) {
      const decoded: any = this.userService['jwtHelper'].decodeToken(token);
      const logId = decoded?.ImpersonationLogId;

      if (logId) {
        // swap back to original superadmin token
        localStorage.setItem('token', originalToken);

        this.supportService.stopImpersonation(Number(logId)).subscribe({
          next: () => console.log(`Stopped impersonation log ${logId}`),
          error: err => console.warn('Failed to stop impersonation:', err),
          complete: () => {
            localStorage.removeItem('originalToken');
            this.userService.loadProfile(); // reload profile as SuperAdmin
            this.isSupportUser = false;
            this.router.navigate(['/']); 
          }
        });
      }
    }
  }

  logout() {
    this.userService.clearSession();
    this.isSupportUser = false;
    this.router.navigate(['/login']);
  } 
  
  switchToGlobalReviewer(): void {
    this.supportService.switchToReviewerPortal().subscribe({
      next: (res) => {
        const { token } = res;

        if (token) {
          localStorage.setItem('token', token);
          this.toast.show('Switched to Global Reviewer Portal');
          this.userService.loadProfile();
          this.userService.ensureProfile$().subscribe();
        } else {
          this.toast.show('No token returned from server.');
        }
      },
      error: (err) => {
        console.error('Failed to switch:', err);
        this.toast.show('Failed to switch to Global Reviewer Portal');
      }
    });
  }

  switchBackFromGlobalReviewer(): void {
    const token = localStorage.getItem('token');
    const originalToken = localStorage.getItem('originalToken')
    if(token && originalToken){
      localStorage.setItem('token', originalToken);
      localStorage.removeItem('originalToken');
      
      this.userService.loadProfile();
      this.userService.ensureProfile$().subscribe();
      this.router.navigate(['/']);
    }
  }
}
