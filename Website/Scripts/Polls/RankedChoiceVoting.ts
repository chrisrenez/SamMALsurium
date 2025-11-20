class RankedChoiceVoting {
    private form: HTMLFormElement | null;
    private container: HTMLElement | null;
    private draggedElement: HTMLElement | null = null;

    constructor() {
        this.form = document.querySelector('.ranked-choice-form');
        this.container = document.getElementById('ranked-options-container');

        if (this.form && this.container) {
            this.initializeDragAndDrop();
            this.initializeRankSelects();
            this.initializeValidation();
            this.initializeLoadingState();
        }
    }

    private initializeDragAndDrop(): void {
        const options = document.querySelectorAll('.ranked-option') as NodeListOf<HTMLElement>;

        options.forEach(option => {
            option.addEventListener('dragstart', (e) => this.handleDragStart(e));
            option.addEventListener('dragend', (e) => this.handleDragEnd(e));
            option.addEventListener('dragover', (e) => this.handleDragOver(e));
            option.addEventListener('drop', (e) => this.handleDrop(e));
        });
    }

    private handleDragStart(e: DragEvent): void {
        const target = e.target as HTMLElement;
        this.draggedElement = target.closest('.ranked-option');

        if (this.draggedElement) {
            this.draggedElement.style.opacity = '0.5';
            if (e.dataTransfer) {
                e.dataTransfer.effectAllowed = 'move';
            }
        }
    }

    private handleDragEnd(e: DragEvent): void {
        const target = e.target as HTMLElement;
        const element = target.closest('.ranked-option') as HTMLElement;
        if (element) {
            element.style.opacity = '1';
        }
    }

    private handleDragOver(e: DragEvent): void {
        e.preventDefault();
        if (e.dataTransfer) {
            e.dataTransfer.dropEffect = 'move';
        }
    }

    private handleDrop(e: DragEvent): void {
        e.preventDefault();

        const target = e.target as HTMLElement;
        const dropTarget = target.closest('.ranked-option');

        if (dropTarget && this.draggedElement && dropTarget !== this.draggedElement && this.container) {
            const allOptions = Array.from(this.container.querySelectorAll('.ranked-option'));
            const draggedIndex = allOptions.indexOf(this.draggedElement);
            const dropIndex = allOptions.indexOf(dropTarget as HTMLElement);

            if (draggedIndex < dropIndex) {
                dropTarget.after(this.draggedElement);
            } else {
                dropTarget.before(this.draggedElement);
            }

            this.updateRanksFromOrder();
        }
    }

    private initializeRankSelects(): void {
        const selects = document.querySelectorAll('.rank-select') as NodeListOf<HTMLSelectElement>;

        selects.forEach(select => {
            select.addEventListener('change', () => {
                this.updateOrderFromRanks();
            });
        });
    }

    private updateRanksFromOrder(): void {
        const options = this.container?.querySelectorAll('.ranked-option') as NodeListOf<HTMLElement>;

        options?.forEach((option, index) => {
            const select = option.querySelector('.rank-select') as HTMLSelectElement;
            if (select && select.value !== '0') {
                select.value = (index + 1).toString();
            }
        });
    }

    private updateOrderFromRanks(): void {
        if (!this.container) return;

        const options = Array.from(this.container.querySelectorAll('.ranked-option') as NodeListOf<HTMLElement>);

        options.sort((a, b) => {
            const aSelect = a.querySelector('.rank-select') as HTMLSelectElement;
            const bSelect = b.querySelector('.rank-select') as HTMLSelectElement;
            const aRank = parseInt(aSelect.value) || 999;
            const bRank = parseInt(bSelect.value) || 999;
            return aRank - bRank;
        });

        options.forEach(option => {
            this.container?.appendChild(option);
        });
    }

    private initializeValidation(): void {
        if (!this.form) return;

        this.form.addEventListener('submit', (e) => {
            const selects = this.form?.querySelectorAll('.rank-select') as NodeListOf<HTMLSelectElement>;
            const ranks = Array.from(selects)
                .map(select => parseInt(select.value))
                .filter(rank => rank > 0);

            const uniqueRanks = new Set(ranks);

            if (ranks.length !== uniqueRanks.size) {
                e.preventDefault();
                alert('Bitte stellen Sie sicher, dass jeder Rang nur einmal vergeben wird.');
            }
        });
    }

    private initializeLoadingState(): void {
        if (!this.form) return;

        this.form.addEventListener('submit', () => {
            const submitBtn = this.form?.querySelector('button[type="submit"]') as HTMLButtonElement;
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
    new RankedChoiceVoting();
});

export {};
