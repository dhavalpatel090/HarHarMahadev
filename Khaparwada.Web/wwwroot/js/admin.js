// Admin panel helpers. Each one is switched on by a data- attribute in the page, and every form also works
// without JavaScript: the server checks everything again.
//   data-slug-from="Name"           fill the web address from the English name (new items only);
//                                   data-slug-year-from="StartDate" adds the year from a date box
//   data-hidden-by="IsAllDay"       hide while a tick box is ticked
//   form data-upload-each           send several chosen photos one per request, with progress
//   data-check-all="Selected"       a "select all" tick box for the tick boxes of that name
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
    // With data-slug-year-from (a date box), the year is added unless the name has it already, as the server does
    // (EventAdminService.DefaultSlug): "navratri-garba-night-2026".
    function initSlug(input) {
        const source = document.getElementById(input.dataset.slugFrom);
        if (!source) {
            return;
        }
        const yearSource = input.dataset.slugYearFrom ? document.getElementById(input.dataset.slugYearFrom) : null;
        let following = input.value === '';
        const update = () => {
            if (!following) {
                return;
            }
            let slug = slugify(source.value);
            const year = yearSource && /^\d{4}-/.test(yearSource.value) ? yearSource.value.slice(0, 4) : '';
            if (slug && year && !slug.split('-').includes(year)) {
                slug = `${slug}-${year}`;
            }
            input.value = slug;
        };
        input.addEventListener('input', () => { following = input.value === ''; });
        source.addEventListener('input', update);
        yearSource?.addEventListener('change', update);
    }

    // data-hidden-by="IsAllDay": hidden while that tick box is ticked (an all-day event's times). The server
    // ignores those fields then, so hiding them only removes clutter.
    function initHiddenBy(element) {
        const box = document.getElementById(element.dataset.hiddenBy);
        if (!box) {
            return;
        }
        const sync = () => { element.hidden = box.checked; };
        box.addEventListener('change', sync);
        sync();
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

        // In a data-upload-each form one oversized photo is skipped (and reported), not the whole selection.
        const uploadsEach = Boolean(input.form && input.form.hasAttribute('data-upload-each'));

        input.addEventListener('change', () => {
            const files = Array.from(input.files || []);
            const maxBytes = Number(input.dataset.maxBytes || 0);
            const maxFiles = Number(input.dataset.maxFiles || 0);
            const tooBig = maxBytes ? files.find((file) => file.size > maxBytes) : null;

            if (maxFiles && files.length > maxFiles) {
                input.value = '';
                setFieldError(input, `You chose ${files.length} files. Choose at most ${maxFiles} at a time.`);
            } else if (tooBig && uploadsEach && files.length > 1) {
                setFieldError(input, `“${tooBig.name}” is ${megabytes(tooBig.size)} MB, over the ${megabytes(maxBytes)} MB limit. It will be skipped.`);
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

    // data-upload-each on an upload form: several photos go one per request (fetch, "Accept: application/json"),
    // with a progress bar. Every request stays small (a whole festival would otherwise be one request of hundreds of
    // MB, refused by the server) and one refused file does not stop the others. When all succeed the page reloads
    // with ?added=n, which shows the usual message. A single photo goes as a normal form post.
    function initUploadEach(form) {
        const input = form.querySelector('input[type="file"]');
        const status = form.querySelector('[data-upload-status]');
        const token = form.querySelector('input[name="__RequestVerificationToken"]');
        const button = form.querySelector('button[type="submit"]');
        if (!input || !status || !token || !button || !window.fetch) {
            return;
        }
        const bar = status.querySelector('progress');
        const text = status.querySelector('.kc-upload-progress-text');
        const errors = status.querySelector('.kc-upload-errors');
        const done = status.querySelector('[data-upload-done]');
        const maxBytes = Number(input.dataset.maxBytes || 0);
        let busy = false;

        const report = (message) => {
            const item = document.createElement('li');
            item.textContent = message;
            errors.append(item);
        };

        form.addEventListener('submit', async (event) => {
            const files = Array.from(input.files || []);
            if (files.length < 2) {
                return;
            }
            event.preventDefault();
            if (busy) {
                return;
            }
            busy = true;
            button.disabled = true;
            input.disabled = true;
            status.hidden = false;
            errors.replaceChildren();
            done.hidden = true;
            bar.max = files.length;
            bar.value = 0;

            let added = 0;
            let signedOut = false;
            for (const [position, file] of files.entries()) {
                text.textContent = `Uploading ${position + 1} of ${files.length}: ${file.name}`;
                if (maxBytes && file.size > maxBytes) {
                    report(`${file.name}: ${megabytes(file.size)} MB, over the ${megabytes(maxBytes)} MB limit.`);
                } else if (signedOut) {
                    report(`${file.name}: not sent.`);
                } else {
                    const data = new FormData();
                    data.append('__RequestVerificationToken', token.value);
                    data.append(input.name, file, file.name);
                    try {
                        const response = await fetch(form.action, {
                            method: 'POST',
                            body: data,
                            headers: { Accept: 'application/json' },
                            credentials: 'same-origin'
                        });
                        const isJson = (response.headers.get('content-type') || '').includes('application/json');
                        if (response.ok && isJson) {
                            const result = await response.json();
                            added += result.added || 0;
                            if (result.error) {
                                report(result.error);
                            }
                        } else if (response.redirected || (response.ok && !isJson)) {
                            // The sign-in has expired: the server sent the sign-in page instead.
                            signedOut = true;
                            report(`${file.name}: you were signed out. Sign in again, then upload the rest.`);
                        } else {
                            report(`${file.name}: ${response.status === 413 ? 'the file is too large.' : `the server answered ${response.status}.`}`);
                        }
                    } catch {
                        report(`${file.name}: the connection failed. Check the internet connection and try again.`);
                    }
                }
                bar.value = position + 1;
            }

            setFieldError(input, null); // a "will be skipped" warning is now in the list below
            const next = window.location.pathname + (added > 0 ? `?added=${added}` : '');
            if (errors.children.length === 0) {
                window.location.assign(next);
                return;
            }
            text.textContent = `${added} of ${files.length} photos uploaded. These were not:`;
            done.href = next;
            done.hidden = false;
            input.value = '';
            input.disabled = false;
            button.disabled = false;
            busy = false;
        });
    }

    // data-check-all="Selected": ticks or clears every tick box of that name in the same form.
    function initCheckAll(box) {
        box.addEventListener('change', () => {
            box.form?.querySelectorAll(`input[type="checkbox"][name="${CSS.escape(box.dataset.checkAll)}"]`)
                .forEach((item) => { item.checked = box.checked; });
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
        document.querySelectorAll('[data-hidden-by]').forEach(initHiddenBy);
        document.querySelectorAll('form[data-upload-each]').forEach(initUploadEach);
        document.querySelectorAll('[data-check-all]').forEach(initCheckAll);
        document.querySelectorAll('input[type="file"][data-max-bytes], input[type="file"][data-preview-target]').forEach(initFileInput);
        document.querySelectorAll('[data-icon-filter]').forEach(initIconFilter);
        document.querySelectorAll('[data-kc-picker]').forEach(initPicker);
    });
})();
