class PollResults {
    constructor() {
        this.initializeAnimations();
        this.initializeTooltips();
    }
    initializeAnimations() {
        // Animate progress bars on load
        const progressBars = document.querySelectorAll('.progress-bar');
        progressBars.forEach((bar, index) => {
            const targetWidth = bar.style.width;
            bar.style.width = '0%';
            bar.style.transition = 'width 0.8s ease-out';
            setTimeout(() => {
                bar.style.width = targetWidth;
            }, 100 + (index * 50));
        });
        // Fade in result items
        const resultItems = document.querySelectorAll('.result-item');
        resultItems.forEach((item, index) => {
            item.style.opacity = '0';
            item.style.transform = 'translateY(20px)';
            item.style.transition = 'opacity 0.5s ease-out, transform 0.5s ease-out';
            setTimeout(() => {
                item.style.opacity = '1';
                item.style.transform = 'translateY(0)';
            }, 200 + (index * 100));
        });
    }
    initializeTooltips() {
        // Initialize Bootstrap tooltips if available
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        if (typeof window.bootstrap !== 'undefined') {
            tooltipTriggerList.forEach(tooltipTriggerEl => {
                new window.bootstrap.Tooltip(tooltipTriggerEl);
            });
        }
    }
}
// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new PollResults();
});
export {};
//# sourceMappingURL=PollResults.js.map