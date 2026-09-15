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
            script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&callback=${callbackName}&libraries=places&v=weekly&loading=async`;
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
            if (place.formattedAddress) return place.formattedAddress;
            return place.displayName ? `${place.displayName}, nearby` : "";
        } catch {
            return "";
        }
    };

    const resolveReadableAddress = async position => {
        try {
            const response = await geocoder.geocode({ location: position });
            if (response.results?.length) return response.results[0].formatted_address;
        } catch {
            // The demo key can reject the classic geocoder. Try Places next.
        }

        return await findNearestPlace(position);
    };

    const choosePosition = async (position, reverseGeocode = true) => {
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
                const position = marker.getPosition();
                accuracyCircle?.setMap(null);
                accuracyCircle = null;
                void choosePosition({ lat: position.lat(), lng: position.lng() });
            });
        } else {
            marker.setPosition(selectedPosition);
        }

        map.panTo(selectedPosition);
        confirmButton.disabled = false;

        if (!reverseGeocode) {
            return;
        }

        const lookupSequence = ++reverseLookupSequence;
        setBusy(true);
        const resolvedAddress = await resolveReadableAddress(selectedPosition);
        if (lookupSequence !== reverseLookupSequence) return;

        setBusy(false);
        selectedAddress = resolvedAddress || `Pinned location (${formattedCoordinates(selectedPosition)})`;
        addressOutput.textContent = selectedAddress;
        search.value = resolvedAddress || "";
        hint.textContent = resolvedAddress
            ? "Check the pin and drag it if the entrance is not exactly here."
            : "The exact pin will still be saved even though Google has no street address for this point.";
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
            accuracyCircle?.setMap(null);
            accuracyCircle = null;
            void choosePosition({ lat: event.latLng.lat(), lng: event.latLng.lng() });
        });
    };

    const findAddress = () => {
        const query = search.value.trim();
        if (!query || !geocoder) return;

        setBusy(true);
        geocoder.geocode({ address: query, region: "BD" }, (results, status) => {
            setBusy(false);
            if (status !== "OK" || !results?.length) {
                showError("We could not find that address. Try an area name or nearby landmark.");
                return;
            }

            const location = results[0].geometry.location;
            selectedAddress = results[0].formatted_address;
            search.value = selectedAddress;
            addressOutput.textContent = selectedAddress;
            map.setZoom(17);
            void choosePosition({ lat: location.lat(), lng: location.lng() }, false);
        });
    };

    const useCurrentLocation = () => {
        if (!navigator.geolocation) {
            showError("Location is not supported by this browser.");
            return;
        }

        setBusy(true, "Getting a precise location from your device...");
        navigator.geolocation.getCurrentPosition(
            position => {
                setBusy(false);
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
                hint.textContent = `Your device reports accuracy within about ${Math.round(position.coords.accuracy)} metres. You can tap the map or drag the pin to correct it.`;
                void choosePosition(currentPosition);
            },
            error => {
                const message = error.code === 1
                    ? "Location permission was not allowed. Search or tap the map instead."
                    : "Your exact location could not be detected. Search or tap the map instead.";
                showError(message);
            },
            { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
        );
    };

    const closePicker = () => {
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
        reverseLookupSequence++;
        confirmButton.disabled = true;
        addressOutput.textContent = "Search for an address or tap anywhere on the map.";
        hint.textContent = "You can drag the pin to adjust the exact entrance.";
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("location-picker-open");
        setBusy(true, "Loading map...");

        try {
            await loadMaps();
            initializeMap();
            setBusy(false);

            const latitude = Number(activeLatitudeInput.value);
            const longitude = Number(activeLongitudeInput.value);
            if (activeLatitudeInput.value && activeLongitudeInput.value &&
                Number.isFinite(latitude) && Number.isFinite(longitude)) {
                map.setZoom(17);
                void choosePosition({ lat: latitude, lng: longitude });
            } else {
                map.setCenter(defaultCenter);
                map.setZoom(13);
                marker?.setMap(null);
                marker = null;
                accuracyCircle?.setMap(null);
                accuracyCircle = null;
            }
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
    search.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            event.preventDefault();
            findAddress();
        }
    });

    confirmButton.addEventListener("click", () => {
        if (!selectedPosition) return;
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
