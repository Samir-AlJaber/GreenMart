(() => {
    const modal = document.getElementById("locationPickerModal");
    if (!modal || modal.dataset.initialized === "true") return;
    modal.dataset.initialized = "true";

    const mapElement = document.getElementById("locationPickerMap");
    const loading = document.getElementById("locationPickerLoading");
    const title = document.getElementById("locationPickerTitle");
    const search = document.getElementById("locationPickerSearch");
    const searchButton = document.getElementById("locationPickerSearchButton");
    const currentButton = document.getElementById("locationPickerCurrentButton");
    const confirmButton = document.getElementById("locationPickerConfirm");
    const addressOutput = document.getElementById("locationPickerAddress");
    const hint = document.getElementById("locationPickerHint");
    const suggestions = document.getElementById("locationPickerSuggestions");
    const searchStatus = document.getElementById("locationPickerSearchStatus");
    const historyKey = modal.dataset.userId ? `greenmart.locations.${modal.dataset.userId}` : null;
    let searchSequence = 0;
    let searchTimer;
    let sessionToken;
    let gpsWatch;
    let gpsTimer;
    let rows = [];
    let activeRow = -1;
    const cleanAddress = value => (value || "").replace(/\b[23456789CFGHJMPQRVWX]{2,8}\+[23456789CFGHJMPQRVWX]{2,}\b,?\s*/gi, "").trim();
    const recentLocations = () => {
        try {
            const saved = JSON.parse(historyKey ? localStorage.getItem(historyKey) || "[]" : "[]");
            return Array.isArray(saved) ? saved.filter(x => typeof x.address === "string" && Number.isFinite(x.lat) && Number.isFinite(x.lng) && Math.abs(x.lat) <= 90 && Math.abs(x.lng) <= 180).slice(0, 6) : [];
        } catch { return []; }
    };
    const hideSuggestions = () => {
        suggestions.hidden = true;
        search.setAttribute("aria-expanded", "false");
        search.removeAttribute("aria-activedescendant");
        activeRow = -1;
    };
    const stopGps = () => {
        if (gpsWatch !== undefined) navigator.geolocation.clearWatch(gpsWatch);
        gpsWatch = undefined;
        clearTimeout(gpsTimer);
    };
    const defaultCenter = { lat: 23.8103, lng: 90.4125 };

    let map;
    let marker;
    let accuracyCircle;
    let geocoder;
    let mapsLoader;
    let activeAddressInput;
    let activeLatitudeInput;
    let activeLongitudeInput;
    let selectedPosition = null;
    let selectedAddress = "";
    let reverseLookupSequence = 0;

    const setBusy = (isBusy, message = "Finding this address...") => {
        loading.textContent = message;
        loading.hidden = !isBusy;
        searchButton.disabled = isBusy;
        currentButton.disabled = isBusy;
    };

    const loadMaps = () => {
        if (window.google?.maps) return Promise.resolve();
        if (mapsLoader) return mapsLoader;

        mapsLoader = new Promise((resolve, reject) => {
            const apiKey = modal.dataset.apiKey;
            if (!apiKey) {
                reject(new Error("Google Maps API key is missing."));
                return;
            }

            const callbackName = `greenMartLocationPickerReady_${Date.now()}`;
            const script = document.createElement("script");
            window[callbackName] = () => {
                delete window[callbackName];
                resolve();
            };
            script.async = true;
            script.defer = true;
            script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&callback=${callbackName}&libraries=places&v=weekly&loading=async&language=en&region=BD`;
            script.onerror = () => {
                delete window[callbackName];
                reject(new Error("Google Maps could not be loaded."));
            };
            document.head.appendChild(script);
        });

        return mapsLoader;
    };

    const showError = message => {
        addressOutput.textContent = message;
        hint.textContent = "You can close this window and type the address manually.";
        setBusy(false);
    };

    const formattedCoordinates = position =>
        `${position.lat.toFixed(6)}, ${position.lng.toFixed(6)}`;

    const findNearestPlace = async position => {
        try {
            const { Place, SearchNearbyRankPreference } = await google.maps.importLibrary("places");
            const { places } = await Place.searchNearby({
                fields: ["displayName", "formattedAddress", "location"],
                locationRestriction: {
                    center: position,
                    radius: 250
                },
                maxResultCount: 1,
                rankPreference: SearchNearbyRankPreference.DISTANCE,
                language: "en",
                region: "bd"
            });

            if (!places?.length) return "";
            const place = places[0];
            return place.displayName ? `Near ${cleanAddress(place.displayName)} (nearby landmark)` : "";
        } catch {
            return "";
        }
    };

    const resolveReadableAddress = async position => {
        try {
            const response = await geocoder.geocode({ location: position, language: "en", region: "BD" });
            const results = (response.results || []).filter(x => !x.types.includes("plus_code"));
            const preferred = ["street_address", "premise", "route", "neighborhood", "sublocality", "locality"];
            results.sort((a, b) => {
                const rank = x => { const i = preferred.findIndex(t => x.types.includes(t)); return i < 0 ? 99 : i; };
                return rank(a) - rank(b);
            });
            const readable = results.map(x => cleanAddress(x.formatted_address)).find(Boolean);
            if (readable) return readable;
        } catch {
            // The demo key can reject the classic geocoder. Try Places next.
        }

        return await findNearestPlace(position);
    };

    const choosePosition = async (position, reverseGeocode = true, accuracyText = "") => {
        const lookupSequence = ++reverseLookupSequence;
        ++searchSequence;
        clearTimeout(searchTimer);
        hideSuggestions();
        selectedPosition = {
            lat: Number(position.lat),
            lng: Number(position.lng)
        };

        if (!marker) {
            marker = new google.maps.Marker({
                map,
                position: selectedPosition,
                draggable: true,
                animation: google.maps.Animation.DROP,
                title: "Drag to adjust the exact location"
            });
            marker.addListener("dragend", () => {
                stopGps();
                const position = marker.getPosition();
                accuracyCircle?.setMap(null);
                accuracyCircle = null;
                void choosePosition({ lat: position.lat(), lng: position.lng() });
            });
        } else {
            marker.setPosition(selectedPosition);
        }

        map.panTo(selectedPosition);
        confirmButton.disabled = reverseGeocode;

        if (!reverseGeocode) {
            return;
        }

        setBusy(true);
        const resolvedAddress = await resolveReadableAddress(selectedPosition);
        if (lookupSequence !== reverseLookupSequence) return;

        setBusy(false);
        confirmButton.disabled = false;
        selectedAddress = resolvedAddress || `Pinned location (${formattedCoordinates(selectedPosition)})`;
        addressOutput.textContent = selectedAddress;
        search.value = resolvedAddress || "";
        hint.textContent = accuracyText || (resolvedAddress
            ? "Check the pin and drag it if the entrance is not exactly here."
            : "The exact pin will still be saved even though Google has no street address for this point.");
    };

    const initializeMap = () => {
        if (map) {
            google.maps.event.trigger(map, "resize");
            return;
        }

        geocoder = new google.maps.Geocoder();
        map = new google.maps.Map(mapElement, {
            center: defaultCenter,
            zoom: 13,
            fullscreenControl: false,
            mapTypeControl: false,
            streetViewControl: false,
            gestureHandling: "greedy"
        });
        map.addListener("click", event => {
            stopGps();
            accuracyCircle?.setMap(null);
            accuracyCircle = null;
            if (event.placeId) {
                event.stop();
                void selectSuggestion({
                    label: "Selected landmark",
                    prediction: { toPlace: () => new google.maps.places.Place({ id: event.placeId, requestedLanguage: "en" }) }
                });
                return;
            }
            void choosePosition({ lat: event.latLng.lat(), lng: event.latLng.lng() });
        });
    };

    const renderSuggestions = entries => {
        rows = entries;
        suggestions.replaceChildren();
        activeRow = -1;
        search.removeAttribute("aria-activedescendant");
        entries.forEach((entry, i) => {
            const button = document.createElement("button");
            button.type = "button";
            button.id = `location-option-${i}`;
            button.setAttribute("role", "option");
            button.setAttribute("aria-selected", "false");
            button.textContent = entry.label;
            const detail = document.createElement("small");
            detail.textContent = entry.recent ? "Recently selected on this browser" : "Matching location";
            button.appendChild(detail);
            button.addEventListener("click", () => void selectSuggestion(entry));
            suggestions.appendChild(button);
        });
        suggestions.hidden = entries.length === 0;
        search.setAttribute("aria-expanded", String(entries.length > 0));
    };

    const selectSuggestion = async entry => {
        stopGps();
        const sequence = ++searchSequence;
        ++reverseLookupSequence;
        clearTimeout(searchTimer);
        hideSuggestions();
        confirmButton.disabled = true;
        setBusy(true, "Loading selected location...");
        try {
            let position = entry.position;
            let address = entry.label;
            if (entry.prediction) {
                const place = entry.prediction.toPlace();
                await place.fetchFields({ fields: ["location", "displayName", "formattedAddress"] });
                if (!place.location) throw new Error("No coordinates");
                position = { lat: place.location.lat(), lng: place.location.lng() };
                const name = cleanAddress(place.displayName);
                const full = cleanAddress(place.formattedAddress);
                address = name && !full.toLowerCase().includes(name.toLowerCase()) ? `${name}, ${full}` : full || name;
                sessionToken = null;
            }
            if (sequence !== searchSequence) return;
            accuracyCircle?.setMap(null);
            accuracyCircle = null;
            selectedAddress = address;
            search.value = address;
            addressOutput.textContent = address;
            map.setZoom(17);
            await choosePosition(position, false);
            hint.textContent = "Check the pin, then drag it to your entrance if needed.";
            searchStatus.textContent = "";
            setBusy(false);
        } catch {
            if (sequence === searchSequence) showError("This place could not be loaded. Choose another result or tap the map.");
        }
    };

    const findAddress = async () => {
        stopGps();
        ++reverseLookupSequence;
        setBusy(false);
        confirmButton.disabled = true;
        const query = search.value.trim();
        const sequence = ++searchSequence;
        const recent = recentLocations().filter(x => x.address.toLowerCase().includes(query.toLowerCase()))
            .map(x => ({ label: x.address, position: { lat: x.lat, lng: x.lng }, recent: true }));
        renderSuggestions(recent);
        if (!query || !geocoder) { searchStatus.textContent = recent.length ? "Choose a recent location or type to search Bangladesh." : "Type a place name to see matching locations in Bangladesh."; return; }
        searchStatus.textContent = "Finding matching locations...";
        let entries = [];
        let unavailable = false;
        try {
            const { AutocompleteSuggestion, AutocompleteSessionToken } = await google.maps.importLibrary("places");
            sessionToken ||= new AutocompleteSessionToken();
            const response = await AutocompleteSuggestion.fetchAutocompleteSuggestions({
                input: query, includedRegionCodes: ["bd"], language: "en", region: "bd",
                locationBias: { center: map.getCenter().toJSON(), radius: 30000 }, sessionToken
            });
            entries = response.suggestions.filter(x => x.placePrediction).map(x => ({ label: x.placePrediction.text.toString(), prediction: x.placePrediction }));
        } catch {
            unavailable = true;
            try {
                const response = await geocoder.geocode({ address: query, componentRestrictions: { country: "BD" }, language: "en", region: "BD" });
                entries = response.results.map(x => ({ label: cleanAddress(x.formatted_address), position: x.geometry.location.toJSON() }));
            } catch { /* Show recent entries and a useful status without moving the pin. */ }
        }
        if (sequence !== searchSequence) return;
        renderSuggestions([...recent, ...entries].slice(0, 10));
        searchStatus.textContent = entries.length ? "Choose the matching location below." : unavailable
            ? "Place search is unavailable. Try a complete address, choose a recent location, or tap the map."
            : "No matching places. Add a road, area or city to your search.";
    };

    const useCurrentLocation = () => {
        if (!navigator.geolocation) {
            showError("Location is not supported by this browser.");
            return;
        }

        stopGps();
        ++reverseLookupSequence;
        ++searchSequence;
        clearTimeout(searchTimer);
        hideSuggestions();
        confirmButton.disabled = true;
        let bestPosition;
        setBusy(true, "Improving your device location (up to 12 seconds)...");
        const finish = () => {
                stopGps();
                if (!bestPosition) {
                    showError("Your location could not be detected. Search for an address or tap the map.");
                    return;
                }
                const position = bestPosition;
                const currentPosition = {
                    lat: position.coords.latitude,
                    lng: position.coords.longitude
                };

                if (accuracyCircle) accuracyCircle.setMap(null);
                accuracyCircle = new google.maps.Circle({
                    map,
                    center: currentPosition,
                    radius: Math.max(position.coords.accuracy || 0, 8),
                    fillColor: "#2563eb",
                    fillOpacity: .1,
                    strokeColor: "#2563eb",
                    strokeOpacity: .35,
                    strokeWeight: 1
                });

                map.setZoom(position.coords.accuracy > 500 ? 15 : position.coords.accuracy > 100 ? 16 : 18);
                const accuracyText = `Device accuracy: about ${Math.round(position.coords.accuracy)} metres. Check the blue circle and drag the pin to your entrance. The address describes the pinned area.`;
                void choosePosition(currentPosition, true, accuracyText);
        };
        gpsTimer = setTimeout(finish, 12000);
        gpsWatch = navigator.geolocation.watchPosition(
            position => {
                if (!bestPosition || position.coords.accuracy < bestPosition.coords.accuracy) bestPosition = position;
                if (bestPosition.coords.accuracy <= 30) finish();
            },
            error => {
                if (bestPosition) { finish(); return; }
                stopGps();
                const message = error.code === 1
                    ? "Location permission was not allowed. Search or tap the map instead."
                    : "Your exact location could not be detected. Search or tap the map instead.";
                showError(message);
            },
            { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
        );
    };

    const closePicker = () => {
        stopGps();
        clearTimeout(searchTimer);
        ++searchSequence;
        ++reverseLookupSequence;
        hideSuggestions();
        sessionToken = null;
        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("location-picker-open");
    };

    const openPicker = async trigger => {
        activeAddressInput = document.querySelector(trigger.dataset.addressTarget);
        activeLatitudeInput = document.querySelector(trigger.dataset.latitudeTarget);
        activeLongitudeInput = document.querySelector(trigger.dataset.longitudeTarget);
        if (!activeAddressInput || !activeLatitudeInput || !activeLongitudeInput) return;

        title.textContent = trigger.dataset.pickerTitle || "Choose location";
        search.value = activeAddressInput.value.trim();
        selectedAddress = "";
        selectedPosition = null;
        const openSequence = ++reverseLookupSequence;
        confirmButton.disabled = true;
        addressOutput.textContent = "Search for an address or tap anywhere on the map.";
        hint.textContent = "You can drag the pin to adjust the exact entrance.";
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("location-picker-open");
        setBusy(true, "Loading map...");

        try {
            await loadMaps();
            if (openSequence !== reverseLookupSequence || !modal.classList.contains("is-open")) return;
            initializeMap();
            setBusy(false);

            const latitude = Number(activeLatitudeInput.value);
            const longitude = Number(activeLongitudeInput.value);
            if (activeLatitudeInput.value && activeLongitudeInput.value &&
                Number.isFinite(latitude) && Number.isFinite(longitude)) {
                map.setZoom(17);
                selectedAddress = cleanAddress(activeAddressInput.value);
                addressOutput.textContent = selectedAddress;
                void choosePosition({ lat: latitude, lng: longitude }, !selectedAddress);
            } else {
                map.setCenter(defaultCenter);
                map.setZoom(13);
                marker?.setMap(null);
                marker = null;
                accuracyCircle?.setMap(null);
                accuracyCircle = null;
            }
            searchStatus.textContent = "Type a place name to see matches, or choose a recent location.";
        } catch {
            showError("Google Maps could not load. Check the API key and Maps JavaScript API settings.");
        }
    };

    document.addEventListener("click", event => {
        const trigger = event.target.closest(".js-location-picker");
        if (trigger) openPicker(trigger);
    });

    searchButton.addEventListener("click", findAddress);
    currentButton.addEventListener("click", useCurrentLocation);
    search.addEventListener("focus", () => {
        const recent = recentLocations().map(x => ({ label: x.address, position: { lat: x.lat, lng: x.lng }, recent: true }));
        renderSuggestions(recent);
    });
    search.addEventListener("input", () => {
        stopGps();
        ++searchSequence;
        ++reverseLookupSequence;
        setBusy(false);
        confirmButton.disabled = true;
        hideSuggestions();
        clearTimeout(searchTimer);
        searchTimer = setTimeout(findAddress, 300);
    });
    search.addEventListener("keydown", event => {
        if ((event.key === "ArrowDown" || event.key === "ArrowUp") && !suggestions.hidden && rows.length) {
            event.preventDefault();
            activeRow = (activeRow + (event.key === "ArrowDown" ? 1 : -1) + rows.length) % rows.length;
            Array.from(suggestions.children).forEach((button, index) => button.setAttribute("aria-selected", String(index === activeRow)));
            search.setAttribute("aria-activedescendant", `location-option-${activeRow}`);
            suggestions.children[activeRow].scrollIntoView({ block: "nearest" });
        }
        if (event.key === "Enter") {
            event.preventDefault();
            clearTimeout(searchTimer);
            if (activeRow >= 0 && !suggestions.hidden) void selectSuggestion(rows[activeRow]);
            else void findAddress();
        }
    });

    confirmButton.addEventListener("click", () => {
        if (!selectedPosition || confirmButton.disabled) return;
        if (historyKey) {
            try {
                const previous = recentLocations().filter(x => Math.abs(x.lat - selectedPosition.lat) > .00005 || Math.abs(x.lng - selectedPosition.lng) > .00005);
                localStorage.setItem(historyKey, JSON.stringify([{ ...selectedPosition, address: selectedAddress }, ...previous].slice(0, 6)));
            } catch { /* Location selection works even when browser storage is unavailable. */ }
        }
        activeAddressInput.value = selectedAddress || formattedCoordinates(selectedPosition);
        activeLatitudeInput.value = selectedPosition.lat.toFixed(6);
        activeLongitudeInput.value = selectedPosition.lng.toFixed(6);
        activeAddressInput.dispatchEvent(new Event("input", { bubbles: true }));
        activeAddressInput.dispatchEvent(new Event("change", { bubbles: true }));
        closePicker();
    });

    modal.querySelector(".location-picker-close").addEventListener("click", closePicker);
    modal.querySelector(".location-picker-cancel").addEventListener("click", closePicker);
    modal.addEventListener("click", event => {
        if (event.target === modal) closePicker();
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && modal.classList.contains("is-open")) closePicker();
    });
})();
