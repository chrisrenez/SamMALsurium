class ScoreVoting {
    constructor() {
        this.form = document.querySelector('.score-voting-form');
        if (this.form) {
            this.initializeSliders();
            this.initializeLoadingState();
        }
    }
    initializeSliders() {
        const sliders = document.querySelectorAll('.score-slider');
        sliders.forEach(slider => {
            const optionId = slider.dataset.optionId;
            const display = document.querySelector(`.score-display-${optionId}`);
            if (display) {
                slider.addEventListener('input', () => {
                    display.textContent = slider.value;
                });
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
    new ScoreVoting();
});
export {};
//# sourceMappingURL=ScoreVoting.js.map