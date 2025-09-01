import { Component, Renderer2 } from '@angular/core';
import { RouterOutlet, RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { UserService } from './services/user.service';
import { isTokenExpired } from './utilities/auth-utilities';
import { TenantContextService } from './services/tenant-context.service';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  imports: [RouterOutlet,
    CommonModule,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    RouterModule],    
})
export class AppComponent {
  isDarkTheme = false;
  isContrastTheme = false;
  isLightTheme = true;
  userRole: string = '';
  accessAdminMenu: boolean = false;

  constructor(private renderer: Renderer2, private router: Router, public userService: UserService, public tenantContext: TenantContextService) { }

  ngOnInit() {
    this.tenantContext.loadTenantContext().subscribe({
      next: ctx => {
        console.log('tenant context loaded', ctx);
        if(!ctx.isActive) {
          // this.router.navigate(['/inactive']);
          return;
        }     

      const savedTheme = localStorage.getItem('theme');
      this.setTheme(savedTheme || 'light');

      const token = localStorage.getItem('token');

      if (!token) {
        this.router.navigate(['/login']);
        return;
      }

      if (isTokenExpired(token)) {
        console.warn('token expired. redirecting');
        localStorage.removeItem('token');
        this.router.navigate(['/login']);
        return;
      } 
      
      this.userService.loadProfile();
    },
      error: err => {
        console.error('Error loading tenant context', err);
        // this.router.navigate(['/error']);
      }
    });
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
