import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserService } from '../../services/user.service';
import { GlobalLoadingService, LoadingContext } from '../../services/global-loading.service';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';

@Component({
  selector: 'app-book-list',
  standalone: true,
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.css',
  encapsulation: ViewEncapsulation.None,
  imports: [
    CommonModule, 
    MatIconModule, 
    MatButtonModule, 
    HttpClientModule, 
    MatFormFieldModule, 
    MatInputModule, 
    FormsModule, 
    MatProgressSpinner,
    MatTooltipModule,
    MatDividerModule,
    MatExpansionModule
  ]
})
export class BookListComponent {
  books: any[] = [];
  filteredBooks: any[] = [];
  recentReads: any[] = [];
  searchQuery: string = '';
  currentUser: any = null;
  continueReadingExpanded = true;
  
  page = 1;
  pageSize = 20;
  totalCount = 0;
  showRecentReads = true;
  isLoading = false;
  allBooksLoaded = false;

  constructor(
    private bookService: BookService, 
    private router: Router, 
    private userService: UserService,
    private globalLoading: GlobalLoadingService
  ) { }

  ngOnInit(): void {
    this.userService.getProfile().subscribe({
      next: (user) => { this.currentUser = user; },
      error: (err) => { 
        console.error("Error fetching current user profile", err);
        this.globalLoading.hide();
      }
    });

    this.globalLoading.show("Loading books...", LoadingContext.Login);
    this.loadRecentReads();
  }

  loadRecentReads(): void {
    this.bookService.getRecentlyRead(10).subscribe({
      next: (data) => {
        this.recentReads = Array.isArray(data)
          ? data.sort((a, b) => new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime())
          : [];

        if(this.recentReads.length <= 0)
          this.showRecentReads = false;

        this.globalLoading.hide();
      },
      error: (err) => {
        console.error("Error loading recently read books: ", err);
        this.showRecentReads = false;
      }
    })
    this.loadBooks();
  }

  viewBookDetails(book: any) {
    const id = book.id ?? book.bookId;
    this.router.navigate(['/book', id]);
  }

  hasCreatePermission(): boolean {
    if (!this.currentUser) return false;
    return ['owner', 'superadmin', 'support'].includes(this.currentUser.role.toLowerCase());
  }

  hasGlobalReviewer() : boolean {
    if (!this.currentUser) return false;
    return [ 'globalreviewer'].includes(this.currentUser.role.toLowerCase());
  }

  loadBooks(): void {
    console.log('Loading page', this.page);
    if (this.isLoading || this.allBooksLoaded) 
      return;

    this.isLoading = true;

    this.bookService.getBooks(this.page, this.pageSize).subscribe({
      next: (response) => {
        const results = response.results || [];
        if (results.length === 0) {
          this.allBooksLoaded = true;
          this.globalLoading.hide();
          this.isLoading = false;
          return;
        }
        
        const newBooks = results.filter(
          (b: any) => !this.books.some((existing: any) => existing.id === b.id)
        );

        this.books = [...this.books, ...newBooks];

        // keep active search in place
        if(this.searchQuery.trim())
        {
          this.filterBooks();
        } else {
          this.filteredBooks = [...this.books];
        }

        this.totalCount = response.totalCount || this.books.length;
        this.page++;
        this.isLoading = false;
        this.globalLoading.hide();
        
        if(results.length < this.pageSize) {
          this.allBooksLoaded = true;
        } else {
          // automatically load next page
          setTimeout(() => this.loadBooks(), 100)
        }
      },
      error: (error) => {
        console.error('Error loading books', error);
        this.globalLoading.hide();
        this.isLoading = false;
      }
    });
  }

  filterBooks() {
    const query = this.searchQuery.toLowerCase().trim();
    if(!query) {
      this.filteredBooks = [...this.books];
    }

    this.filteredBooks = this.books
      .filter(book =>
        book.title.toLowerCase().includes(query) ||
        book.author?.toLowerCase().includes(query) ||
        book.series?.name?.toLowerCase().includes(query) ||
        book.genre?.name?.toLowerCase().includes(query)
      )
      .sort((a, b) => a.title.localeCompare(b.title));
  }

  addBook() {
    this.router.navigate(['/create']);
  }

  openBookViewer(book: any, event: MouseEvent) {
    event.stopPropagation();

    const routeBase = this.hasGlobalReviewer()
      ? '/support/book-viewer'
      : '/book-viewer';

    this.router.navigate([routeBase, book.fileId]);
  }
}
