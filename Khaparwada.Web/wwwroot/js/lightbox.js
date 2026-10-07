// Photo viewer for lists marked data-lightbox (album pages, a place's photos). The links inside
// (a[data-lightbox-item]) point at the large photo, so without JavaScript, or with Ctrl/middle click, they simply
// open it. With JavaScript they open the <dialog data-lightbox-dialog> from Views/Shared/_Lightbox.cshtml:
// previous/next buttons, arrow keys and swipes; Esc, focus handling and the backdrop come with <dialog>.
// Text from the page (captions) is set with textContent, never as HTML.
(() => {
    'use strict';

    const dialog = document.querySelector('[data-lightbox-dialog]');
    if (!dialog || typeof dialog.showModal !== 'function') {
        return; // very old browsers: the links keep working as plain links
    }

    const image = dialog.querySelector('.kc-lightbox-image');
    const text = dialog.querySelector('.kc-lightbox-text');
    const meta = dialog.querySelector('.kc-lightbox-meta');
    const counter = dialog.querySelector('.kc-lightbox-counter');
    const previous = dialog.querySelector('[data-lightbox-prev]');
    const next = dialog.querySelector('[data-lightbox-next]');
    const close = dialog.querySelector('[data-lightbox-close]');

    let items = [];
    let index = 0;
    let opener = null;

    function preload(link) {
        if (link) {
            const picture = new Image();
            picture.srcset = link.dataset.srcset || '';
            picture.sizes = '100vw';
            picture.src = link.href;
        }
    }

    function show(position) {
        index = (position + items.length) % items.length;
        const link = items[index];
        image.removeAttribute('src'); // no flash of the previous photo while the next one loads
        image.srcset = link.dataset.srcset || '';
        image.sizes = '100vw';
        image.src = link.href;
        image.alt = link.dataset.alt || '';
        text.textContent = link.dataset.caption || '';
        meta.textContent = link.dataset.meta || '';
        counter.textContent = items.length > 1 ? `${index + 1} / ${items.length}` : '';
        previous.hidden = next.hidden = items.length < 2;
        if (items.length > 1) {
            preload(items[(index + 1) % items.length]);
            preload(items[(index - 1 + items.length) % items.length]);
        }
    }

    document.addEventListener('click', (event) => {
        const link = event.target.closest('a[data-lightbox-item]');
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return; // a new tab or window was asked for: let the browser open the photo
        }
        const group = link.closest('[data-lightbox]');
        if (!group) {
            return;
        }
        event.preventDefault();
        items = Array.from(group.querySelectorAll('a[data-lightbox-item]'));
        opener = link;
        show(items.indexOf(link));
        dialog.showModal();
    });

    previous.addEventListener('click', () => show(index - 1));
    next.addEventListener('click', () => show(index + 1));
    close.addEventListener('click', () => dialog.close());

    dialog.addEventListener('keydown', (event) => {
        if (event.key === 'ArrowLeft' && items.length > 1) {
            event.preventDefault();
            show(index - 1);
        } else if (event.key === 'ArrowRight' && items.length > 1) {
            event.preventDefault();
            show(index + 1);
        }
    });

    // Back to where the visitor was: focus returns to the photo they opened (keyboard and screen-reader users).
    dialog.addEventListener('close', () => {
        image.removeAttribute('src');
        image.removeAttribute('srcset');
        if (opener) {
            opener.focus();
        }
    });

    // Swipe left/right on phones; a tap on the dark area around the photo closes the viewer.
    let startX = null;
    let swiped = false;
    dialog.addEventListener('pointerdown', (event) => {
        startX = event.clientX;
        swiped = false;
    });
    dialog.addEventListener('pointerup', (event) => {
        if (startX === null) {
            return;
        }
        const distance = event.clientX - startX;
        startX = null;
        if (Math.abs(distance) > 50 && items.length > 1) {
            swiped = true;
            show(index + (distance < 0 ? 1 : -1));
        }
    });
    dialog.addEventListener('click', (event) => {
        if (!swiped && (event.target === dialog || event.target.classList.contains('kc-lightbox-figure'))) {
            dialog.close();
        }
        swiped = false;
    });
})();
