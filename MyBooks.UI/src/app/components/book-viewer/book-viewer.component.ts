import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, OnDestroy, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { Subscription, fromEvent } from 'rxjs';
import { debounce, debounceTime } from 'rxjs/operators';
import * as pdfjsLib from 'pdfjs-dist';
import { PDFViewer, PDFLinkService, EventBus } from 'pdfjs-dist/web/pdf_viewer';
import ePub from 'epubjs';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { GlobalLoadingService, LoadingContext } from '../../services/global-loading.service';
import { SupportUserService } from '../../services/support-user.service';
import { DragScrollService } from '../../services/drag-scoll.service';

pdfjsLib.GlobalWorkerOptions.workerSrc = '/pdf.worker.min.mjs';

@Component({
  selector: 'app-book-viewer',
  standalone: true,
  templateUrl: './book-viewer.component.html',
  styleUrls: ['./book-viewer.component.css'],
  imports: [
  CommonModule,
  MatIconModule]
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
  zoomLevel: number = 1.0;
  readingProgress: number = 0;

  private currentEpubContents: any;
  private themeObserver: MutationObserver | null = null;
  private pdfViewer?: PDFViewer;
  private readonly ZOOM_KEY = "bookViewerZoom";

  constructor(
    private route: ActivatedRoute,
    private bookService: BookService,
    private supportService: SupportUserService,
    private router: Router,
    private globalLoading: GlobalLoadingService,
    private dragScroll: DragScrollService
  ) { }  

  // swap bookService or supportService depending on route
  get viewerService() {
    const url = this.router.url;
    const service = url.startsWith('/support') ? this.supportService : this.bookService;
    return service;
  }

  get isSupportService() {
    const url = this.router.url;
    return url.startsWith('/support');
  }

  ngOnInit(): void {
    this.globalLoading.show("Loading your book...", LoadingContext.BookViewer);

    const savedZoom = localStorage.getItem(this.ZOOM_KEY);
    if (savedZoom) {
      this.zoomLevel = parseFloat(savedZoom);
    }

    this.fileId = Number(this.route.snapshot.paramMap.get('fileId'));
    
    this.viewerService.getFileMetadata(this.fileId).subscribe({
      next: (metadata) => {
        if (!metadata) {
          alert('File not found.');
          this.router.navigate(['/']);
          this.globalLoading.hide();
          return;
        }

        // check if book is restricted
        this.viewerService.getBook(metadata.bookId).subscribe({
          next: (book) => {
            if (book.isRestricted && !this.isSupportService) {
              alert('This book is currently under investigation and cannot be viewed.');
              this.router.navigate(['/book', metadata.bookId]);
              this.globalLoading.hide();
              return;
            }

            // safe to continue with fileType logic
            if (metadata.contentType === 'application/pdf') {
              this.fileType = 'pdf';
              if (metadata.isConverted === true)
                this.fileType = 'epub';
            } else if (metadata.contentType === 'application/epub+zip') {
              this.fileType = 'epub';
            } else {
              console.warn('Unsupported ContentType, defaulting to pdf');
              this.fileType = 'pdf';
            }

            this.loadFile();
          },
          error: (err) => {
            console.error('Error fetching book details', err);
            this.router.navigate(['/']);
            this.globalLoading.hide();
          }
        });
      },
      error: (err) => {
        console.error('Error fetching file metadata', err);
        this.router.navigate(['/']);
        this.globalLoading.hide();
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

  zoomIn(): void {
    this.setZoom(this.zoomLevel + 0.25);
  }

  zoomOut(): void {
    this.setZoom(Math.max(0.5, this.zoomLevel - 0.25));
  }

  private setZoom(level: number): void {
    this.zoomLevel = level;
    localStorage.setItem(this.ZOOM_KEY, this.zoomLevel.toString());

    if(this.fileType === 'pdf' && this.pdfViewer) {
      (this.pdfViewer as any).currentScale = this.zoomLevel;
    } else if (this.fileType === 'epub' && this.rendition) {
      this.rendition.themes.fontSize(`${this.zoomLevel * 100}%`);
    }
  }

  loadFile(): void {
    this.viewerService.downloadFile(this.fileId, true).subscribe({
      next: (fileBlob) => {
        if (this.fileType === 'pdf') {
          this.loadPdf(fileBlob);
        } else if (this.fileType === 'epub') {
          this.loadEpub(fileBlob);
        }
      },
      error: (error) => {
        console.error("error downloading file: ", error);
        this.globalLoading.hide();
        alert('Error downloading file.');
      }
    });
  }

  loadPdf(fileBlob: Blob): void {
    const blobUrl = URL.createObjectURL(fileBlob);
    const loadingTask = pdfjsLib.getDocument(blobUrl);
    loadingTask.promise.then((pdf: any) => {
      this.pdfDocument = pdf;
      this.renderPdf();
    }).catch((error: any) => {
      console.error("error loading pdf: ", error);
      this.globalLoading.hide();
      alert('Error loading PDF document.');
    });
  }

  renderPdf(): void {
    const container = this.viewerContainer.nativeElement;
    const viewer = this.pdfViewerElement.nativeElement;

    const eventBus = new EventBus();
    const pdfLinkService = new PDFLinkService({ eventBus });
    this.pdfViewer = new PDFViewer({
      container: container,
      viewer: viewer,
      eventBus,
      linkService: pdfLinkService
    });
    pdfLinkService.setViewer(this.pdfViewer);
    this.pdfViewer.setDocument(this.pdfDocument);
    this.dragScroll.enableDragScroll(this.viewerContainer.nativeElement);

    eventBus.on('pagesinit', () => {
      (this.pdfViewer as any).currentScale = this.zoomLevel;
      this.viewerService.getReadingProgress(this.fileId).subscribe({
        next: (progressData) => {
          const progressPercent = progressData && (progressData.ProgressPercent || progressData.progressPercent) || 0;
          this.readingProgress = progressPercent;
          const totalScrollable = container.scrollHeight - container.clientHeight;
          container.scrollTop = (progressPercent / 100) * totalScrollable;
          this.globalLoading.hide();
        },
        error: (error) => {
          console.error("Error fetching reading progress:", error);
          this.globalLoading.hide();
        }
      });
    });
  }

  handlePdfScroll(): void {
    //calculate progress percentage from scroll position
    const container = this.viewerContainer.nativeElement;
    const scrollTop = container.scrollTop;
    const totalScrollable = container.scrollHeight - container.clientHeight;
    const progress = totalScrollable > 0 ? (scrollTop / totalScrollable) * 100 : 0;
    this.readingProgress = progress;
    this.updateProgress(progress);
  }

  private injectThemeIntoEpub(contents: any): void {
    const themeClass = Array.from(document.body.classList).find(cls =>
      ['dark-mode', 'high-contrast-mode'].includes(cls));
    
    const temp = document.createElement('div');
    if (themeClass) temp.classList.add(themeClass);
    temp.style.display = 'none';
    document.body.appendChild(temp);

    const computed = getComputedStyle(temp);
    const textColor = computed.getPropertyValue('--text-color').trim();
    const bgColor = computed.getPropertyValue('--bg-color').trim();
    document.body.removeChild(temp); // Clean up

    const styleEl = contents.document.createElement('style');
    styleEl.innerHTML = `
      body, p, h1, h2, h3, h4, h5, h6, span, a {
        color: ${textColor} !important;
        background-color: ${bgColor} !important;
      }
      ::selection {
        background-color: ${textColor}33;
      }
    `;

    const oldStyle = contents.document.head.querySelector('style[data-theme]');
    if(oldStyle) 
      oldStyle.remove(); // Remove old style if exists
    styleEl.setAttribute('data-theme', 'injected'); 
    contents.document.head.appendChild(styleEl);
  }

  loadEpub(fileBlob: Blob): void {
    const blobUrl = URL.createObjectURL(fileBlob);
    this.epubBook = ePub(blobUrl, { openAs: 'epub' });

    this.rendition = this.epubBook.renderTo(this.viewerContainer.nativeElement, {
      width: '100%',
      height: '90%',
      allowScriptedContent: true
    });
    
    this.rendition.themes.fontSize(`${this.zoomLevel * 100}%`)

    this.rendition.hooks.content.register((contents: any) => {
      this.currentEpubContents = contents;
      this.injectThemeIntoEpub(contents);
      contents.document.defaultView.frameElement.setAttribute('sandbox', 'allow-scripts allow-same-origin');
    });

    this.epubBook.ready.then(() => {
      return this.epubBook.locations.generate(1600);
    }).then(() => {
      this.rendition.on('relocated', (location: any) => {
        const currentLocation = this.epubBook.locations.locationFromCfi(location.start.cfi);
        const totalLocations = this.epubBook.locations.total;

        //calculate progress
        if (currentLocation !== undefined && totalLocations > 0) {
          const progress = (currentLocation / totalLocations) * 100;
          this.readingProgress = progress;
          this.updateProgress(progress);
        } else {
          console.warn("Unable to calculate EPUB progress.");
          this.globalLoading.hide();
        }
      });

      //get reading progress
      this.viewerService.getReadingProgress(this.fileId).subscribe({
        next: (progressData) => {
          const progressPercent = progressData && (progressData.ProgressPercent || progressData.progressPercent) || 0;
          this.readingProgress = progressPercent;
          const totalLocations = this.epubBook.locations.total;
          const savedLocationIndex = Math.floor((progressPercent / 100) * totalLocations);
          const cfi = this.epubBook.locations.cfiFromLocation(savedLocationIndex);

          if (cfi) {
            this.rendition.display(cfi);
          } else {
            this.rendition.display();
          }

          this.themeObserver = new MutationObserver(() => {
            if (this.currentEpubContents) {
              this.injectThemeIntoEpub(this.currentEpubContents);
            }
          });
          this.themeObserver.observe(document.body, { attributes: true, attributeFilter: ['class'] });

          this.globalLoading.hide();
        },
        error: (error) => {
          console.error("Error fetching reading progress:", error);
          this.rendition.display();
          this.globalLoading.hide();
        }
      });

    }).catch((err: any) => {
      console.error('Error generating locations:', err);
      this.globalLoading.hide();
      alert('Error loading EPUB document.');
    });      
  }

  updateProgress(progress: number): void {
    this.viewerService.updateReadingProgress(this.fileId, progress).subscribe({
      error: (error) => console.error('Error updating progress: ', error)
    });
  }

  nextPage(): void {
    if (this.rendition) {
      this.rendition.next();
    }
  }

  prevPage(): void {
    if (this.rendition) {
      this.rendition.prev();
    }
  }
}
