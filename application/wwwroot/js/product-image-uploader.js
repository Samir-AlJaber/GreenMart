(() => {

    const sourceInput = document.getElementById("ProductImageSource");
    const processedInput = document.getElementById("ProductImages");
    const dropzone = document.getElementById("productImageDropzone");
    const backgroundOption = document.getElementById("AutoRemoveProductBackground");
    const previewShell = document.getElementById("productImagePreviewShell");
    const previewGrid = document.getElementById("productImagePreviewGrid");
    const status = document.getElementById("productImageStatus");
    const removeButton = document.getElementById("removeProductImage");
    const submitButton = document.getElementById("productSubmitButton");
    const productForm = document.getElementById("productForm");

    if (!sourceInput ||
        !processedInput ||
        !dropzone ||
        !backgroundOption ||
        !previewShell ||
        !previewGrid ||
        !status ||
        !removeButton ||
        !submitButton ||
        !productForm)
    {
        return;
    }

    const maximumFileSize = 10 * 1024 * 1024;
    const maximumPhotos = 8;
    const maximumDimension = 2400;
    const defaultStatus = status.textContent.trim();

    let isProcessing = false;
    let processingVersion = 0;
    let selectedFiles = [];
    let processedFiles = [];
    let previewUrls = [];


    const setStatus = (message, state = "") => {
        status.textContent = message;
        status.className = "product-image-status";

        if (state)
        {
            status.classList.add(`is-${state}`);
        }
    };


    const revokePreviewUrls = () => {
        for (const previewUrl of previewUrls)
        {
            URL.revokeObjectURL(previewUrl);
        }

        previewUrls = [];
    };


    const clearImages = () => {
        processingVersion++;
        isProcessing = false;
        submitButton.disabled = false;
        sourceInput.value = "";
        processedInput.value = "";
        selectedFiles = [];
        processedFiles = [];
        revokePreviewUrls();
        previewGrid.replaceChildren();
        previewShell.classList.remove("is-visible");
        dropzone.classList.remove("has-image", "is-processing");
        setStatus(defaultStatus);
    };


    const syncProcessedInput = () => {
        const transfer = new DataTransfer();

        for (const file of processedFiles)
        {
            transfer.items.add(file);
        }

        processedInput.files = transfer.files;
    };


    const renderPreviews = () => {
        revokePreviewUrls();
        previewGrid.replaceChildren();

        processedFiles.forEach((file, index) => {
            const previewUrl = URL.createObjectURL(file);
            const previewCard = document.createElement("div");
            const image = document.createElement("img");
            const badge = document.createElement("span");
            const refinePhotoButton = document.createElement("button");
            const removePhotoButton = document.createElement("button");

            previewUrls.push(previewUrl);
            previewCard.className = "product-image-preview-card";
            image.src = previewUrl;
            image.alt = `New product photo ${index + 1} preview`;
            badge.className = "product-image-order";
            badge.textContent = index === 0 ? "New main photo" : `New photo ${index + 1}`;
            refinePhotoButton.type = "button";
            refinePhotoButton.className = "product-image-refine";
            refinePhotoButton.dataset.refinePhoto = index.toString();
            refinePhotoButton.textContent = "Refine";
            refinePhotoButton.setAttribute(
                "aria-label",
                `Manually refine new photo ${index + 1}`
            );
            removePhotoButton.type = "button";
            removePhotoButton.className = "product-image-single-remove";
            removePhotoButton.dataset.removePhoto = index.toString();
            removePhotoButton.setAttribute("aria-label", `Remove new photo ${index + 1}`);
            removePhotoButton.textContent = "×";

            previewCard.append(image, badge, refinePhotoButton, removePhotoButton);
            previewGrid.appendChild(previewCard);
        });

        const hasPhotos = processedFiles.length > 0;
        previewShell.classList.toggle("is-visible", hasPhotos);
        dropzone.classList.toggle("has-image", hasPhotos);
    };


    const loadImage = file =>
        new Promise((resolve, reject) => {
            const image = new Image();
            const objectUrl = URL.createObjectURL(file);

            image.onload = () => {
                URL.revokeObjectURL(objectUrl);
                resolve(image);
            };

            image.onerror = () => {
                URL.revokeObjectURL(objectUrl);
                reject(new Error("The selected file could not be opened as an image."));
            };

            image.src = objectUrl;
        });


    const sampleCorner = (pixels, width, height, startX, startY, sampleSize) => {
        let red = 0;
        let green = 0;
        let blue = 0;
        let count = 0;

        for (let y = startY; y < Math.min(startY + sampleSize, height); y++)
        {
            for (let x = startX; x < Math.min(startX + sampleSize, width); x++)
            {
                const offset = (y * width + x) * 4;

                if (pixels[offset + 3] < 20)
                {
                    continue;
                }

                red += pixels[offset];
                green += pixels[offset + 1];
                blue += pixels[offset + 2];
                count++;
            }
        }

        return count === 0
            ? [255, 255, 255]
            : [red / count, green / count, blue / count];
    };


    const removeConnectedBackground = (imageData, width, height) => {
        const pixels = imageData.data;
        const pixelCount = width * height;
        const sampleSize = Math.max(
            4,
            Math.min(18, Math.round(Math.min(width, height) * 0.025))
        );
        const palette = [
            sampleCorner(pixels, width, height, 0, 0, sampleSize),
            sampleCorner(pixels, width, height, width - sampleSize, 0, sampleSize),
            sampleCorner(pixels, width, height, 0, height - sampleSize, sampleSize),
            sampleCorner(pixels, width, height, width - sampleSize, height - sampleSize, sampleSize)
        ];
        const visited = new Uint8Array(pixelCount);
        const queue = new Int32Array(pixelCount);
        const threshold = 72;
        const thresholdSquared = threshold * threshold;
        let queueStart = 0;
        let queueEnd = 0;

        const closestDistance = pixelIndex => {
            const offset = pixelIndex * 4;
            let closest = Number.POSITIVE_INFINITY;

            for (const color of palette)
            {
                const redDifference = pixels[offset] - color[0];
                const greenDifference = pixels[offset + 1] - color[1];
                const blueDifference = pixels[offset + 2] - color[2];
                const distance =
                    redDifference * redDifference +
                    greenDifference * greenDifference +
                    blueDifference * blueDifference;

                if (distance < closest)
                {
                    closest = distance;
                }
            }

            return closest;
        };

        const visit = pixelIndex => {
            if (visited[pixelIndex])
            {
                return;
            }

            visited[pixelIndex] = 1;

            if (pixels[pixelIndex * 4 + 3] < 16 ||
                closestDistance(pixelIndex) <= thresholdSquared)
            {
                queue[queueEnd++] = pixelIndex;
            }
        };

        for (let x = 0; x < width; x++)
        {
            visit(x);
            visit((height - 1) * width + x);
        }

        for (let y = 1; y < height - 1; y++)
        {
            visit(y * width);
            visit(y * width + width - 1);
        }

        while (queueStart < queueEnd)
        {
            const pixelIndex = queue[queueStart++];
            pixels[pixelIndex * 4 + 3] = 0;

            const x = pixelIndex % width;
            const y = Math.floor(pixelIndex / width);

            if (x > 0) visit(pixelIndex - 1);
            if (x < width - 1) visit(pixelIndex + 1);
            if (y > 0) visit(pixelIndex - width);
            if (y < height - 1) visit(pixelIndex + width);
        }
    };


    const processImage = async (file, shouldRemoveBackground) => {
        const supportedTypes = ["image/png", "image/jpeg", "image/webp"];

        if (!supportedTypes.includes(file.type))
        {
            throw new Error("Please choose a JPG, PNG or WebP image.");
        }

        if (file.size > maximumFileSize)
        {
            throw new Error("Please choose an image smaller than 10 MB.");
        }

        const image = await loadImage(file);
        const scale = Math.min(
            1,
            maximumDimension / Math.max(image.naturalWidth, image.naturalHeight)
        );
        const width = Math.max(1, Math.round(image.naturalWidth * scale));
        const height = Math.max(1, Math.round(image.naturalHeight * scale));
        const canvas = document.createElement("canvas");

        canvas.width = width;
        canvas.height = height;

        const context = canvas.getContext("2d", { willReadFrequently: true });
        context.imageSmoothingEnabled = true;
        context.imageSmoothingQuality = "high";
        context.drawImage(image, 0, 0, width, height);

        if (shouldRemoveBackground)
        {
            const imageData = context.getImageData(0, 0, width, height);
            removeConnectedBackground(imageData, width, height);
            context.clearRect(0, 0, width, height);
            context.putImageData(imageData, 0, 0);
        }

        return new Promise((resolve, reject) => {
            canvas.toBlob(blob => {
                if (blob)
                {
                    resolve(blob);
                }
                else
                {
                    reject(new Error("The image could not be prepared."));
                }
            }, "image/png");
        });
    };


    const prepareSelectedImages = async () => {
        if (selectedFiles.length === 0)
        {
            clearImages();
            return;
        }

        isProcessing = true;
        submitButton.disabled = true;
        dropzone.classList.add("is-processing");

        const shouldRemoveBackground = backgroundOption.checked;
        const currentProcessingVersion = ++processingVersion;
        const nextProcessedFiles = [];

        try
        {
            for (let index = 0; index < selectedFiles.length; index++)
            {
                const file = selectedFiles[index];

                setStatus(
                    shouldRemoveBackground
                        ? `Cleaning background · Photo ${index + 1} of ${selectedFiles.length}`
                        : `Preparing photos · Photo ${index + 1} of ${selectedFiles.length}`,
                    "processing"
                );

                const processedBlob = await processImage(file, shouldRemoveBackground);

                if (currentProcessingVersion !== processingVersion)
                {
                    return;
                }

                const cleanName = file.name
                    .replace(/\.[^/.]+$/, "")
                    .replace(/[^a-z0-9-_]+/gi, "-") || `product-${index + 1}`;

                nextProcessedFiles.push(
                    new File(
                        [processedBlob],
                        shouldRemoveBackground
                            ? `${cleanName}-background-removed.png`
                            : `${cleanName}.png`,
                        { type: "image/png" }
                    )
                );
            }

            processedFiles = nextProcessedFiles;
            syncProcessedInput();
            renderPreviews();
            setStatus(
                shouldRemoveBackground
                    ? `${processedFiles.length} replacement photo${processedFiles.length === 1 ? " is" : "s are"} ready with a crisp transparent background.`
                    : `${processedFiles.length} replacement photo${processedFiles.length === 1 ? " is" : "s are"} ready.`,
                "success"
            );
        }
        catch (error)
        {
            if (currentProcessingVersion !== processingVersion)
            {
                return;
            }

            processedFiles = [];
            processedInput.value = "";
            renderPreviews();
            setStatus(
                error.message || "The photos could not be prepared. Please choose them again.",
                "error"
            );
        }
        finally
        {
            if (currentProcessingVersion === processingVersion)
            {
                isProcessing = false;
                submitButton.disabled = false;
                dropzone.classList.remove("is-processing");
            }
        }
    };


    sourceInput.addEventListener("change", () => {
        const chosenFiles = Array.from(sourceInput.files);

        if (chosenFiles.length === 0)
        {
            clearImages();
            return;
        }

        if (chosenFiles.length > maximumPhotos)
        {
            clearImages();
            setStatus(`You can add up to ${maximumPhotos} photos for one product.`, "error");
            return;
        }

        selectedFiles = chosenFiles;
        prepareSelectedImages();
    });


    previewGrid.addEventListener("click", event => {
        const refinePhotoButton = event.target.closest("[data-refine-photo]");

        if (refinePhotoButton)
        {
            const photoIndex = Number(refinePhotoButton.dataset.refinePhoto);

            if (!window.GreenMartProductImageEditor)
            {
                setStatus("The manual photo editor could not be opened.", "error");
                return;
            }

            window.GreenMartProductImageEditor.open({
                originalFile: selectedFiles[photoIndex],
                processedFile: processedFiles[photoIndex],
                fileName: processedFiles[photoIndex].name,
                onSave: refinedFile => {
                    processedFiles[photoIndex] = refinedFile;
                    syncProcessedInput();
                    renderPreviews();
                    setStatus(
                        `Photo ${photoIndex + 1} refinement saved.`,
                        "success"
                    );
                }
            }).catch(error => {
                setStatus(
                    error.message || "The manual photo editor could not be opened.",
                    "error"
                );
            });

            return;
        }

        const removePhotoButton = event.target.closest("[data-remove-photo]");

        if (!removePhotoButton)
        {
            return;
        }

        const photoIndex = Number(removePhotoButton.dataset.removePhoto);
        selectedFiles.splice(photoIndex, 1);
        processedFiles.splice(photoIndex, 1);

        if (processedFiles.length === 0)
        {
            clearImages();
            return;
        }

        syncProcessedInput();
        renderPreviews();
        setStatus(
            `${processedFiles.length} replacement photo${processedFiles.length === 1 ? " is" : "s are"} ready.`,
            "success"
        );
    });


    removeButton.addEventListener("click", clearImages);


    backgroundOption.addEventListener("change", () => {
        if (selectedFiles.length > 0)
        {
            prepareSelectedImages();
        }
        else
        {
            setStatus(
                backgroundOption.checked
                    ? "Background cleaning is ready. Browse photos with a plain background for the cleanest result."
                    : defaultStatus
            );
        }
    });


    productForm.addEventListener("submit", event => {
        if (isProcessing)
        {
            event.preventDefault();
            setStatus("Please wait while the product photos are being prepared.", "processing");
        }
    });

})();
