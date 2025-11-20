/**
 * ImageLibrary.ts
 * Handles image library UI interactions including image selection and privacy toggles.
 */

interface ImageLibraryElements {
    imageCards: NodeListOf<HTMLElement>;
    privacyForms: NodeListOf<HTMLFormElement>;
}

/**
 * Initializes the image library UI components
 */
export function initializeLibrary(): void {
    const elements = getElements();

    setupImageSelection(elements);
    setupPrivacyToggles(elements);

    console.log('Image library initialized');
}

/**
 * Gets all required DOM elements
 */
function getElements(): ImageLibraryElements {
    return {
        imageCards: document.querySelectorAll('[data-image-id]'),
        privacyForms: document.querySelectorAll('.privacy-form')
    };
}

/**
 * Sets up image selection handlers for selection mode
 */
function setupImageSelection(elements: ImageLibraryElements): void {
    const { imageCards } = elements;

    // Check if we're in selection mode (indicated by selectable cards)
    const selectableCards = document.querySelectorAll('.image-card-selectable');

    if (selectableCards.length === 0) {
        return; // Not in selection mode
    }

    imageCards.forEach(card => {
        if (!card.classList.contains('image-card-selectable')) {
            return;
        }

        // Find the select button form within this card
        const selectForm = card.querySelector<HTMLFormElement>('form[action*="SelectFromLibrary"]');

        if (!selectForm) {
            return;
        }

        // Make the entire card clickable
        card.style.cursor = 'pointer';

        card.addEventListener('click', (e) => {
            // Don't trigger if clicking on a form element directly
            const target = e.target as HTMLElement;
            if (target.tagName === 'BUTTON' || target.tagName === 'SELECT' || target.closest('button, select')) {
                return;
            }

            // Submit the selection form
            selectForm.submit();
        });
    });
}

/**
 * Sets up privacy toggle handlers with AJAX
 */
function setupPrivacyToggles(elements: ImageLibraryElements): void {
    const { privacyForms } = elements;

    privacyForms.forEach(form => {
        const select = form.querySelector<HTMLSelectElement>('select[name="privacy"]');

        if (!select) {
            return;
        }

        // Remove the inline onchange handler and add our own
        select.removeAttribute('onchange');

        select.addEventListener('change', async (e) => {
            e.preventDefault();

            const imageId = form.dataset.imageId;
            const privacy = select.value;

            if (!imageId) {
                console.error('Image ID not found');
                return;
            }

            try {
                // Disable select while updating
                select.disabled = true;

                // Get anti-forgery token
                const token = form.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value;

                if (!token) {
                    console.error('Anti-forgery token not found');
                    return;
                }

                // Send AJAX request
                const response = await fetch('/Images/UpdatePrivacy', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: new URLSearchParams({
                        imageId: imageId,
                        privacy: privacy,
                        __RequestVerificationToken: token
                    })
                });

                if (!response.ok) {
                    throw new Error('Failed to update privacy setting');
                }

                const result = await response.json();

                if (result.success) {
                    // Update the privacy badge
                    updatePrivacyBadge(form, privacy);

                    // Show success feedback
                    showSuccessFeedback(form, 'Privacy updated');
                } else {
                    throw new Error('Server returned error');
                }

            } catch (error) {
                console.error('Error updating privacy:', error);
                alert('Failed to update privacy setting. Please try again.');

                // Revert select to previous value
                const card = form.closest('[data-image-id]');
                if (card) {
                    const badge = card.querySelector('.badge');
                    if (badge?.textContent?.includes('Community')) {
                        select.value = '0';
                    } else {
                        select.value = '1';
                    }
                }
            } finally {
                // Re-enable select
                select.disabled = false;
            }
        });
    });
}

/**
 * Updates the privacy badge on the image card
 */
function updatePrivacyBadge(form: HTMLFormElement, privacy: string): void {
    const card = form.closest('[data-image-id]');
    if (!card) return;

    const badge = card.querySelector<HTMLElement>('.position-absolute.top-0.end-0 .badge');
    if (!badge) return;

    if (privacy === '0') {
        // Community Only
        badge.className = 'badge bg-secondary';
        badge.innerHTML = '<i class="bi bi-people-fill"></i> Community';
    } else {
        // Public Profile
        badge.className = 'badge bg-info';
        badge.innerHTML = '<i class="bi bi-globe"></i> Public';
    }
}

/**
 * Shows success feedback briefly
 */
function showSuccessFeedback(form: HTMLFormElement, message: string): void {
    const card = form.closest('[data-image-id]');
    if (!card) return;

    // Create feedback element
    const feedback = document.createElement('div');
    feedback.className = 'position-absolute top-50 start-50 translate-middle';
    feedback.innerHTML = `
        <div class="alert alert-success alert-sm shadow-sm" role="alert">
            <i class="bi bi-check-circle"></i> ${message}
        </div>
    `;

    card.appendChild(feedback);

    // Remove after 2 seconds
    setTimeout(() => {
        feedback.remove();
    }, 2000);
}
