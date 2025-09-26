import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { UserService } from '../../services/user.service';
import { ConfirmDialogComponent } from '../../components/shared/confirmation.component';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { publish } from 'rxjs';
import { GlobalLoadingService, LoadingContext } from '../../services/global-loading.service';

@Component({
  selector: 'app-book-form',
  templateUrl: './book-form.component.html',
  styleUrls: ['./book-form.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatStepperModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    HttpClientModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatDatepickerModule,
    MatNativeDateModule
  ]
})
export class BookFormComponent implements OnInit {
  bookForm!: FormGroup;
  selectedFile: File | null = null;
  bookId!: number;
  genres: any[] = [];
  ageCategories: any[] = [];
  seriesList: any[] = [];
  newSeries: boolean = false;
  fileId?: number;
  currentUser: any = null;

  constructor(
    private fb: FormBuilder,
    private bookService: BookService,
    private userService: UserService,
    private router: Router,
    private route: ActivatedRoute,
    private dialog: MatDialog,
    private globalLoading: GlobalLoadingService
  ) { }

  ngOnInit(): void {
    this.bookForm = this.fb.group({
      step1: this.fb.group({
        title: ['', Validators.required],
        genreId: ['', Validators.required],
        ageCategoryId: ['', Validators.required]
      }),
      step2: this.fb.group({
        author: [''],
        seriesId: [null],
        seriesPosition: [null],
        seriesName: [''],
        isbn: [''],
        description: [''],
        location: [''],
        publishedDate: [null]
      }),
      step3: this.fb.group({ file: [null] })
    });

    this.loadGenres();
    this.loadAgeCategories();
    this.loadSeries();

    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      if (id) {
        this.userService.getProfile().subscribe({
          next: user => {
            this.currentUser = user;
            this.bookService.getBook(id).subscribe({
              next: book => {
                if (!book) {
                  alert('Book not found.');
                  this.router.navigate(['/']);
                } else if (book.isRestricted)
                {
                  alert('This book is currently under investigation and cannot be modified.');
                  this.router.navigate(['/book', id]);
                } else if (
                  book.createdBy !== this.currentUser.id.toString() &&
                  !['owner', 'superadmin'].includes(this.currentUser.role.toLowerCase())
                ) {
                  alert('You do not have permission to edit this book.');
                  this.router.navigate(['/book', id]);
                } else {
                  this.bookId = id;
                  this.loadBook(this.bookId)
                }
              }
            })
          }
        })
        ;
      }
    });
  }

  get step1(): FormGroup {
    return this.bookForm.get('step1') as FormGroup;
  }

  get step2(): FormGroup {
    return this.bookForm.get('step2') as FormGroup;
  }

  get step3(): FormGroup {
    return this.bookForm.get('step3') as FormGroup;
  }

  loadBook(id: number) {
    this.bookService.getBook(id).subscribe({
      next: (book) => {
        if (book) {   
          this.bookId = book.id;
          this.fileId = book.fileId;

          this.bookForm.patchValue({
            step1: {
              title: book.title,
              genreId: book.genreId,
              ageCategoryId: book.ageCategoryId
            },
            step2: {
              author: book.author,
              seriesId: book.seriesId,
              seriesPosition: book.seriesPosition,
              isbn: book.isbn,
              description: book.description,
              publishedDate: book.publishedDate ? new Date(book.publishedDate).getFullYear() : null,
              location: book.location
            }
          });
        }
      },
      error: (error) => console.error('Error loading book: ', error)
    });
  }

  loadGenres() {
    this.bookService.getGenres().subscribe({
      next: (data: any[]) => this.genres = data.sort((a, b) => a.name.localeCompare(b.name))
    })
  }

  loadAgeCategories() {
    this.bookService.getAgeCategories().subscribe({
      next: (data: any[]) => this.ageCategories = data
    })
  }

  loadSeries() {
    this.bookService.getSeries().subscribe({
      next: (data: any[]) => this.seriesList = data.sort((a, b) => a.name.localeCompare(b.name))
    })
  }

  removeSeries() {
    this.step2.patchValue({ seriesId: null, seriesPosition: null });
  }

  toggleNewSeries() {
    this.newSeries = !this.newSeries;
    this.step2.patchValue({ seriesName: '' });
  }

  saveSeries() {
    const seriesName = (this.step2.get('seriesName')?.value ?? '').trim();
    console.log("Attempting to save series:", seriesName);

    if (!seriesName) {
      alert("Series name cannot be empty.");
      return;
    }

    const exists = this.seriesList.some(s => s.name?.toLowerCase().trim() === seriesName.toLowerCase());
    if (exists) {
      alert('That series already exists.');
      return;
    }

    const newSeries = { name: seriesName };
    
    this.bookService.createSeries(newSeries).subscribe({
      next: (createdSeries) => {
        this.seriesList = [...this.seriesList, createdSeries];
        this.step2.patchValue({ seriesId: createdSeries.id });
        this.toggleNewSeries();
      },
      error: (error) => {
        console.error("Error saving series: ", error);
        alert("Failed to save series.");
      }
    })
  }

  saveStep1() {
    if (this.step1.invalid) {
      alert('Please fill in all required fields.');
      return;
    }

    this.globalLoading.show("Finalizing Step 1...",LoadingContext.BookViewer);

    const bookData = {
      id: this.bookId,
      ...this.step1.value,
      ...this.step2.value,      
    };

    if (this.bookId) {
      this.bookService.updateBook(this.bookId, bookData).subscribe({
        next: () => this.globalLoading.hide(),
        error: (error) => {
          console.error("Error updating book: ", error);
          this.globalLoading.hide();
        }
      });
    } else {
      this.bookService.createBook(bookData).subscribe({
        next: (response) => {
          this.bookId = response.id;

          // pre-fill step 2 with enriched values from backend
          this.bookForm.patchValue({
            step2: {
              author: response.author,
              isbn: response.isbn,
              publishedDate: response.publishedDate ? new Date(response.publishedDate).getFullYear() : null
            }              
          });
          this.globalLoading.hide();
        },
        error: (error) => {
          console.error("Error creating book: ", error);
          this.globalLoading.hide();
        }
      });
    }
  }

  saveStep2() {
    if (!this.bookId) return;

    this.globalLoading.show("Finalizing Step 2...", LoadingContext.BookViewer);
    const bookData = {
      id: this.bookId,
      ...this.step1.value,
      ...this.step2.value
    };

    // normalize publishedDate if exists
    if (bookData.publishedDate && typeof bookData.publishedDate === 'number') {
      bookData.publishedDate = new Date(bookData.publishedDate, 0, 1);
    }

    this.bookService.updateBook(this.bookId, bookData).subscribe({
      next: () => this.globalLoading.hide(),
      error: (error) => {
        console.error('Error updating book: ', error);
        this.globalLoading.hide();
      }
    });
  }

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0] || null;
  }

  skipFile() {
    this.router.navigate(['/']);
  }

  uploadFileWithBookId() {
    if (!this.selectedFile || !this.bookId) {
      console.warn("No file selected or bookId missing.");
      return;
    }

    if(this.fileId) {
      const dialogRef = this.dialog.open(ConfirmDialogComponent, {
        data: {
          itemType: 'Existing File',
          itemSpecific: this.selectedFile.name,
          message: "Uploading a new file will permanently delete the old one.",
          cancelText: "Keep Existing File",
          confirmText: "Replace File"
        }
      });

      dialogRef.afterClosed().subscribe((result) => {
        if (result) {
          this.doUpload();
        }
      });
    } 
    else {
      this.doUpload();
    }
  }

  private doUpload() {
    const bookTitle = this.bookForm.get('step1')?.get('title')?.value;
    this.globalLoading.show("Uploading file...Don't leave this page until upload is complete", LoadingContext.BulkImport);

    this.bookService.uploadFile(this.selectedFile!, this.bookId, bookTitle).subscribe({
      next: (response) => {
        if (response && response.fileId) {
          this.fileId = response.fileId;

          if (!this.fileId) { //yes I know it's redundant, but I can't get updateBookFileId to work without it
            this.globalLoading.hide();
            return;
          }
          this.bookService.updateBookFileId(this.bookId, this.fileId).subscribe({
            next: () => {
              this.globalLoading.hide();
              this.router.navigate(['/']);
            },
            error: (error) => {
              console.error("Failed to update book with FileId", error);
              this.globalLoading.hide();
            }
          });
        } else {
          console.warn("no file id returned from api");
          this.globalLoading.hide();
        }
      },
      error: (error) => {
        console.error('error uploading file', error);
        this.globalLoading.hide();
      }
    });
  }
}
