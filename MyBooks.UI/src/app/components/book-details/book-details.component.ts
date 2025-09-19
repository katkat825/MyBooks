import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { BookService } from '../../services/book.service';
import { UserService } from '../../services/user.service';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-book-details',
  standalone: true,
  templateUrl: './book-details.component.html',
  styleUrls: ['./book-details.component.css'],
  imports: [
    CommonModule,
     MatIconModule, 
     MatButtonModule, 
     RouterModule, 
     HttpClientModule, 
     MatCardModule,
     MatDialogModule
    ],
})
export class BookDetailsComponent implements OnInit {
  book: any = null;
  bookCards: any[] = [];
  readingMode: boolean = false;
  readingUrl: SafeResourceUrl = '';
  readingProgress: number = 0;
  unauthorized: boolean = false;
  currentUser: any = null;

  constructor(
    private route: ActivatedRoute,
    private bookService: BookService,
    private userService: UserService,
    private router: Router,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.userService.getProfile().subscribe({
      next: (user) => { this.currentUser = user; },
      error: (err) => { console.error("Error fetching current user profile", err); }
    });

    const bookId = Number(this.route.snapshot.paramMap.get('id'));
    if (bookId) {
      this.bookService.getBook(bookId).subscribe({
        next: (data) => {
          if (!data || Object.keys(data).length === 0) {
            this.unauthorized = true;
            this.book = null;
          } else {
            this.book = data;
            this.unauthorized = false;
          }
        },
        error: (error) => {
          console.error('Error fetching book details:', error);
          if ([403, 401, 404].includes(error.status)) {
            this.unauthorized = true;
            this.book = null;
          }
        }
      });
    }
  }

  hasEditDeletePermission(): boolean {
    if (!this.book || !this.currentUser) return false;
    return this.book.createdBy === this.currentUser.id.toString() ||
      ['admin', 'editor', 'owner', 'superadmin'].includes(this.currentUser.role.toLowerCase());
  }

  editBook(book: any) {
    this.router.navigate(['/create/', book.id])
  }

  deleteBook(book: any) {
    if (book.isRestricted)
      {
        alert('This book is currently under investigation and cannot be modified.');
        this.router.navigate(['/book', book.id]);
        return;
      }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { itemType: 'Book', itemSpecific: book.title }
    });
    
    dialogRef.afterClosed().subscribe((result) => {
      if(result) {
        const id = book.id;

        if (book.fileId) {
          this.bookService.deleteFile(book.fileId).subscribe({
            next: () => {
              this.deleteBookRecord(id);
            },
            error: (error) => console.error("Error deleting file", error),
          });
        } else {
          this.deleteBookRecord(id);
        }
      }
    });      
  }

  private deleteBookRecord(id: number) {
    this.bookService.deleteBook(id).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (error) => console.error("Error deleting book: ", error),
    });
  }

  downloadBookFile(fileId: number) {
    if (this.book.isRestricted)
      {
        alert('This book is currently under investigation and cannot be downloaded.');
        this.router.navigate(['/book', this.book.id]);
        return;
      }
    this.bookService.downloadFile(fileId).subscribe({
      next: (fileBlob) => {
        const blobUrl = window.URL.createObjectURL(fileBlob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = this.book.title || 'book-file';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      },
      error: (error) => console.error('Error downloading file', error)
    });
  }
}
