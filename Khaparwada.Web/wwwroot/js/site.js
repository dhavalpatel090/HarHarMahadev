// Khaparwada Connect: small site-wide behaviours. Plain JavaScript, no dependencies; loaded with "defer".
(() => {
    'use strict';

    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Header: a soft shadow once the page has scrolled.
    const header = document.querySelector('.kc-header');
    if (header) {
        const update = () => header.classList.toggle('is-scrolled', window.scrollY > 8);
        update();
        window.addEventListener('scroll', update, { passive: true });
    }

    // Sections fade in as they scroll into view. Whatever is already on screen is marked visible
    // first, so nothing flickers; without IntersectionObserver everything simply stays visible.
    const revealItems = document.querySelectorAll('[data-reveal]');
    if (revealItems.length > 0 && !reduceMotion && 'IntersectionObserver' in window) {
        revealItems.forEach((item) => {
            if (item.getBoundingClientRect().top < window.innerHeight) {
                item.classList.add('is-visible');
            }
        });
        document.documentElement.classList.add('kc-js');

        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { rootMargin: '0px 0px -8% 0px' });

        revealItems.forEach((item) => {
            if (!item.classList.contains('is-visible')) {
                observer.observe(item);
            }
        });
    }

    // Hero video: never on phones, with Save-Data on, or for visitors who prefer reduced motion.
    const video = document.querySelector('[data-hero-video]');
    if (video) {
        const saveData = navigator.connection && navigator.connection.saveData;
        const smallScreen = window.matchMedia('(max-width: 767.98px)').matches;
        if (saveData || reduceMotion || smallScreen) {
            video.remove();
        } else {
            video.src = video.dataset.src;
            video.play().catch(() => video.remove());
        }
    }
})();
