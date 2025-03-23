declare module 'pdfjs-dist/web/pdf_viewer' {
  export class PDFViewer {
    constructor(options: any);
    setDocument(pdfDocument: any): void;
  }
  export class PDFLinkService {
    constructor(options: any);
    setViewer(viewer: PDFViewer): void;
  }
  export class EventBus {
    constructor();
    on(eventName: string, callback: (...args: any[]) => void): void;
    off(eventName: string, callback: (...args: any[]) => void): void;
    dispatch(eventName: string, data?: any): void;
  }
}
