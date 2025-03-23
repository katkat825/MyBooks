import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, OnDestroy, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BookService } from '../../services/book.service';
import { Subscription, fromEvent } from 'rxjs';
import { debounce, debounceTime } from 'rxjs/operators';
import * as pdfjsLib from 'pdfjs-dist';
import { PDFViewer, PDFLinkService, EventBus } from 'pdfjs-dist/web/pdf_viewer';
import ePub from 'epubjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';

pdfjsLib.GlobalWorkerOptions.workerSrc = '/pdf.worker.min.mjs';

@Component({
  selector: 'app-book-viewer',
  standalone: true,
  templateUrl: './book-viewer.component.html',
  styleUrls: ['./book-viewer.component.css'],
  imports: [MatProgressSpinnerModule,
  CommonModule]
})
export class BookViewerComponent implements OnInit, AfterViewInit, OnDestroy {
  fileId!: number;
  fileType: string = 'pdf';
  @ViewChild('viewerContainer') viewerContainer!: ElementRef;
  @ViewChild('pdfViewer') pdfViewerElement!: ElementRef;

  pdfDocument: any;
  epubBook: any;
  rendition: any;
  scrollSub!: Subscription;
  isLoading: boolean = true;

  constructor(
    private route: ActivatedRoute,
    private bookService: BookService
  ) { }  

  ngOnInit(): void {
    this.fileId = Number(this.route.snapshot.paramMap.get('fileId'));

    this.bookService.getFileMetadata(this.fileId).subscribe({
      next: (metadata) => {
        if (metadata && metadata.ContentType) {
          if (metadata.ContentType === 'application/pdf') {
            this.fileType = 'pdf';
          } else if (metadata.ContentType === 'application/epub+zip') {
            this.fileType = 'epub';
          } else {
            console.warn('Unsupported ContentType, defaulting to pdf');
            this.fileType = 'pdf';
          }
        }

        this.loadFile();
      },
      error: (err) => {
        console.error('Error fetching file metadata', err);
        this.fileType = 'pdf';
        this.loadFile();
      }
    });
  }

  ngAfterViewInit(): void {
    if (this.fileType === 'pdf') {
      this.scrollSub = fromEvent(this.viewerContainer.nativeElement, 'scroll')
        .pipe(debounceTime(200))
        .subscribe(() => this.handlePdfScroll());
    }
  }

  ngOnDestroy(): void {
    if (this.scrollSub) {
      this.scrollSub.unsubscribe();
    }
  }

  loadFile(): void {
    this.bookService.downloadFile(this.fileId).subscribe({
      next: (fileBlob) => {
        if (this.fileType === 'pdf') {
          this.loadPdf(fileBlob);
        } else if (this.fileType === 'epub') {
          this.loadEpub(fileBlob);
        }
      },
      error: (error) => console.error("error downloading file: ", error)
    });
  }

  loadPdf(fileBlob: Blob): void {
    const blobUrl = URL.createObjectURL(fileBlob);
    const loadingTask = pdfjsLib.getDocument(blobUrl);
    loadingTask.promise.then((pdf: any) => {
      console.log('PDF loaded with', pdf.numPages, 'pages.');
      this.pdfDocument = pdf;
      this.renderPdf();
    }).catch((error: any) => {
      console.error("error loading pdf: ", error);
    });
  }

  renderPdf(): void {
    const container = this.viewerContainer.nativeElement;
    const viewer = this.pdfViewerElement.nativeElement;

    const eventBus = new EventBus();
    const pdfLinkService = new PDFLinkService({ eventBus });
    const pdfViewer = new PDFViewer({
      container: container,
      viewer: viewer,
      eventBus,
      linkService: pdfLinkService
    });
    pdfLinkService.setViewer(pdfViewer);
    pdfViewer.setDocument(this.pdfDocument);

    eventBus.on('pagesinit', () => {
      this.bookService.getReadingProgress(this.fileId).subscribe({
        next: (progressData) => {
          const progressPercent = progressData && (progressData.ProgressPercent || progressData.progressPercent) || 0;
          const totalScrollable = container.scrollHeight - container.clientHeight;
          container.scrollTop = (progressPercent / 100) * totalScrollable;
          this.isLoading = false;
        },
        error: (error) => console.error("Error fetching reading progress:", error)
      });
    });
  }

  handlePdfScroll(): void {
    //calculate progress percentage from scroll position
    const container = this.viewerContainer.nativeElement;
    const scrollTop = container.scrollTop;
    const totalScrollable = container.scrollHeight - container.clientHeight;
    const progress = totalScrollable > 0 ? (scrollTop / totalScrollable) * 100 : 0;
    this.updateProgress(progress);
  }

  loadEpub(fileBlob: Blob): void {
    const blobUrl = URL.createObjectURL(fileBlob);
    this.epubBook = ePub(blobUrl);

    this.rendition = this.epubBook.renderTo(this.viewerContainer.nativeElement, {
      width: '100%',
      height: '90vh'
    });

    this.rendition.display();
    this.epubBook.locations.generate(1600).then(() => {
      this.rendition.on('relocated', (location: any) => {
        const progress = this.epubBook.locations.percentageFromCfi(location.start.cfi) * 100;
        this.updateProgress(progress);
      });
      this.isLoading = false;
    });
  }

  updateProgress(progress: number): void {
    this.bookService.updateReadingProgress(this.fileId, progress).subscribe({
      next: () => console.log(`Progress updated: ${progress.toFixed(2)}%`),
      error: (error) => console.error('Error updating progress: ', error)
    });
  }
}
