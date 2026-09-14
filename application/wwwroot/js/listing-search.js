document.addEventListener(
    "DOMContentLoaded",
    function () {

        const listingSearchModules =
            document.querySelectorAll(".listing-search");


        listingSearchModules.forEach(
            function (searchArea) {

                const searchBox =
                    searchArea.querySelector(
                        ".listing-search-input"
                    );


                const suggestionBox =
                    searchArea.querySelector(
                        ".listing-search-suggestions"
                    );


                const suggestionsUrl =
                    searchArea.dataset.suggestionsUrl;


                const itemUrl =
                    searchArea.dataset.itemUrl;

                const resultsUrl =
                    searchArea.dataset.resultsUrl;


                const gridTarget =
                    searchArea.dataset.gridTarget;

                const sortTarget =
                    searchArea.dataset.sortTarget;

                if (
                    !searchBox ||
                    !suggestionBox ||
                    !suggestionsUrl ||
                    !itemUrl
                ) {
                    return;
                }


                let searchTimer;

                async function updateGrid() {

                    if (
                        !resultsUrl ||
                        !gridTarget
                    ) {
                        return;
                    }


                    const grid =
                        document.querySelector(
                            gridTarget
                        );


                    if (!grid) {
                        return;
                    }

                    const searchValue =
                        searchBox.value.trim();


                    const sortValue =
                        sortTarget
                            ?
                            document.querySelector(sortTarget)?.value
                            :
                            "";


                    const categoryValue =
                        searchArea.dataset.categoryTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.categoryTarget
                            )?.value
                            :
                            "";


                    const brandValue =
                        searchArea.dataset.brandTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.brandTarget
                            )?.value
                            :
                            "";


                    const minPriceValue =
                        searchArea.dataset.minPriceTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.minPriceTarget
                            )?.value
                            :
                            "";


                    const maxPriceValue =
                        searchArea.dataset.maxPriceTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.maxPriceTarget
                            )?.value
                            :
                            "";

                    const sellerValue =
                        searchArea.dataset.sellerTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.sellerTarget
                            )?.value
                            :
                            "";


                    const statusValue =
                        searchArea.dataset.statusTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.statusTarget
                            )?.value
                            :
                            "";


                    const fromDateValue =
                        searchArea.dataset.fromDateTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.fromDateTarget
                            )?.value
                            :
                            "";


                    const toDateValue =
                        searchArea.dataset.toDateTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.toDateTarget
                            )?.value
                            :
                            "";


                    const stockValue =
                        searchArea.dataset.stockTarget
                            ?
                            document.querySelector(
                                searchArea.dataset.stockTarget
                            )?.value
                            :
                            "";

                    try {

                        const params =
                            new URLSearchParams();

                        params.append(
                            "SearchTerm",
                            searchValue
                        );


                        params.append(
                            "SortBy",
                            sortValue
                        );


                        params.append(
                            "CategoryId",
                            categoryValue
                        );


                        params.append(
                            "Brand",
                            brandValue
                        );


                        params.append(
                            "MinPrice",
                            minPriceValue
                        );


                        params.append(
                            "MaxPrice",
                            maxPriceValue
                        );


                        params.append(
                            "InStock",
                            stockValue
                        );

                        params.append(
                            "Seller",
                            sellerValue
                        );


                        params.append(
                            "Status",
                            statusValue
                        );


                        params.append(
                            "FromDate",
                            fromDateValue
                        );


                        params.append(
                            "ToDate",
                            toDateValue
                        );

                        const response =
                            await fetch(
                                resultsUrl
                                +
                                "?"
                                +
                                params.toString()
                            );


                        if (!response.ok) {
                            return;
                        }


                        const html =
                            await response.text();


                        grid.innerHTML =
                            html;

                        const historyType =
                            document
                                .querySelector(".product-search-area")
                                .dataset.historyType;


                        const statusResponse =
                            await fetch(
                                "/Order/GetAvailableStatuses?type="
                                + historyType
                            );

                        const statuses =
                            await statusResponse.json();


                        const statusDropdown =
                            document.querySelector(".filter-status");


                        if (statusDropdown) {

                            const currentStatus =
                                statusDropdown.value;


                            statusDropdown.innerHTML =
                                '<option value="">All Status</option>';


                            statuses.forEach(
                                function (status) {

                                    const option =
                                        document.createElement("option");


                                    option.value = status;

                                    option.textContent = status;


                                    if (status === currentStatus) {
                                        option.selected = true;
                                    }


                                    statusDropdown.appendChild(option);

                                }
                            );

                        }


                    }
                    catch (error) {

                        console.error(
                            "Grid update failed:",
                            error
                        );

                    }

                }

                async function loadSuggestions() {

                    clearTimeout(searchTimer);


                    const value =
                        searchBox.value.trim();


                    if (value.length === 0) {

                        suggestionBox.innerHTML = "";

                        suggestionBox.style.display =
                            "none";

                        return;
                    }


                    searchTimer =
                        setTimeout(
                            async function () {

                                try {

                                    const response =
                                        await fetch(
                                            suggestionsUrl
                                            +
                                            "?searchTerm="
                                            +
                                            encodeURIComponent(value)
                                        );


                                    if (!response.ok) {

                                        suggestionBox.style.display =
                                            "none";

                                        return;
                                    }


                                    const suggestions =
                                        await response.json();


                                    suggestionBox.innerHTML = "";


                                    if (
                                        !Array.isArray(suggestions) ||
                                        suggestions.length === 0
                                    ) {

                                        const emptyResult =
                                            document.createElement(
                                                "div"
                                            );


                                        emptyResult.className =
                                            "no-search-result";


                                        emptyResult.textContent =
                                            "No products found";


                                        suggestionBox.appendChild(
                                            emptyResult
                                        );


                                        suggestionBox.style.display =
                                            "block";

                                        return;
                                    }


                                    suggestions.forEach(
                                        function (product) {

                                            const item =
                                                document.createElement(
                                                    "div"
                                                );


                                            item.className =
                                                "search-suggestion-item";


                                            const imageArea =
                                                document.createElement(
                                                    "div"
                                                );


                                            imageArea.className =
                                                "search-suggestion-image";


                                            function showPlaceholder() {

                                                imageArea.innerHTML = "";


                                                const placeholder =
                                                    document.createElement(
                                                        "div"
                                                    );


                                                placeholder.className =
                                                    "no-suggestion-image";


                                                placeholder.textContent =
                                                    "🌱";


                                                imageArea.appendChild(
                                                    placeholder
                                                );

                                            }


                                            if (product.imagePath) {

                                                const image =
                                                    document.createElement(
                                                        "img"
                                                    );


                                                image.src =
                                                    product.imagePath;


                                                image.alt =
                                                    product.productName || "Product";


                                                image.addEventListener(
                                                    "error",
                                                    function () {

                                                        showPlaceholder();

                                                    }
                                                );


                                                imageArea.appendChild(
                                                    image
                                                );

                                            }
                                            else {

                                                showPlaceholder();

                                            }


                                            const info =
                                                document.createElement(
                                                    "div"
                                                );


                                            info.className =
                                                "search-suggestion-info";


                                            const productName =
                                                document.createElement(
                                                    "strong"
                                                );


                                            productName.textContent =
                                                product.productName || "";


                                            info.appendChild(
                                                productName
                                            );


                                            const category =
                                                document.createElement(
                                                    "span"
                                                );


                                            category.textContent =
                                                "Category: "
                                                +
                                                (product.categoryName || "");


                                            info.appendChild(
                                                category
                                            );


                                            if (product.brand) {

                                                const brand =
                                                    document.createElement(
                                                        "span"
                                                    );


                                                brand.textContent =
                                                    "Brand: "
                                                    +
                                                    product.brand;


                                                info.appendChild(
                                                    brand
                                                );

                                            }


                                            const price =
                                                document.createElement(
                                                    "span"
                                                );


                                            price.textContent =
                                                "৳"
                                                +
                                                product.price;


                                            info.appendChild(
                                                price
                                            );


                                            const availability =
                                                document.createElement(
                                                    "span"
                                                );

                                            if (!document
                                                .querySelector(".product-search-area")
                                                .classList.contains("history-search")) {

                                                if (product.isAvailable) {

                                                    availability.className =
                                                        "available-text";


                                                    availability.textContent =
                                                        "In Stock";

                                                }
                                                else {

                                                    availability.className =
                                                        "out-stock-text";


                                                    availability.textContent =
                                                        "Out of Stock";

                                                }
                                            }


                                            info.appendChild(
                                                availability
                                            );


                                            item.appendChild(
                                                imageArea
                                            );


                                            item.appendChild(
                                                info
                                            );


                                            item.addEventListener(
                                                "click",
                                                function () {

                                                    window.location.href =
                                                        itemUrl
                                                        +
                                                        product.productId;

                                                }
                                            );


                                            suggestionBox.appendChild(
                                                item
                                            );

                                        }
                                    );


                                    suggestionBox.style.display =
                                        "block";

                                }
                                catch {

                                    suggestionBox.style.display =
                                        "none";

                                }

                            },
                            100
                        );

                }


                searchBox.addEventListener(
                    "input",
                    function () {

                        loadSuggestions();

                        updateGrid();

                    }
                );

                if (sortTarget) {

                    const sortDropdown =
                        document.querySelector(
                            sortTarget
                        );


                    if (sortDropdown) {

                        sortDropdown.addEventListener(
                            "change",
                            function () {

                                updateGrid();

                            }
                        );

                    }

                }

                const filterSelectors = [

                    searchArea.dataset.categoryTarget,

                    searchArea.dataset.brandTarget,

                    searchArea.dataset.sellerTarget,

                    searchArea.dataset.minPriceTarget,

                    searchArea.dataset.maxPriceTarget,

                    searchArea.dataset.stockTarget,

                    searchArea.dataset.statusTarget,

                    searchArea.dataset.fromDateTarget,

                    searchArea.dataset.toDateTarget

                ];



                filterSelectors.forEach(
                    selector => {

                        if (!selector) {
                            return;
                        }


                        const element =
                            document.querySelector(
                                selector
                            );


                        if (!element) {
                            return;
                        }



                        const isPriceInput =
                            selector === searchArea.dataset.minPriceTarget ||
                            selector === searchArea.dataset.maxPriceTarget;



                        if (isPriceInput) {

                            let timer;


                            element.addEventListener(
                                "input",
                                function () {

                                    clearTimeout(timer);


                                    timer =
                                        setTimeout(
                                            function () {

                                                updateGrid();

                                            },
                                            100
                                        );

                                }
                            );

                        }
                        else {


                            element.addEventListener(
                                "change",
                                function () {

                                    updateGrid();

                                }
                            );

                        }


                    }
                );

                const resetButton =
                    document.querySelector(
                        searchArea.dataset.resetTarget
                    );


                if (resetButton) {

                    resetButton.addEventListener(
                        "click",
                        function () {

                            const currentState = {

                                search:
                                    searchBox.value,

                                category:
                                    searchArea.dataset.categoryTarget
                                        ? document.querySelector(searchArea.dataset.categoryTarget)?.value
                                        : "",

                                brand:
                                    searchArea.dataset.brandTarget
                                        ? document.querySelector(searchArea.dataset.brandTarget)?.value
                                        : "",

                                minPrice:
                                    searchArea.dataset.minPriceTarget
                                        ? document.querySelector(searchArea.dataset.minPriceTarget)?.value
                                        : "",

                                maxPrice:
                                    searchArea.dataset.maxPriceTarget
                                        ? document.querySelector(searchArea.dataset.maxPriceTarget)?.value
                                        : "",

                                seller:
                                    searchArea.dataset.sellerTarget
                                        ? document.querySelector(searchArea.dataset.sellerTarget)?.value
                                        : "",

                                status:
                                    searchArea.dataset.statusTarget
                                        ? document.querySelector(searchArea.dataset.statusTarget)?.value
                                        : "",

                                fromDate:
                                    searchArea.dataset.fromDateTarget
                                        ? document.querySelector(searchArea.dataset.fromDateTarget)?.value
                                        : "",

                                toDate:
                                    searchArea.dataset.toDateTarget
                                        ? document.querySelector(searchArea.dataset.toDateTarget)?.value
                                        : "",

                                sort:
                                    searchArea.dataset.sortTarget
                                        ? document.querySelector(searchArea.dataset.sortTarget)?.value
                                        : ""

                            };


                            const alreadyDefault =
                                Object.values(currentState)
                                    .every(x => x === "");


                            if (alreadyDefault) {
                                return;
                            }

                            if (searchArea.dataset.categoryTarget) {
                                document.querySelector(
                                    searchArea.dataset.categoryTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.brandTarget) {
                                document.querySelector(
                                    searchArea.dataset.brandTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.minPriceTarget) {
                                document.querySelector(
                                    searchArea.dataset.minPriceTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.maxPriceTarget) {
                                document.querySelector(
                                    searchArea.dataset.maxPriceTarget
                                ).value = "";
                            }


                            if (searchArea.dataset.stockTarget) {
                                document.querySelector(
                                    searchArea.dataset.stockTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.sellerTarget) {
                                document.querySelector(
                                    searchArea.dataset.sellerTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.statusTarget) {
                                document.querySelector(
                                    searchArea.dataset.statusTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.fromDateTarget) {
                                document.querySelector(
                                    searchArea.dataset.fromDateTarget
                                ).value = "";
                            }



                            if (searchArea.dataset.toDateTarget) {

                                document.querySelector(
                                    searchArea.dataset.toDateTarget
                                ).value = "";

                            }


                            searchBox.value = "";


                            if (searchArea.dataset.sortTarget) {
                                document.querySelector(
                                    searchArea.dataset.sortTarget
                                ).value = "";
                            }


                            updateGrid();

                        }
                    );

                }

                searchBox.addEventListener(
                    "focus",
                    function () {

                        if (
                            searchBox.value.trim().length > 0
                        ) {

                            loadSuggestions();

                        }

                    }
                );


                document.addEventListener(
                    "click",
                    function (event) {

                        if (
                            !searchArea.contains(event.target)
                        ) {

                            suggestionBox.style.display =
                                "none";

                        }

                    }
                );
                window.refreshOrderGrid = updateGrid;
            }
        );

    }
);

let selectedOrderId = null;



document.addEventListener(
    "click",
    function (e) {



        if (e.target.classList.contains("order-confirm-btn")) {

            selectedOrderId =
                e.target.dataset.orderId;


            document
                .getElementById("confirmOrderModal")
                .style.display = "flex";

        }





        if (e.target.classList.contains("order-reject-btn")) {

            selectedOrderId =
                e.target.dataset.orderId;


            document
                .getElementById("rejectOrderId")
                .value =
                selectedOrderId;


            document
                .getElementById("rejectOrderModal")
                .style.display = "flex";

        }





        if (e.target.classList.contains("order-cancel-modal")) {

            document
                .getElementById("confirmOrderModal")
                .style.display = "none";

        }





        if (e.target.classList.contains("order-cancel-reject")) {

            document
                .getElementById("rejectOrderModal")
                .style.display = "none";

        }

    }
);







document
    .querySelector(".order-confirm-modal")
    ?.addEventListener(
        "click",
        function () {


            fetch(
                "/Order/ConfirmOrder",
                {
                    method: "POST",
                    headers:
                    {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body:
                        "orderId=" + selectedOrderId
                }
            )
                .then(
                    response => response.json()
                )
                .then(
                    data => {

                        if (data.success) {


                            document
                                .getElementById("confirmOrderModal")
                                .style.display = "none";


                            window.refreshOrderGrid();

                        }

                    }
                );


        }
    );








document
    .querySelector(".order-submit-reject")
    ?.addEventListener(
        "click",
        function () {


            let orderId =
                document
                    .getElementById("rejectOrderId")
                    .value;



            let reason =
                document
                    .getElementById("rejectReason")
                    .value;



            let note =
                document
                    .getElementById("rejectNote")
                    .value;

            document
                .getElementById("rejectReason")
                .addEventListener(
                    "change",
                    function () {

                        document
                            .getElementById("rejectReasonError")
                            .style.display =
                            "none";

                    }
                );


            if (reason === "") {


                const errorMessage =
                    document.getElementById(
                        "rejectReasonError"
                    );


                errorMessage.textContent =
                    "Please select a rejection reason.";


                errorMessage.style.display =
                    "block";


                return;

            }




            fetch(
                "/Order/RejectOrder",
                {
                    method: "POST",
                    headers:
                    {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body:
                        "orderId=" + orderId +
                        "&rejectionReason=" +
                        encodeURIComponent(reason) +
                        "&rejectionNote=" +
                        encodeURIComponent(note)
                }
            )
                .then(
                    response => response.json()
                )
                .then(
                    data => {

                        if (data.success) {


                            document
                                .getElementById("rejectOrderModal")
                                .style.display = "none";


                            document
                                .getElementById("rejectReason")
                                .value = "";


                            document
                                .getElementById("rejectNote")
                                .value = "";


                            window.refreshOrderGrid();

                        }
                    }
                );


        }
    );