import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SupportUserService } from '../../../services/support-user.service';
import { GlobalLoadingService, LoadingContext } from '../../../services/global-loading.service';

@Component({
  selector: 'app-content-review',
  standalone: true,
  templateUrl: './content-review.component.html',
  styleUrls: ['./content-review.component.css'],
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule
  ]
})
export class ContentReviewComponent implements OnInit {
  books: any[] = [];
  filteredBooks: any[] = [];
  displayedColumns: string[] = ['id', 'title', 'author', 'series', 'genre', 'actions'];
  searchTerm = '';

  constructor(
    private supportService: SupportUserService,
    private loadingService: GlobalLoadingService
  ) {}

  ngOnInit(): void {
    this.loadBooks();
  }

  loadBooks(): void {
    this.loadingService.show('Loading books for review...', LoadingContext.BookViewer);
    this.supportService.getAllBooks().subscribe({
      next: (data) => {
        const books = Array.isArray(data) ? data : data?.$values || [];

        this.books = books
          .filter((b: any) => b.isActive && b.fileId)
          .sort((a: any, b: any) => {
            const authorA = a.author?.toLowerCase() || '';
            const authorB = b.author?.toLowerCase() || '';
            if (authorA !== authorB) return authorA.localeCompare(authorB);

            const seriesA = a.series?.name?.toLowerCase() || '';
            const seriesB = b.series?.name?.toLowerCase() || '';
            if (seriesA !== seriesB) return seriesA.localeCompare(seriesB);

            const posA = a.seriesPosition ?? 0;
            const posB = b.seriesPosition ?? 0;
            if (posA !== posB) return posA - posB;

            const titleA = a.title?.toLowerCase() || '';
            const titleB = b.title?.toLowerCase() || '';
            return titleA.localeCompare(titleB);
          });

        this.filteredBooks = [...this.books];
        this.loadingService.hide();
      },
      error: (err) => {
        console.error('Failed to load books:', err);
        this.loadingService.hide();
      }
    });
  }

  applyFilters(): void {
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredBooks = this.books.filter(b =>
      (!term ||
        (b.title?.toLowerCase().includes(term) ||
         b.author?.toLowerCase().includes(term) ||
         b.series?.name?.toLowerCase().includes(term) ||
         b.genre?.name?.toLowerCase().includes(term)))
    );
  }
}
