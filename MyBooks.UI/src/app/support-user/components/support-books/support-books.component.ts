import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SupportUserService } from '../../../services/support-user.service';

@Component({
  selector: 'app-support-books',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSlideToggleModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
    RouterModule,
    MatSelectModule,
    MatCheckboxModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './support-books.component.html',
  styleUrls: ['./support-books.component.css']
})
export class SupportBooksComponent implements OnInit {
  private supportService = inject(SupportUserService);

  books: any[] = [];
  tenants: any[] = [];

  // allow filtering
  filteredBooks: any[] = [];
  searchTerm: string = '';
  showOnlyActive: boolean = false;
  showOnlyRestricted: boolean = false;
  selectedTenant: number | null = null;
  
  displayedColumns = ['id', 'title', 'tenant','isActive', 'restricted', 'actions'];

  ngOnInit(): void {
    this.supportService.getAllBooks().subscribe(data => {
      this.books = data.$values;
      this.tenants = [...new Set(this.books.map(b => b.tenantId))].sort();
      this.applyFilters();
    });
  }

  toggleRestricted(book: any): void {
    const newValue = !book.isRestricted;
    this.supportService.toggleBookRestricted(book.id, newValue).subscribe(() => {
      book.isRestricted = newValue; 
      this.applyFilters();
    });
  }

  applyFilters(): void {
    this.filteredBooks = this.books.filter(book => {
      const matchesTitle = book.title?.toLowerCase().includes(this.searchTerm.toLowerCase());
      const matchesActive = !this.showOnlyActive || book.isActive;
      const matchesRestricted = !this.showOnlyRestricted || book.isRestricted;
      const matchesTenant = !this.selectedTenant || book.tenantId === this.selectedTenant;
      return matchesTitle && matchesActive && matchesRestricted && matchesTenant;
    });
  }
}
