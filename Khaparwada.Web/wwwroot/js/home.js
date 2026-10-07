// Home page: the village map, which loads Leaflet only when it is about to scroll into view
// (most visitors never scroll that far, so most never download it).
(() => {
    'use strict';

    const mapElement = document.querySelector('[data-kc-map]');
    if (mapElement && window.KcMap) {
        const start = () => window.KcMap.show(mapElement);
        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                if (entries.some((entry) => entry.isIntersecting)) {
                    observer.disconnect();
                    start();
                }
            }, { rootMargin: '400px 0px' });
            observer.observe(mapElement);
        } else {
            start();
        }
    }
})();
