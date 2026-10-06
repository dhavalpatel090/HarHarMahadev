// Village map on Leaflet, shared by the Home page, (Phase 5) the full map page and the admin location picker.
// Markers and pop-ups are built with DOM methods and textContent, never innerHTML, so text typed in Admin
// can never inject HTML or script. Colours are set through element.style, which a strict
// Content-Security-Policy allows (inline style attributes it would block).
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

    function popup(spriteUrl, marker, detailsLabel) {
        const root = document.createElement('div');
        root.className = 'kc-map-popup';

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

        const points = markers.map((marker) => {
            const position = [marker.lat, marker.lng];
            window.L.marker(position, { title: marker.name, icon: markerIcon(data.icons, marker) })
                .bindPopup(popup(data.icons, marker, data.detailsLabel))
                .addTo(map);
            return position;
        });

        if (data.lat && data.lng) {
            map.setView([Number(data.lat), Number(data.lng)], Number(data.zoom) || 15);
        } else if (points.length > 0) {
            map.fitBounds(points, { padding: [48, 48], maxZoom: 17 });
        }

        loading?.remove();
        return map;
    }

    return { show, loadLeaflet, markerIcon };
})();
