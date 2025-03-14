import { Component, Renderer2 } from '@angular/core';
import { RouterOutlet, RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterOutlet, CommonModule, MatMenuModule, MatIconModule, MatButtonModule, MatToolbarModule, RouterModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.css'
})
export class AppComponent {
  isDarkTheme = false;
  isContrastTheme = false;
  isLightTheme = true;

  constructor(private renderer: Renderer2, private router: Router) { }

  ngOnInit() {
    const savedTheme = localStorage.getItem('theme');
    this.setTheme(savedTheme || 'light');
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
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
