import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SupportUserService } from '../../../services/support-user.service';
import { RouterModule } from '@angular/router';

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
    RouterModule
  ],
  templateUrl: './support-books.component.html',
  styleUrls: ['./support-books.component.css']
})
export class SupportBooksComponent implements OnInit {
  private supportService = inject(SupportUserService);

  books: any[] = [];
  displayedColumns = ['id', 'title', 'tenant','isActive', 'restricted', 'actions'];

  ngOnInit(): void {
    this.supportService.getAllBooks().subscribe(data => {
      this.books = data.$values;
    });
  }

  toggleRestricted(book: any): void {
    const newValue = !book.isRestricted;
    this.supportService.toggleBookRestricted(book.id, newValue).subscribe(() => {
      book.isRestricted = newValue; // update ui after success
    });
  }
}
