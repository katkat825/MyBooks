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
import { Observable } from 'rxjs';

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
    MatProgressSpinnerModule
  ]
})
export class AppComponent {
  @ViewChild(ToastComponent) toast!: ToastComponent;

  isDarkTheme = false;
  isContrastTheme = false;
  isLightTheme = true;
  userRole: string = '';
  currentUrl = window.location.href;

  globalLoading$: Observable<boolean>;
  globalMessage$: Observable<string>;

  constructor(
    private renderer: Renderer2, 
    private router: Router, 
    public userService: UserService,
    private toastService: ToastService,
    private loadingService: GlobalLoadingService
  ) { 
    this.globalLoading$ = this.loadingService.isVisible$;
    this.globalMessage$ = this.loadingService.message$;
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

  logout() {
    this.userService.clearSession();
    this.router.navigate(['/login']);
  } 
}
