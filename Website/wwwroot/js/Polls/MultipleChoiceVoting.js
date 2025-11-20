class MultipleChoiceVoting {
    constructor() {
        this.form = document.querySelector('.multiple-choice-form');
        if (this.form) {
            this.initializeValidation();
            this.initializeLoadingState();
        }
    }
    initializeValidation() {
        if (!this.form)
            return;
        this.form.addEventListener('submit', (e) => {
            const form = e.target;
            const inputs = form.querySelectorAll('input[name="options[]"]');
            const isAnyChecked = Array.from(inputs).some(input => input.checked);
            if (!isAnyChecked) {
                e.preventDefault();
                const errorDiv = form.querySelector(`[id^="validation-error-"]`);
                if (errorDiv) {
                    errorDiv.classList.remove('d-none');
                }
                // Show alert
                alert('Bitte wählen Sie mindestens eine Option aus.');
            }
        });
    }
    initializeLoadingState() {
        if (!this.form)
            return;
        this.form.addEventListener('submit', () => {
            const submitBtn = this.form?.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                const originalText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Wird gespeichert...';
                // Re-enable after 5 seconds as fallback
                setTimeout(() => {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalText;
                }, 5000);
            }
        });
    }
}
// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new MultipleChoiceVoting();
});
export {};
//# sourceMappingURL=MultipleChoiceVoting.js.map