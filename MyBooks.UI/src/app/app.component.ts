import { Component, Renderer2 } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterOutlet, CommonModule, MatMenuModule, MatIconModule, MatButtonModule, MatToolbarModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.css'
})
export class AppComponent {
    isDarkMode = false;

    constructor(private renderer: Renderer2) { }

    ngOnInit() {
        const savedTheme = localStorage.getItem('theme');
        this.isDarkMode = savedTheme === 'dark';
        this.updateTheme();
    }

    toggleTheme() {
        this.isDarkMode = !this.isDarkMode;
        localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
        this.updateTheme();
    }

    setTheme(theme: string) {
        this.isDarkMode = theme === 'dark';
        localStorage.setItem('theme', theme);
        this.updateTheme();
    }

    updateTheme() {
        if (this.isDarkMode) {
            this.renderer.addClass(document.body, 'dark-mode');
        } else {
            this.renderer.removeClass(document.body, 'dark-mode');
        }
    }
}
