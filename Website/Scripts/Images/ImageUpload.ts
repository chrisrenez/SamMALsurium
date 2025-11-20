/**
 * ImageUpload.ts
 * Handles image upload UI interactions including drag-and-drop, file selection,
 * client-side validation, and image preview.
 */

const MAX_FILE_SIZE = 26214400; // 25MB in bytes
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif'];
const ALLOWED_MIME_TYPES = ['image/jpeg', 'image/png', 'image/gif'];

interface ImageUploadElements {
    dropZone: HTMLElement | null;
    dropZoneContent: HTMLElement | null;
    fileInput: HTMLInputElement | null;
    imagePreview: HTMLElement | null;
    previewImage: HTMLImageElement | null;
    previewFilename: HTMLElement | null;
    previewFilesize: HTMLElement | null;
    removeButton: HTMLElement | null;
    uploadButton: HTMLButtonElement | null;
    selectFileBtn: HTMLElement | null;
    form: HTMLFormElement | null;
}

/**
 * Initializes the image upload UI components
 */
export function initializeUpload(): void {
    const elements = getElements();

    if (!elements.dropZone || !elements.fileInput) {
        console.error('Required upload elements not found');
        return;
    }

    setupDragAndDrop(elements);
    setupFileInput(elements);
    setupRemoveButton(elements);
    setupSelectButton(elements);

    console.log('Image upload initialized');
}

/**
 * Gets all required DOM elements
 */
function getElements(): ImageUploadElements {
    return {
        dropZone: document.getElementById('dropZone'),
        dropZoneContent: document.getElementById('dropZoneContent'),
        fileInput: document.getElementById('fileInput') as HTMLInputElement,
        imagePreview: document.getElementById('imagePreview'),
        previewImage: document.getElementById('previewImg') as HTMLImageElement,
        previewFilename: document.getElementById('fileName'),
        previewFilesize: document.getElementById('fileSize'),
        removeButton: document.getElementById('removeImageBtn'),
        uploadButton: document.getElementById('uploadBtn') as HTMLButtonElement,
        selectFileBtn: document.getElementById('selectFileBtn'),
        form: document.querySelector('form')
    };
}

/**
 * Sets up drag-and-drop functionality
 */
function setupDragAndDrop(elements: ImageUploadElements): void {
    const { dropZone, fileInput } = elements;

    if (!dropZone || !fileInput) return;

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop zone when item is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.add('drag-over');
        }, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.remove('drag-over');
        }, false);
    });

    // Handle dropped files
    dropZone.addEventListener('drop', (e: Event) => {
        const dt = (e as DragEvent).dataTransfer;
        if (dt && dt.files && dt.files.length > 0) {
            const file = dt.files[0];
            handleFileSelection(file, elements);
        }
    }, false);

    // Also handle clicks on the drop zone (except on buttons)
    dropZone.addEventListener('click', (e) => {
        const target = e.target as HTMLElement;
        // Don't trigger file input if clicking on a button
        if (target.tagName === 'BUTTON' || target.closest('button')) {
            return;
        }
        fileInput.click();
    });
}

/**
 * Sets up the select file button
 */
function setupSelectButton(elements: ImageUploadElements): void {
    const { selectFileBtn, fileInput } = elements;

    if (!selectFileBtn || !fileInput) return;

    selectFileBtn.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        fileInput.click();
    });
}

/**
 * Sets up file input change handler
 */
function setupFileInput(elements: ImageUploadElements): void {
    const { fileInput } = elements;

    if (!fileInput) return;

    fileInput.addEventListener('change', () => {
        if (fileInput.files && fileInput.files.length > 0) {
            const file = fileInput.files[0];
            handleFileSelection(file, elements);
        }
    });
}

/**
 * Sets up remove button functionality
 */
function setupRemoveButton(elements: ImageUploadElements): void {
    const { removeButton, fileInput, dropZone, dropZoneContent, imagePreview } = elements;

    if (!removeButton) return;

    removeButton.addEventListener('click', (e) => {
        e.stopPropagation(); // Prevent triggering file input

        if (fileInput) {
            fileInput.value = '';
        }

        if (imagePreview) {
            imagePreview.classList.add('d-none');
        }

        if (dropZoneContent) {
            dropZoneContent.classList.remove('d-none');
        }

        if (dropZone) {
            dropZone.classList.remove('has-image');
        }

        clearValidationErrors();
    });
}

/**
 * Handles file selection from either drag-and-drop or file input
 */
function handleFileSelection(file: File, elements: ImageUploadElements): void {
    // Clear previous errors
    clearValidationErrors();

    // Validate file
    const validationResult = validateFile(file);

    if (!validationResult.valid) {
        showValidationError(validationResult.error || 'Invalid file');
        return;
    }

    // Show preview
    showImagePreview(file, elements);

    // Enable upload button
    if (elements.uploadButton) {
        elements.uploadButton.disabled = false;
    }
}

/**
 * Validates the selected file
 */
function validateFile(file: File): { valid: boolean; error?: string } {
    // Check file size
    if (file.size > MAX_FILE_SIZE) {
        const maxSizeMB = (MAX_FILE_SIZE / 1024 / 1024).toFixed(0);
        return {
            valid: false,
            error: `File size exceeds ${maxSizeMB}MB limit. Please compress your image and try again.`
        };
    }

    // Check file extension
    const fileName = file.name.toLowerCase();
    const hasValidExtension = ALLOWED_EXTENSIONS.some(ext => fileName.endsWith(ext));

    if (!hasValidExtension) {
        return {
            valid: false,
            error: `File type not supported. Allowed types: ${ALLOWED_EXTENSIONS.join(', ')}`
        };
    }

    // Check MIME type
    if (!ALLOWED_MIME_TYPES.includes(file.type)) {
        return {
            valid: false,
            error: `File type not supported. Please select a valid image file.`
        };
    }

    return { valid: true };
}

/**
 * Shows image preview
 */
function showImagePreview(file: File, elements: ImageUploadElements): void {
    const {
        dropZone,
        dropZoneContent,
        imagePreview,
        previewImage,
        previewFilename,
        previewFilesize
    } = elements;

    const reader = new FileReader();

    reader.onload = (e) => {
        if (previewImage && e.target?.result) {
            previewImage.src = e.target.result as string;
        }

        if (previewFilename) {
            previewFilename.textContent = file.name;
        }

        if (previewFilesize) {
            previewFilesize.textContent = formatFileSize(file.size);
        }

        if (imagePreview) {
            imagePreview.classList.remove('d-none');
        }

        if (dropZoneContent) {
            dropZoneContent.classList.add('d-none');
        }

        if (dropZone) {
            dropZone.classList.add('has-image');
        }
    };

    reader.onerror = () => {
        showValidationError('Error reading file. Please try again.');
    };

    reader.readAsDataURL(file);
}

/**
 * Formats file size in human-readable format
 */
function formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
}

/**
 * Shows validation error message
 */
function showValidationError(message: string): void {
    const errorContainer = document.getElementById('validationError');

    if (errorContainer) {
        errorContainer.textContent = message;
        errorContainer.classList.remove('d-none');
    } else {
        // Fallback to alert if container doesn't exist
        alert(message);
    }
}

/**
 * Clears validation error messages
 */
function clearValidationErrors(): void {
    const errorContainer = document.getElementById('validationError');

    if (errorContainer) {
        errorContainer.textContent = '';
        errorContainer.classList.add('d-none');
    }
}

/**
 * Prevents default behavior for events
 */
function preventDefaults(e: Event): void {
    e.preventDefault();
    e.stopPropagation();
}
