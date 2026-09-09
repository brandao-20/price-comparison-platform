window.mapsInterop = window.mapsInterop || {};


// Função para carregar a API do Google Maps e o MarkerClusterer
window.mapsInterop.loadGoogleMaps = function (apiKey) {

    // Carrega o script do Google Maps
    if (!document.querySelector('script[src*="maps.googleapis.com"]')) {
        const googleMapsScript = document.createElement('script');
        googleMapsScript.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&callback=initMap&v=quarterly&libraries=places,marker&loading=async`;
        googleMapsScript.async = true;
        googleMapsScript.defer = true;
        googleMapsScript.onerror = function () {
            console.error("[ERROR] Falha ao carregar o script do Google Maps.");
        };
        document.head.appendChild(googleMapsScript);
    }

    // Carrega o script do MarkerClusterer com uma versão específica
    if (!document.querySelector('script[src*="markerclusterer"]')) {
        const clusterScript = document.createElement('script');
        clusterScript.src = "https://unpkg.com/@googlemaps/markerclusterer@2.0.15/dist/index.min.js";
        clusterScript.async = true;
        clusterScript.defer = true;
        clusterScript.onerror = function () {
            console.error("[ERROR] Falha ao carregar o script do MarkerClusterer.");
        };
        clusterScript.onload = function () {
            window.markerClustererLoaded = true;
        };
        document.head.appendChild(clusterScript);
    } else {
        window.markerClustererLoaded = true;
    }
};

// Inicializa o Google Maps quando o script é carregado
window.initMap = function () {
    window.googleMapsLoaded = true;
};

window.mapsInterop.getUserLocation = async function () {
    return new Promise((resolve, reject) => {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        lat: position.coords.latitude,
                        lng: position.coords.longitude
                    });
                },
                (error) => {
                    console.error("[ERROR] Erro ao obter geolocalização:", error.message);
                    reject(new Error("Erro ao obter geolocalização: " + error.message));
                },
                { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
            );
        } else {
            console.error("[ERROR] Geolocalização não é suportada pelo navegador.");
            reject(new Error("Geolocalização não é suportada pelo navegador."));
        }
    });
};

// Função para aguardar o carregamento do MarkerClusterer
async function waitForMarkerClusterer(maxAttempts = 100, delayMs = 100) {
    let attempts = 0;
    while (!window.markerClustererLoaded && attempts < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, delayMs));
        attempts++;
    }
    if (!window.markerClustererLoaded) {
        console.warn("[WARN] MarkerClusterer não carregado após aguardar. Prosseguindo sem clustering.");
        return false;
    }
    return true;
}

// Lista de cidades no NORTE de Portugal para busca de supermercados
const cidadesPortugal = [
    { name: "Porto", lat: 41.1496, lng: -8.6110 },
    { name: "Braga", lat: 41.5454, lng: -8.4265 },
    { name: "Viana do Castelo", lat: 41.6918, lng: -8.8345 },
    { name: "Ponte de Lima", lat: 41.7670, lng: -8.5839 },
    { name: "Guimarães", lat: 41.4440, lng: -8.2962 },
    { name: "Vila Nova de Famalicão", lat: 41.4080, lng: -8.5196 },
    { name: "Barcelos", lat: 41.5317, lng: -8.6182 },
    { name: "Chaves", lat: 41.7402, lng: -7.4710 },
    { name: "Vila Real", lat: 41.2958, lng: -7.7462 },
    { name: "Mirandela", lat: 41.4854, lng: -7.1809 }
];

window.mapsInterop.findNearbySupermarkets = async function (lat, lng, elementId, dotNetRef) {
    if (!window.google || !window.google.maps) {
        console.error("[ERROR] Google Maps API não carregado ou chave inválida.");
        return;
    }

    const userLocation = new google.maps.LatLng(lat, lng);
    const mapElement = document.getElementById(elementId);
    if (!mapElement) {
        console.error("[ERROR] Elemento do mapa não encontrado:", elementId);
        return;
    }

    const map = new google.maps.Map(mapElement, {
        zoom: 12,
        center: userLocation,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false,
        mapId: "7af9339427e7484e" // Map ID configurado
    });


    const service = new google.maps.places.PlacesService(map);
    let allSupermarkets = [];

    // Função para buscar supermercados em uma localização específica
    async function searchSupermarkets(location, radius, keywords) {
        return new Promise((resolve) => {
            const request = {
                location: location,
                radius: radius,
                keyword: keywords
            };

            service.nearbySearch(request, (results, status) => {
                if (status === google.maps.places.PlacesServiceStatus.OK) {
                    const filteredResults = results.filter(place => {
                        const types = place.types || [];
                        const isRelevant = types.some(type =>
                            type === "supermarket" ||
                            type === "grocery_or_supermarket" ||
                            type === "convenience_store" ||
                            type === "food" ||
                            type === "store"
                        ) && !types.includes("gas_station");
                        return isRelevant;
                    });
                    resolve(filteredResults);
                } else {
                    console.error("[ERROR] Falha ao buscar supermercados:", status);
                    resolve([]);
                }
            });
        });
    }

    // Busca local (próxima ao usuário) com raio de 15 km
    const localSupermarkets = await searchSupermarkets(
        userLocation,
        15000,
        "supermercado OR mercearia OR minimercado OR mercado OR loja de conveniência OR alimentos OR groceries OR continente OR pingo doce OR lidl OR aldi OR intermarché"
    );
    allSupermarkets = allSupermarkets.concat(localSupermarkets);

    // Busca em cidades do norte de Portugal (raio de 10 km por cidade)
    for (const cidade of cidadesPortugal) {
        const cidadeLocation = new google.maps.LatLng(cidade.lat, cidade.lng);
        const cidadeSupermarkets = await searchSupermarkets(
            cidadeLocation,
            10000,
            "supermercado OR mercearia OR minimercado OR mercado OR loja de conveniência OR alimentos OR groceries OR continente OR pingo doce OR lidl OR aldi OR intermarché"
        );
        allSupermarkets = allSupermarkets.concat(cidadeSupermarkets);
    }

    // Remove duplicatas com base no place_id
    const uniqueSupermarkets = Array.from(new Map(allSupermarkets.map(place => [place.place_id, place])).values());

    // Mapeia os resultados para o formato desejado
    const supermarkets = uniqueSupermarkets.map(place => ({
        name: place.name,
        placeId: place.place_id,
        address: place.vicinity,
        lat: place.geometry.location.lat(),
        lng: place.geometry.location.lng()
    }));

    // Limpa marcadores anteriores
    if (window.currentMarkers) {
        window.currentMarkers.forEach(marker => marker.setMap(null));
    }
    window.currentMarkers = [];

    // Verifica se AdvancedMarkerElement está disponível
    const useAdvancedMarkers = window.google.maps.marker && window.google.maps.marker.AdvancedMarkerElement;

    // Cria os marcadores
    const markers = supermarkets.map((supermarket, index) => {
        if (useAdvancedMarkers) {
            const marker = new google.maps.marker.AdvancedMarkerElement({
                position: { lat: supermarket.lat, lng: supermarket.lng },
                map: map,
                title: supermarket.name,
                content: createCustomMarkerIcon()
            });
            return marker;
        } else {
            console.warn("[WARN] Usando marcadores padrão como fallback devido à falta de AdvancedMarkerElement.");
            const marker = new google.maps.Marker({
                position: { lat: supermarket.lat, lng: supermarket.lng },
                map: map,
                title: supermarket.name,
                icon: {
                    url: "https://maps.google.com/mapfiles/ms/icons/red-dot.png"
                }
            });
            return marker;
        }
    });

    // Adiciona clustering para evitar sobreposição
    const markerClustererAvailable = await waitForMarkerClusterer();
    if (markerClustererAvailable && typeof MarkerClusterer !== "undefined") {
        const markerCluster = new MarkerClusterer({
            map: map,
            markers: markers,
            renderer: {
                render: ({ count, position }) => {
                    if (useAdvancedMarkers) {
                        const clusterMarker = new google.maps.marker.AdvancedMarkerElement({
                            position: position,
                            map: map,
                            content: createClusterIcon(count)
                        });
                        return clusterMarker;
                    } else {
                        const clusterMarker = new google.maps.Marker({
                            position: position,
                            map: map,
                            icon: {
                                url: "https://maps.google.com/mapfiles/ms/icons/yellow-dot.png",
                                labelOrigin: new google.maps.Point(16, 16)
                            },
                            label: {
                                text: String(count),
                                color: "black",
                                fontSize: "12px",
                                fontWeight: "bold"
                            }
                        });
                        return clusterMarker;
                    }
                }
            }
        });
    } else {
        console.warn("[WARN] MarkerClusterer não disponível, marcadores exibidos sem clustering.");
        markers.forEach((marker, index) => {
            marker.setMap(map);
        });
    }

    // Adiciona infowindows aos marcadores
    let currentInfoWindow = null;
    markers.forEach((marker, index) => {
        const supermarket = supermarkets[index];
        if (!supermarket) {
            console.warn(`[WARN] Supermercado não encontrado para o marcador no índice ${index}`);
            return;
        }
        // Treat provider/store text as text, not HTML or JavaScript.
        const content = document.createElement("div");
        content.style.fontFamily = "Arial, sans-serif";
        const title = document.createElement("h5");
        title.style.cssText = "margin: 0 0 5px 0; font-size: 16px;";
        title.textContent = supermarket.name;
        const address = document.createElement("p");
        address.style.cssText = "margin: 0 0 10px 0; font-size: 14px; color: #555;";
        address.textContent = supermarket.address;
        const select = document.createElement("button");
        select.style.cssText = "background-color: #1a73e8; color: white; border: none; padding: 6px 12px; border-radius: 4px; cursor: pointer; font-size: 14px; box-shadow: 0 2px 4px rgba(0,0,0,0.2); transition: background-color 0.3s;";
        select.textContent = "Select";
        select.addEventListener("click", () => window.selectSupermarket(supermarket.placeId));
        content.append(title, address, select);
        const infowindow = new google.maps.InfoWindow({ content });

        marker.addListener("click", () => {
            if (currentInfoWindow) {
                currentInfoWindow.close();
            }
            infowindow.open(map, marker);
            currentInfoWindow = infowindow;
        });

        window.currentMarkers.push(marker);
    });

    window.selectSupermarket = function (placeId) {
        const supermarket = supermarkets.find(s => s.placeId === placeId);
        if (supermarket) {
            dotNetRef.invokeMethodAsync("SelectSupermarket", supermarket);
        } else {
            console.warn("[WARN] Supermercado com placeId não encontrado:", placeId);
        }
    };

    // Ajusta o zoom para mostrar todos os marcadores, mas foca na localização do usuário
    if (supermarkets.length > 0) {
        const bounds = new google.maps.LatLngBounds();
        supermarkets.forEach(supermarket => {
            bounds.extend(new google.maps.LatLng(supermarket.lat, supermarket.lng));
        });
        map.fitBounds(bounds);

        // Garante que o zoom inicial foca na localização do usuário
        setTimeout(() => {
            map.setCenter(userLocation);
            map.setZoom(12);
        }, 1000);
    } else {
        console.warn("[WARN] Nenhum supermercado encontrado para exibir no mapa.");
    }
};

// Função para criar ícone personalizado para os marcadores
function createCustomMarkerIcon() {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("width", "40");
    svg.setAttribute("height", "40");
    svg.setAttribute("viewBox", "0 0 40 40");

    const circle = document.createElementNS("http://www.w3.org/2000/svg", "circle");
    circle.setAttribute("cx", "20");
    circle.setAttribute("cy", "20");
    circle.setAttribute("r", "20");
    circle.setAttribute("fill", "#a3cffa");
    circle.setAttribute("fill-opacity", "0.9");
    svg.appendChild(circle);

    const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
    text.setAttribute("x", "50%");
    text.setAttribute("y", "50%");
    text.setAttribute("font-size", "28");
    text.setAttribute("fill", "#ffffff");
    text.setAttribute("dominant-baseline", "middle");
    text.setAttribute("text-anchor", "middle");
    text.textContent = "🛒";
    svg.appendChild(text);

    return svg;
}

// Função para criar ícone personalizado para clusters
function createClusterIcon(count) {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("width", "40");
    svg.setAttribute("height", "40");
    svg.setAttribute("viewBox", "0 0 40 40");

    const circle = document.createElementNS("http://www.w3.org/2000/svg", "circle");
    circle.setAttribute("cx", "20");
    circle.setAttribute("cy", "20");
    circle.setAttribute("r", "20");
    circle.setAttribute("fill", "#ffeb3b");
    svg.appendChild(circle);

    const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
    text.setAttribute("x", "50%");
    text.setAttribute("y", "50%");
    text.setAttribute("font-size", "16");
    text.setAttribute("fill", "#ffffff");
    text.setAttribute("dominant-baseline", "middle");
    text.setAttribute("text-anchor", "middle");
    text.textContent = String(count);
    svg.appendChild(text);

    return svg;
}

window.mapsInterop.focusOnSupermarket = function (elementId, lat, lng) {
    const mapElement = document.getElementById(elementId);
    if (!mapElement || !window.google || !window.google.maps) {
        console.error("[ERROR] Mapa ou Google Maps API não encontrados para focar no supermercado.");
        return;
    }
    const map = mapElement.__googleMap;
    if (!map) {
        console.error("[ERROR] Mapa não inicializado para o elemento:", elementId);
        return;
    }
    map.setCenter({ lat: lat, lng: lng });
    map.setZoom(15);
};

window.mapsInterop.initMapaLojaClick = function (lat, lng, elementId, dotNetRef) {
    if (!window.google || !window.google.maps) {
        console.error("[ERROR] Google Maps API não carregado ou chave inválida.");
        return;
    }
    const center = { lat: parseFloat(lat), lng: parseFloat(lng) };
    const mapElement = document.getElementById(elementId);
    if (!mapElement) {
        console.error("[ERROR] Elemento do mapa não encontrado:", elementId);
        return;
    }

    const map = new google.maps.Map(mapElement, {
        zoom: 15,
        center: center,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false,
        mapId: "7af9339427e7484e" // Map ID configurado
    });
    mapElement.__googleMap = map;


    // Verifica se AdvancedMarkerElement está disponível
    const useAdvancedMarkers = window.google.maps.marker && window.google.maps.marker.AdvancedMarkerElement;
    let userMarker;
    if (useAdvancedMarkers) {
        userMarker = new google.maps.marker.AdvancedMarkerElement({
            position: center,
            map: map,
            draggable: true,
            content: createCustomMarkerIcon()
        });
    } else {
        console.warn("[WARN] Usando marcador padrão como fallback para o marcador do usuário.");
        userMarker = new google.maps.Marker({
            position: center,
            map: map,
            draggable: true,
            icon: {
                url: "https://maps.google.com/mapfiles/ms/icons/blue-dot.png"
            }
        });
    }

    const geocoder = new google.maps.Geocoder();
    function handleMapClick(e) {
        const latClicked = e.latLng.lat();
        const lngClicked = e.latLng.lng();
        userMarker.position = new google.maps.LatLng(latClicked, lngClicked);
        geocoder.geocode({ location: { lat: latClicked, lng: latClicked } }, function (results, status) {
            if (status === google.maps.GeocoderStatus.OK && results[0]) {
                const endereco = results[0].formatted_address;
                dotNetRef.invokeMethodAsync("SetCoordinatesWithAddress", latClicked, lngClicked, endereco);
            } else {
                console.error("[ERROR] Falha na geocodificação:", status);
                dotNetRef.invokeMethodAsync("SetCoordinatesWithAddress", latClicked, lngClicked, "Endereço não encontrado");
            }
        });
    }

    map.addListener("click", handleMapClick);
    userMarker.addListener("dragend", function () {
        handleMapClick({ latLng: userMarker.position });
    });
};

window.mapsInterop.initStreetView = function (elementId, lat, lng) {
    const location = new google.maps.LatLng(lat, lng);
    const panorama = new google.maps.StreetViewPanorama(
        document.getElementById(elementId),
        {
            position: location,
            pov: { heading: 165, pitch: 0 },
            zoom: 1,
            addressControl: false,
            linksControl: true,
            panControl: true,
            enableCloseButton: false
        }
    );

    // Verifica se o Street View está disponível no local
    const streetViewService = new google.maps.StreetViewService();
    streetViewService.getPanorama({ location: location, radius: 50 }, (data, status) => {
        if (status === google.maps.StreetViewStatus.OK) {
            panorama.setPosition(location);
        } else {
            console.warn(`[WARN] Street View não disponível para lat: ${lat} lng: ${lng}`);
            document.getElementById(elementId).innerHTML = "<p>Street View não disponível neste local.</p>";
        }
    });
};
