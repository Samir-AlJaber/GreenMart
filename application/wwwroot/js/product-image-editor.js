(() => {

    let editor = null;
    let workingCanvas = null;
    let originalCanvas = null;
    let workingContext = null;
    let originalContext = null;
    let initialImageData = null;
    let undoStack = [];
    let redoStack = [];
    let currentTool = "erase";
    let brushSize = 48;
    let zoomLevel = 1;
    let baseDisplayWidth = 0;
    let baseDisplayHeight = 0;
    let isDrawing = false;
    let previousPoint = null;
    let saveHandler = null;
    let outputFileName = "product-photo.png";
    let previouslyFocusedElement = null;


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
                reject(new Error("The photo could not be opened in the editor."));
            };

            image.src = objectUrl;
        });


    const createEditor = () => {
        const wrapper = document.createElement("div");

        wrapper.className = "product-image-editor";
        wrapper.hidden = true;
        wrapper.innerHTML = `
            <div class="product-image-editor-dialog"
                 role="dialog"
                 aria-modal="true"
                 aria-labelledby="productImageEditorTitle">
                <header class="product-image-editor-header">
                    <div>
                        <p>Manual background editor</p>
                        <h2 id="productImageEditorTitle">Refine product photo</h2>
                    </div>
                    <button type="button"
                            class="product-image-editor-close"
                            data-editor-close
                            aria-label="Close photo editor">×</button>
                </header>

                <div class="product-image-editor-tools" aria-label="Editing tools">
                    <div class="product-image-editor-tool-group">
                        <button type="button" class="is-active" data-editor-tool="erase">Erase unwanted area</button>
                        <button type="button" data-editor-tool="restore">Restore product area</button>
                    </div>

                    <label class="product-image-editor-brush">
                        <span>Brush</span>
                        <input type="range" min="8" max="120" value="48" data-editor-brush />
                        <output data-editor-brush-output>48</output>
                    </label>

                    <div class="product-image-editor-tool-group">
                        <button type="button" data-editor-undo disabled>Undo</button>
                        <button type="button" data-editor-redo disabled>Redo</button>
                        <button type="button" data-editor-reset>Reset</button>
                    </div>

                    <button type="button" class="product-image-editor-before" data-editor-before>
                        Hold for original
                    </button>
                </div>

                <div class="product-image-editor-stage" data-editor-stage>
                    <div class="product-image-editor-help" data-editor-help>
                        <strong>Erase mode</strong>
                        <span>Click and drag over the background you want to remove.</span>
                    </div>
                    <div class="product-image-editor-canvas-wrap" data-editor-canvas-wrap>
                        <canvas class="product-image-editor-original" aria-hidden="true"></canvas>
                        <canvas class="product-image-editor-working"
                                aria-label="Editable product photo"></canvas>
                        <span class="product-image-editor-brush-cursor"
                              data-editor-brush-cursor
                              aria-hidden="true"></span>
                    </div>
                </div>

                <footer class="product-image-editor-footer">
                    <p><strong>Tip:</strong> Drag directly on the photo. Use Restore if you erase part of the product.</p>

                    <div class="product-image-editor-zoom">
                        <button type="button" data-editor-zoom-out aria-label="Zoom out">−</button>
                        <button type="button" data-editor-zoom-reset>100%</button>
                        <button type="button" data-editor-zoom-in aria-label="Zoom in">+</button>
                    </div>

                    <div class="product-image-editor-actions">
                        <button type="button" class="is-secondary" data-editor-close>Cancel</button>
                        <button type="button" class="is-primary" data-editor-save>Save refinement</button>
                    </div>
                </footer>
            </div>`;

        document.body.appendChild(wrapper);

        editor = wrapper;
        workingCanvas = editor.querySelector(".product-image-editor-working");
        originalCanvas = editor.querySelector(".product-image-editor-original");
        workingContext = workingCanvas.getContext("2d", { willReadFrequently: true });
        originalContext = originalCanvas.getContext("2d");

        bindEditorEvents();
    };


    const updateHistoryButtons = () => {
        editor.querySelector("[data-editor-undo]").disabled = undoStack.length === 0;
        editor.querySelector("[data-editor-redo]").disabled = redoStack.length === 0;
    };


    const rememberCurrentImage = stack => {
        stack.push(
            workingContext.getImageData(
                0,
                0,
                workingCanvas.width,
                workingCanvas.height
            )
        );

        if (stack.length > 6)
        {
            stack.shift();
        }
    };


    const setTool = tool => {
        currentTool = tool;

        editor.querySelectorAll("[data-editor-tool]").forEach(button => {
            button.classList.toggle("is-active", button.dataset.editorTool === tool);
        });

        const help = editor.querySelector("[data-editor-help]");
        const cursor = editor.querySelector("[data-editor-brush-cursor]");

        help.innerHTML = tool === "erase"
            ? "<strong>Erase mode</strong><span>Drag over the background. Removed areas will show as a transparent grid.</span>"
            : "<strong>Restore mode</strong><span>Click and drag over any part of the product you removed by mistake.</span>";
        cursor.classList.toggle("is-restore", tool === "restore");
    };


    const updateBrushCursor = event => {
        const canvasWrap = editor.querySelector("[data-editor-canvas-wrap]");
        const cursor = editor.querySelector("[data-editor-brush-cursor]");
        const bounds = canvasWrap.getBoundingClientRect();

        cursor.style.left = `${event.clientX - bounds.left}px`;
        cursor.style.top = `${event.clientY - bounds.top}px`;
        cursor.style.width = `${brushSize}px`;
        cursor.style.height = `${brushSize}px`;
        canvasWrap.classList.add("is-cursor-visible");
    };


    const updateCanvasDisplaySize = () => {
        const displayWidth = baseDisplayWidth * zoomLevel;
        const displayHeight = baseDisplayHeight * zoomLevel;
        const canvasWrap = editor.querySelector("[data-editor-canvas-wrap]");

        canvasWrap.style.width = `${displayWidth}px`;
        canvasWrap.style.height = `${displayHeight}px`;
        workingCanvas.style.width = `${displayWidth}px`;
        workingCanvas.style.height = `${displayHeight}px`;
        originalCanvas.style.width = `${displayWidth}px`;
        originalCanvas.style.height = `${displayHeight}px`;
        editor.querySelector("[data-editor-zoom-reset]").textContent =
            `${Math.round(zoomLevel * 100)}%`;
    };


    const updateZoom = nextZoom => {
        zoomLevel = Math.min(4, Math.max(.5, nextZoom));
        updateCanvasDisplaySize();
    };


    const canvasPointFromEvent = event => {
        const bounds = workingCanvas.getBoundingClientRect();

        return {
            x: (event.clientX - bounds.left) * workingCanvas.width / bounds.width,
            y: (event.clientY - bounds.top) * workingCanvas.height / bounds.height,
            radius: brushSize * workingCanvas.width / bounds.width / 2
        };
    };


    const paintCircle = point => {
        workingContext.save();

        if (currentTool === "erase")
        {
            workingContext.globalCompositeOperation = "destination-out";
            workingContext.beginPath();
            workingContext.arc(point.x, point.y, point.radius, 0, Math.PI * 2);
            workingContext.fill();
        }
        else
        {
            workingContext.globalCompositeOperation = "source-over";
            workingContext.beginPath();
            workingContext.arc(point.x, point.y, point.radius, 0, Math.PI * 2);
            workingContext.clip();
            workingContext.drawImage(originalCanvas, 0, 0);
        }

        workingContext.restore();
    };


    const paintBetween = (fromPoint, toPoint) => {
        if (currentTool === "erase")
        {
            workingContext.save();
            workingContext.globalCompositeOperation = "destination-out";
            workingContext.lineWidth = toPoint.radius * 2;
            workingContext.lineCap = "round";
            workingContext.lineJoin = "round";
            workingContext.beginPath();
            workingContext.moveTo(fromPoint.x, fromPoint.y);
            workingContext.lineTo(toPoint.x, toPoint.y);
            workingContext.stroke();
            workingContext.restore();
            return;
        }

        const distance = Math.hypot(
            toPoint.x - fromPoint.x,
            toPoint.y - fromPoint.y
        );
        const spacing = Math.max(1, toPoint.radius * .16);
        const steps = Math.max(1, Math.ceil(distance / spacing));

        for (let step = 0; step <= steps; step++)
        {
            const progress = step / steps;

            paintCircle({
                x: fromPoint.x + (toPoint.x - fromPoint.x) * progress,
                y: fromPoint.y + (toPoint.y - fromPoint.y) * progress,
                radius: toPoint.radius
            });
        }
    };


    const stopDrawing = event => {
        if (!isDrawing)
        {
            return;
        }

        isDrawing = false;
        previousPoint = null;
        editor.querySelector("[data-editor-canvas-wrap]").classList.remove("is-painting");

        if (event.pointerId !== undefined &&
            workingCanvas.hasPointerCapture(event.pointerId))
        {
            workingCanvas.releasePointerCapture(event.pointerId);
        }

        updateHistoryButtons();
    };


    const showOriginal = shouldShow => {
        editor.classList.toggle("is-showing-original", shouldShow);
    };


    const closeEditor = () => {
        editor.hidden = true;
        document.body.classList.remove("product-image-editor-open");
        showOriginal(false);

        if (previouslyFocusedElement)
        {
            previouslyFocusedElement.focus();
        }
    };


    const bindEditorEvents = () => {
        editor.querySelectorAll("[data-editor-close]").forEach(button => {
            button.addEventListener("click", closeEditor);
        });

        editor.querySelectorAll("[data-editor-tool]").forEach(button => {
            button.addEventListener("click", () => setTool(button.dataset.editorTool));
        });

        const brushInput = editor.querySelector("[data-editor-brush]");

        brushInput.addEventListener("input", () => {
            brushSize = Number(brushInput.value);
            editor.querySelector("[data-editor-brush-output]").textContent = brushSize;

            const cursor = editor.querySelector("[data-editor-brush-cursor]");
            cursor.style.width = `${brushSize}px`;
            cursor.style.height = `${brushSize}px`;
        });

        editor.querySelector("[data-editor-undo]").addEventListener("click", () => {
            if (undoStack.length === 0) return;
            rememberCurrentImage(redoStack);
            workingContext.putImageData(undoStack.pop(), 0, 0);
            updateHistoryButtons();
        });

        editor.querySelector("[data-editor-redo]").addEventListener("click", () => {
            if (redoStack.length === 0) return;
            rememberCurrentImage(undoStack);
            workingContext.putImageData(redoStack.pop(), 0, 0);
            updateHistoryButtons();
        });

        editor.querySelector("[data-editor-reset]").addEventListener("click", () => {
            rememberCurrentImage(undoStack);
            redoStack = [];
            workingContext.putImageData(initialImageData, 0, 0);
            updateHistoryButtons();
        });

        const beforeButton = editor.querySelector("[data-editor-before]");
        beforeButton.addEventListener("pointerdown", () => showOriginal(true));
        beforeButton.addEventListener("pointerup", () => showOriginal(false));
        beforeButton.addEventListener("pointercancel", () => showOriginal(false));
        beforeButton.addEventListener("pointerleave", () => showOriginal(false));

        editor.querySelector("[data-editor-zoom-out]").addEventListener(
            "click",
            () => updateZoom(zoomLevel - .25)
        );
        editor.querySelector("[data-editor-zoom-in]").addEventListener(
            "click",
            () => updateZoom(zoomLevel + .25)
        );
        editor.querySelector("[data-editor-zoom-reset]").addEventListener(
            "click",
            () => updateZoom(1)
        );

        workingCanvas.addEventListener("pointerdown", event => {
            event.preventDefault();
            updateBrushCursor(event);
            rememberCurrentImage(undoStack);
            redoStack = [];
            isDrawing = true;
            editor.querySelector("[data-editor-canvas-wrap]").classList.add("is-painting");
            previousPoint = canvasPointFromEvent(event);
            workingCanvas.setPointerCapture(event.pointerId);
            paintCircle(previousPoint);
        });

        workingCanvas.addEventListener("pointermove", event => {
            updateBrushCursor(event);
            if (!isDrawing) return;

            const coalescedEvents = typeof event.getCoalescedEvents === "function"
                ? event.getCoalescedEvents()
                : [];
            const pointerEvents = coalescedEvents.length > 0
                ? coalescedEvents
                : [event];

            pointerEvents.forEach(pointerEvent => {
                const nextPoint = canvasPointFromEvent(pointerEvent);
                paintBetween(previousPoint, nextPoint);
                previousPoint = nextPoint;
            });
        });

        workingCanvas.addEventListener("pointerup", stopDrawing);
        workingCanvas.addEventListener("pointercancel", stopDrawing);
        workingCanvas.addEventListener("pointerenter", updateBrushCursor);
        workingCanvas.addEventListener("pointerleave", () => {
            if (!isDrawing)
            {
                editor.querySelector("[data-editor-canvas-wrap]")
                    .classList.remove("is-cursor-visible");
            }
        });

        editor.querySelector("[data-editor-save]").addEventListener("click", () => {
            workingCanvas.toBlob(blob => {
                if (!blob) return;

                saveHandler(
                    new File(
                        [blob],
                        outputFileName.replace(/\.[^/.]+$/, "") + "-refined.png",
                        { type: "image/png" }
                    )
                );

                closeEditor();
            }, "image/png");
        });

        document.addEventListener("keydown", event => {
            if (editor.hidden) return;

            if (event.key === "Escape") closeEditor();
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "z")
            {
                event.preventDefault();
                editor.querySelector("[data-editor-undo]").click();
            }
        });
    };


    const open = async ({ originalFile, processedFile, fileName, onSave }) => {
        if (!editor)
        {
            createEditor();
        }

        const [originalImage, processedImage] = await Promise.all([
            loadImage(originalFile),
            loadImage(processedFile)
        ]);

        previouslyFocusedElement = document.activeElement;
        saveHandler = onSave;
        outputFileName = fileName || processedFile.name;
        undoStack = [];
        redoStack = [];
        zoomLevel = 1;
        brushSize = 48;
        editor.querySelector("[data-editor-brush]").value = brushSize;
        editor.querySelector("[data-editor-brush-output]").textContent = brushSize;
        setTool("erase");

        workingCanvas.width = processedImage.naturalWidth;
        workingCanvas.height = processedImage.naturalHeight;
        originalCanvas.width = processedImage.naturalWidth;
        originalCanvas.height = processedImage.naturalHeight;

        originalContext.clearRect(0, 0, originalCanvas.width, originalCanvas.height);
        originalContext.drawImage(
            originalImage,
            0,
            0,
            originalCanvas.width,
            originalCanvas.height
        );

        workingContext.clearRect(0, 0, workingCanvas.width, workingCanvas.height);
        workingContext.drawImage(processedImage, 0, 0);
        initialImageData = workingContext.getImageData(
            0,
            0,
            workingCanvas.width,
            workingCanvas.height
        );

        editor.hidden = false;
        document.body.classList.add("product-image-editor-open");

        const stage = editor.querySelector("[data-editor-stage]");
        const availableWidth = Math.max(280, stage.clientWidth - 40);
        const availableHeight = Math.max(240, stage.clientHeight - 40);
        const fitScale = Math.min(
            1,
            availableWidth / workingCanvas.width,
            availableHeight / workingCanvas.height
        );

        baseDisplayWidth = workingCanvas.width * fitScale;
        baseDisplayHeight = workingCanvas.height * fitScale;
        updateCanvasDisplaySize();
        updateHistoryButtons();
        editor.querySelector("[data-editor-tool='erase']").focus();
    };


    window.GreenMartProductImageEditor = {
        open
    };

})();
