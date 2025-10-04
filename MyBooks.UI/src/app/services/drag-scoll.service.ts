import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DragScrollService {

  enableDragScroll(element: HTMLElement): void {
    let isDown = false;
    let startX = 0;
    let startY = 0;
    let scrollLeft = 0;
    let scrollTop = 0;

    const startDrag = (e: MouseEvent | TouchEvent) => {
      isDown = true;
      const pageX = e instanceof MouseEvent ? e.pageX : e.touches[0].pageX;
      const pageY = e instanceof MouseEvent ? e.pageY : e.touches[0].pageY;
      startX = pageX - element.offsetLeft;
      startY = pageY - element.offsetTop;
      scrollLeft = element.scrollLeft;
      scrollTop = element.scrollTop;
      element.style.cursor = 'grabbing';
      element.style.userSelect = 'none';
    };

    const endDrag = () => {
      isDown = false;
      element.style.cursor = 'grab';
      element.style.removeProperty('user-select');
    };

    const moveDrag = (e: MouseEvent | TouchEvent) => {
      if (!isDown) return;
      e.preventDefault();
      const pageX = e instanceof MouseEvent ? e.pageX : e.touches[0].pageX;
      const pageY = e instanceof MouseEvent ? e.pageY : e.touches[0].pageY;
      const x = pageX - element.offsetLeft;
      const y = pageY - element.offsetTop;
      const walkX = (x - startX) * 1.2;
      const walkY = (y - startY) * 1.2;
      element.scrollLeft = scrollLeft - walkX;
      element.scrollTop = scrollTop - walkY;
    };

    // attach event listeners
    element.addEventListener('mousedown', startDrag);
    element.addEventListener('mouseleave', endDrag);
    element.addEventListener('mouseup', endDrag);
    element.addEventListener('mousemove', moveDrag);

    element.addEventListener('touchstart', startDrag, { passive: false });
    element.addEventListener('touchend', endDrag);
    element.addEventListener('touchmove', moveDrag, { passive: false });

    element.style.cursor = 'grab';
  }

  disableDragScroll(element: HTMLElement): void {
    // remove listeners and reset styles
    const clone = element.cloneNode(true) as HTMLElement;
    element.replaceWith(clone);
  }
}
