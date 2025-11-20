class PollResults {
    constructor() {
        this.initializeAnimations();
        this.initializeTooltips();
    }

    private initializeAnimations(): void {
        // Animate progress bars on load
        const progressBars = document.querySelectorAll('.progress-bar') as NodeListOf<HTMLElement>;

        progressBars.forEach((bar, index) => {
            const targetWidth = bar.style.width;
            bar.style.width = '0%';
            bar.style.transition = 'width 0.8s ease-out';

            setTimeout(() => {
                bar.style.width = targetWidth;
            }, 100 + (index * 50));
        });

        // Fade in result items
        const resultItems = document.querySelectorAll('.result-item') as NodeListOf<HTMLElement>;

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

    private initializeTooltips(): void {
        // Initialize Bootstrap tooltips if available
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');

        if (typeof (window as any).bootstrap !== 'undefined') {
            tooltipTriggerList.forEach(tooltipTriggerEl => {
                new (window as any).bootstrap.Tooltip(tooltipTriggerEl);
            });
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new PollResults();
});

export {};
