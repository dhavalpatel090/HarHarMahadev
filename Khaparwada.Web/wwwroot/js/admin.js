// Admin panel helpers. Each one is switched on by a data- attribute in the page, and every form also works
// without JavaScript: the server checks everything again.
//   data-slug-from="Name"           fill the web address from the English name (new items only)
//   data-max-bytes / data-max-files / data-preview-target   check file sizes and count before uploading; preview a photo
//   data-icon-filter="grid-id"      search box for the icon picker
//   data-kc-picker                  map for choosing the village centre (needs map.js)
(() => {
    'use strict';

    // Same rules as Infrastructure/Text/SlugGenerator.cs: "Hospitals & Health Centers" → "hospitals-health-centers".
    function slugify(text) {
        const slug = text.normalize('NFD')
            .replace(/\p{Mn}/gu, '')
            .toLowerCase()
            .replace(/[^a-z0-9]/g, '-')
            .replace(/-{2,}/g, '-')
            .replace(/^-+|-+$/g, '');
        return slug.length <= 200 ? slug : slug.slice(0, 200).replace(/-+$/, '');
    }

    // Only while the box is empty or still holds the generated value: on an existing item the address is
    // never changed automatically, because that would break links people have already shared.
    function initSlug(input) {
        const source = document.getElementById(input.dataset.slugFrom);
        if (!source) {
            return;
        }
        let following = input.value === '';
        input.addEventListener('input', () => { following = input.value === ''; });
        source.addEventListener('input', () => {
            if (following) {
                input.value = slugify(source.value);
            }
        });
    }

    // Shows or clears the message under a field, in the same place and style as the server's messages.
    function setFieldError(input, message) {
        const box = input.form && input.form.querySelector(`[data-valmsg-for="${CSS.escape(input.name)}"]`);
        input.classList.toggle('input-validation-error', Boolean(message));
        if (box) {
            box.textContent = message || '';
            box.classList.toggle('field-validation-error', Boolean(message));
            box.classList.toggle('field-validation-valid', !message);
        }
    }

    const megabytes = (bytes) => Number((bytes / 1048576).toFixed(1));

    function initFileInput(input) {
        const preview = input.dataset.previewTarget ? document.getElementById(input.dataset.previewTarget) : null;
        const originalSource = preview ? preview.getAttribute('src') : null;
        // Fields without a saved photo show a "No photo yet" tile instead of the (hidden) preview image.
        const emptyTile = preview ? document.querySelector(`[data-preview-empty="${CSS.escape(preview.id)}"]`) : null;
        let objectUrl = null;

        const showPreview = (source) => {
            if (source) {
                preview.src = source;
            } else {
                preview.removeAttribute('src');
            }
            preview.hidden = !source;
            if (emptyTile) {
                emptyTile.hidden = Boolean(source);
            }
        };

        input.addEventListener('change', () => {
            const files = Array.from(input.files || []);
            const maxBytes = Number(input.dataset.maxBytes || 0);
            const maxFiles = Number(input.dataset.maxFiles || 0);
            const tooBig = maxBytes ? files.find((file) => file.size > maxBytes) : null;

            if (maxFiles && files.length > maxFiles) {
                input.value = '';
                setFieldError(input, `You chose ${files.length} files. Choose at most ${maxFiles} at a time.`);
            } else if (tooBig) {
                input.value = '';
                const name = files.length > 1 ? `“${tooBig.name}” is` : 'This file is';
                setFieldError(input, `${name} ${megabytes(tooBig.size)} MB. The limit is ${megabytes(maxBytes)} MB.`);
            } else {
                setFieldError(input, null);
            }

            if (!preview) {
                return;
            }
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
                objectUrl = null;
            }
            const chosen = input.files && input.files[0];
            if (chosen) {
                objectUrl = URL.createObjectURL(chosen);
                showPreview(objectUrl);
            } else {
                showPreview(originalSource);
            }
        });
    }

    function initIconFilter(search) {
        const grid = document.getElementById(search.dataset.iconFilter);
        if (!grid) {
            return;
        }
        const options = Array.from(grid.querySelectorAll('.kc-icon-option'));
        const none = grid.parentElement.querySelector('.kc-icon-none');

        search.addEventListener('input', () => {
            const words = search.value.trim().toLowerCase().split(/\s+/).filter(Boolean);
            let shown = 0;
            for (const option of options) {
                const matches = words.every((word) => option.dataset.name.includes(word));
                option.hidden = !matches;
                shown += matches ? 1 : 0;
            }
            if (none) {
                none.hidden = shown > 0;
            }
        });

        // Enter in the search box must not submit the whole form.
        search.addEventListener('keydown', (event) => {
            if (event.key === 'Enter') {
                event.preventDefault();
            }
        });

        // Scroll the chosen icon into view inside the grid, without moving the page.
        const checked = grid.querySelector('input:checked');
        if (checked) {
            const option = checked.closest('.kc-icon-option');
            grid.scrollTop += option.getBoundingClientRect().top - grid.getBoundingClientRect().top
                - (grid.clientHeight - option.clientHeight) / 2;
        }
    }

    function initPicker(element) {
        const data = element.dataset;
        const latInput = document.getElementById(data.latInput);
        const lngInput = document.getElementById(data.lngInput);
        const zoomInput = data.zoomInput ? document.getElementById(data.zoomInput) : null; // optional (places have none)
        const loading = element.querySelector('.kc-map-loading');
        const clearButton = document.querySelector(`[data-picker-clear="${element.id}"]`);
        let removePin = () => {};

        // Removing the pin works even if the map itself could not load.
        clearButton?.addEventListener('click', () => {
            latInput.value = '';
            lngInput.value = '';
            removePin();
        });

        if (!window.KcMap) {
            loading?.classList.add('is-error');
            return;
        }

        window.KcMap.loadLeaflet(data.leafletCss, data.leafletJs).then(() => {
            const L = window.L;
            // The wheel zooms only after the map is clicked, so scrolling down the form never gets caught by it.
            const map = L.map(element, { scrollWheelZoom: false });
            L.tileLayer(data.tiles, { maxZoom: Number(data.maxZoom), attribution: data.attribution }).addTo(map);
            map.on('click focus', () => map.scrollWheelZoom.enable());
            map.on('blur', () => map.scrollWheelZoom.disable());

            const icon = window.KcMap.markerIcon(data.icons, { icon: 'location-dot', color: '#7A1531' });
            let pin = null;
            removePin = () => {
                pin?.remove();
                pin = null;
            };

            const readInputs = () => {
                const lat = Number.parseFloat(latInput.value);
                const lng = Number.parseFloat(lngInput.value);
                const valid = Number.isFinite(lat) && Number.isFinite(lng) && Math.abs(lat) <= 90 && Math.abs(lng) <= 180;
                return valid ? L.latLng(lat, lng) : null;
            };

            const zoomForSite = () => String(Math.min(19, Math.max(5, map.getZoom())));

            const revalidate = (input) => {
                if (window.jQuery && window.jQuery.fn.valid && input.form && window.jQuery(input.form).data('validator')) {
                    window.jQuery(input).valid();
                }
            };

            const writeInputs = (latlng) => {
                latInput.value = latlng.lat.toFixed(6);
                lngInput.value = latlng.lng.toFixed(6);
                if (zoomInput) {
                    zoomInput.value = zoomForSite();
                }
                [latInput, lngInput, zoomInput].filter(Boolean).forEach(revalidate);
            };

            const placePin = (latlng) => {
                if (pin) {
                    pin.setLatLng(latlng);
                    return;
                }
                pin = L.marker(latlng, { icon, draggable: true, title: 'Village centre (drag to move)' }).addTo(map);
                pin.on('dragend', () => writeInputs(pin.getLatLng().wrap()));
            };

            const saved = readInputs();
            if (saved) {
                map.setView(saved, Number(zoomInput?.value || data.pinZoom) || 15);
                placePin(saved);
            } else {
                map.setView([Number(data.startLat), Number(data.startLng)], Number(data.startZoom));
            }

            map.on('click', (event) => {
                const latlng = event.latlng.wrap();
                placePin(latlng);
                writeInputs(latlng);
            });

            // The website's map opens at the zoom the admin leaves this map on.
            map.on('zoomend', () => {
                if (pin && zoomInput) {
                    zoomInput.value = zoomForSite();
                }
            });

            // Typed values move the pin and the map.
            const onTyped = () => {
                const latlng = readInputs();
                if (latlng) {
                    placePin(latlng);
                    map.panTo(latlng);
                }
            };
            latInput.addEventListener('change', onTyped);
            lngInput.addEventListener('change', onTyped);
            zoomInput?.addEventListener('change', () => {
                const zoom = Number(zoomInput.value);
                if (zoom >= 5 && zoom <= 19) {
                    map.setZoom(zoom);
                }
            });

            loading?.remove();
        }).catch(() => loading?.classList.add('is-error'));
    }

    // Deferred scripts run before DOMContentLoaded, so by then map.js (loaded after this file) is ready too.
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-slug-from]').forEach(initSlug);
        document.querySelectorAll('input[type="file"][data-max-bytes], input[type="file"][data-preview-target]').forEach(initFileInput);
        document.querySelectorAll('[data-icon-filter]').forEach(initIconFilter);
        document.querySelectorAll('[data-kc-picker]').forEach(initPicker);
    });
})();
