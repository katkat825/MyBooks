import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { BookService } from '../../services/book.service';
import { MatIconModule } from '@angular/material/icon';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-admin-genres',
  standalone: true,
  templateUrl: './admin-genres.component.html',
  styleUrls: ['./admin-genres.component.css'],
  imports: [
    FormsModule, 
    MatFormFieldModule, 
    MatInputModule, 
    CommonModule, 
    MatTableModule, 
    MatIconModule, 
    ReactiveFormsModule,
    MatDialogModule
  ]
})
export class AdminGenresComponent {
  genres: any[] = [];
  editForm!: FormGroup;
  editingGenre: any = null;
  createForm!: FormGroup;
  addingGenre: boolean = false;

  constructor(private bookService: BookService, private fb: FormBuilder, private dialog: MatDialog) { }

  ngOnInit(): void {
    this.loadGenres();
    this.editForm = this.fb.group({
      name: ['', Validators.required]
    });
    this.createForm = this.fb.group({
      name: ['', Validators.required]
    });
  }

  loadGenres() {
    this.bookService.getGenres().subscribe({
      next: (data) => {
        this.genres = data.sort((a, b) => a.name.localeCompare(b.name));
        },
      error: (error) => console.error("Error fetching genres: ", error)
    });
  }

  startEdit(genre: any) {
    this.editingGenre = genre;
    this.editForm.patchValue({ name: genre.name });
  }

  cancelEdit() {
    this.editingGenre = null;
    this.editForm.reset();
  }

  saveEdit() {
    if (!this.editingGenre) return;

    const updatedGenre = { ...this.editingGenre, name: this.editForm.value.name };

    this.bookService.updateGenre(updatedGenre.id, updatedGenre).subscribe({
      next: () => {
        this.loadGenres();
        this.cancelEdit();
      },
      error: (error) => {
        console.error("Error updating genres: ", error);
        alert("Failed to update genres.");
      }
    });
  }

  cancelCreate() {
    this.addingGenre = false;
    this.createForm.reset();
  }

  addGenre() {
    this.addingGenre = true;
    this.createForm.reset();
  }

  saveCreate() {
    if (this.createForm.invalid) {
      alert("Genre name cannot be empty.");
      return;
    }

    const newGenre = { name: this.createForm.value.name.trim() };

    this.bookService.createGenre(newGenre).subscribe({
      next: (createdGenre) => {
        this.genres.push(createdGenre);
        this.addingGenre = false;
        this.createForm.reset();
        this.loadGenres();
      },
      error: (error) => {
        console.error("error adding genres: ", error);
        alert("Failed to add genres.");
      }
    })
  }

  deleteGenre(genre: any) {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { itemType: 'Genre', itemSpecific: genre.name}
    });

    dialogRef.afterClosed().subscribe((result) => {
      if(result) {
        this.bookService.deleteGenre(genre.id).subscribe({
          next: () => this.loadGenres(),
          error: (error) => {
            console.error('Error deleting genres: ', error)
            if (error.status === 409) {
              alert("This genres cannot be deleted because it contains books.");
            } else {
              alert("An error occurred while deleting the genres.");
            }
          }
        });
      }
    });
  }
}
