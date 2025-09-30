import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';


@Component({
  selector: 'app-support-layout',
  standalone: true,
  imports: [
    RouterModule,
    MatSidenavModule,
    MatListModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './support-layout.component.html',
  styleUrls: ['./support-layout.component.css']
})
export class SupportLayoutComponent {
  menuOpen = true;

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
  }
}
