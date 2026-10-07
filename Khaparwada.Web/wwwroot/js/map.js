// Village maps on Leaflet: Home, the /map page, each place's page, and (through loadLeaflet and markerIcon) the
// admin location picker. Every element with data-kc-map starts by itself: "eager" at once, "lazy" when it
// scrolls near the screen, so visitors who never reach a map never download Leaflet (about 40 KB compressed).
// Markers and pop-ups are built with DOM methods and textContent, never innerHTML, so text typed in Admin can
// never inject HTML or script. Colours are set through element.style, which a strict Content-Security-Policy
// allows (inline style attributes it would block).
window.KcMap = (() => {
    'use strict';

    const SVG_NS = 'http://www.w3.org/2000/svg';
    let leafletLoading = null;

    function loadFile(tag, attributes) {
        return new Promise((resolve, reject) => {
            const element = Object.assign(document.createElement(tag), attributes);
            element.onload = () => resolve();
            element.onerror = () => reject(new Error(`Could not load ${attributes.src || attributes.href}`));
            document.head.appendChild(element);
        });
    }

    // Leaflet is downloaded only when a map is actually shown, and only once per page.
    function loadLeaflet(cssUrl, jsUrl) {
        if (window.L) {
            return Promise.resolve();
        }
        leafletLoading ??= Promise.all([
            loadFile('link', { rel: 'stylesheet', href: cssUrl }),
            loadFile('script', { src: jsUrl })
        ]);
        return leafletLoading;
    }

    function icon(spriteUrl, name, color) {
        const svg = document.createElementNS(SVG_NS, 'svg');
        svg.setAttribute('class', 'icon');
        svg.setAttribute('aria-hidden', 'true');
        if (color) {
            svg.setAttribute('color', color);
        }
        const use = document.createElementNS(SVG_NS, 'use');
        use.setAttribute('href', `${spriteUrl}#${name}`);
        svg.appendChild(use);
        return svg;
    }

    function pin(spriteUrl, marker) {
        const element = document.createElement('span');
        element.className = 'kc-pin';
        element.style.setProperty('--pin', marker.color);
        element.appendChild(icon(spriteUrl, marker.icon));
        return element;
    }

    // The same pin everywhere: on the public maps and in the admin location picker.
    function markerIcon(spriteUrl, marker) {
        return window.L.divIcon({
            html: pin(spriteUrl, marker),
            className: 'kc-pin-wrap',
            iconSize: [36, 36],
            iconAnchor: [18, 43],
            popupAnchor: [0, -40]
        });
    }

    // Name, category, short description, photo and a "View details" link.
    function popup(spriteUrl, marker, detailsLabel) {
        const root = document.createElement('div');
        root.className = 'kc-map-popup';

        if (marker.image) {
            const image = document.createElement('img');
            image.className = 'kc-map-popup-image';
            image.src = marker.image;
            image.alt = '';
            image.width = 240;
            image.height = 135;
            image.loading = 'lazy';
            root.append(image);
        }

        const category = document.createElement('span');
        category.className = 'kc-map-popup-category';
        category.append(icon(spriteUrl, marker.icon, marker.color), document.createTextNode(marker.category));

        const title = document.createElement('p');
        title.className = 'kc-map-popup-title';
        title.textContent = marker.name;

        const text = document.createElement('p');
        text.className = 'kc-map-popup-text';
        text.textContent = marker.summary;

        const link = document.createElement('a');
        link.className = 'kc-map-popup-link';
        link.href = marker.url;
        link.textContent = detailsLabel;

        root.append(category, title, text, link);
        return root;
    }

    // Category buttons for this map (data-map-for="<map id>"): each shows or hides its category's markers.
    // The bar stays hidden without JavaScript, where it could not work.
    function wireFilters(element, map, layers, points) {
        const buttons = Array.from(document.querySelectorAll(`[data-map-for="${CSS.escape(element.id)}"] [data-map-filter]`));
        if (buttons.length === 0) {
            return;
        }

        const apply = (button) => {
            const layer = layers[button.dataset.mapFilter];
            if (!layer) {
                return;
            }
            if (button.getAttribute('aria-pressed') === 'true') {
                layer.addTo(map);
            } else {
                layer.remove();
            }
        };

        buttons.forEach((button) => {
            apply(button);
            button.addEventListener('click', () => {
                button.setAttribute('aria-pressed', button.getAttribute('aria-pressed') === 'true' ? 'false' : 'true');
                apply(button);
            });
        });

        document.querySelectorAll(`[data-map-for="${CSS.escape(element.id)}"] [data-map-filter-all]`).forEach((all) => {
            all.addEventListener('click', () => {
                buttons.forEach((button) => {
                    button.setAttribute('aria-pressed', 'true');
                    apply(button);
                });
                if (points.length > 0) {
                    map.fitBounds(points, { padding: [48, 48], maxZoom: 17 });
                }
            });
        });

        document.querySelectorAll(`[data-map-for="${CSS.escape(element.id)}"]`).forEach((bar) => { bar.hidden = false; });
    }

    async function show(element) {
        const data = element.dataset;
        const loading = element.querySelector('.kc-map-loading');

        try {
            await loadLeaflet(data.leafletCss, data.leafletJs);
        } catch {
            loading?.classList.add('is-error');
            return null;
        }

        const markers = JSON.parse(document.getElementById(data.markers).textContent);

        // Scroll-wheel zoom stays off so scrolling down the page never gets caught by the map.
        const map = window.L.map(element, { scrollWheelZoom: false });
        window.L.tileLayer(data.tiles, { maxZoom: Number(data.maxZoom), attribution: data.attribution }).addTo(map);

        // One layer per category, so the map page can show or hide a category in one step.
        const layers = {};
        const points = markers.map((marker) => {
            const position = [marker.lat, marker.lng];
            const pinMarker = window.L.marker(position, { title: marker.name, icon: markerIcon(data.icons, marker) });
            if (data.popups !== 'false') {
                pinMarker.bindPopup(popup(data.icons, marker, data.detailsLabel));
            }
            layers[marker.cat] ??= window.L.layerGroup().addTo(map);
            layers[marker.cat].addLayer(pinMarker);
            return position;
        });

        // With one category chosen (/map?category=temples), frame that category's places.
        const chosen = data.category ? markers.filter((marker) => marker.cat === data.category).map((m) => [m.lat, m.lng]) : [];
        if (chosen.length > 0) {
            map.fitBounds(chosen, { padding: [48, 48], maxZoom: 17 });
        } else if (data.lat && data.lng) {
            map.setView([Number(data.lat), Number(data.lng)], Number(data.zoom) || 15);
        } else if (points.length > 0) {
            map.fitBounds(points, { padding: [48, 48], maxZoom: 17 });
        }

        wireFilters(element, map, layers, points);
        loading?.remove();
        return map;
    }

    function start(element) {
        if (element.dataset.kcMap === 'eager' || !('IntersectionObserver' in window)) {
            show(element);
            return;
        }
        const observer = new IntersectionObserver((entries) => {
            if (entries.some((entry) => entry.isIntersecting)) {
                observer.disconnect();
                show(element);
            }
        }, { rootMargin: '400px 0px' });
        observer.observe(element);
    }

    document.addEventListener('DOMContentLoaded', () => document.querySelectorAll('[data-kc-map]').forEach(start));

    return { show, loadLeaflet, markerIcon };
})();
