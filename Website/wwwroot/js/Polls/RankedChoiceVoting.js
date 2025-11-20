class RankedChoiceVoting {
    constructor() {
        this.draggedElement = null;
        this.form = document.querySelector('.ranked-choice-form');
        this.container = document.getElementById('ranked-options-container');
        if (this.form && this.container) {
            this.initializeDragAndDrop();
            this.initializeRankSelects();
            this.initializeValidation();
            this.initializeLoadingState();
        }
    }
    initializeDragAndDrop() {
        const options = document.querySelectorAll('.ranked-option');
        options.forEach(option => {
            option.addEventListener('dragstart', (e) => this.handleDragStart(e));
            option.addEventListener('dragend', (e) => this.handleDragEnd(e));
            option.addEventListener('dragover', (e) => this.handleDragOver(e));
            option.addEventListener('drop', (e) => this.handleDrop(e));
        });
    }
    handleDragStart(e) {
        const target = e.target;
        this.draggedElement = target.closest('.ranked-option');
        if (this.draggedElement) {
            this.draggedElement.style.opacity = '0.5';
            if (e.dataTransfer) {
                e.dataTransfer.effectAllowed = 'move';
            }
        }
    }
    handleDragEnd(e) {
        const target = e.target;
        const element = target.closest('.ranked-option');
        if (element) {
            element.style.opacity = '1';
        }
    }
    handleDragOver(e) {
        e.preventDefault();
        if (e.dataTransfer) {
            e.dataTransfer.dropEffect = 'move';
        }
    }
    handleDrop(e) {
        e.preventDefault();
        const target = e.target;
        const dropTarget = target.closest('.ranked-option');
        if (dropTarget && this.draggedElement && dropTarget !== this.draggedElement && this.container) {
            const allOptions = Array.from(this.container.querySelectorAll('.ranked-option'));
            const draggedIndex = allOptions.indexOf(this.draggedElement);
            const dropIndex = allOptions.indexOf(dropTarget);
            if (draggedIndex < dropIndex) {
                dropTarget.after(this.draggedElement);
            }
            else {
                dropTarget.before(this.draggedElement);
            }
            this.updateRanksFromOrder();
        }
    }
    initializeRankSelects() {
        const selects = document.querySelectorAll('.rank-select');
        selects.forEach(select => {
            select.addEventListener('change', () => {
                this.updateOrderFromRanks();
            });
        });
    }
    updateRanksFromOrder() {
        const options = this.container?.querySelectorAll('.ranked-option');
        options?.forEach((option, index) => {
            const select = option.querySelector('.rank-select');
            if (select && select.value !== '0') {
                select.value = (index + 1).toString();
            }
        });
    }
    updateOrderFromRanks() {
        if (!this.container)
            return;
        const options = Array.from(this.container.querySelectorAll('.ranked-option'));
        options.sort((a, b) => {
            const aSelect = a.querySelector('.rank-select');
            const bSelect = b.querySelector('.rank-select');
            const aRank = parseInt(aSelect.value) || 999;
            const bRank = parseInt(bSelect.value) || 999;
            return aRank - bRank;
        });
        options.forEach(option => {
            this.container?.appendChild(option);
        });
    }
    initializeValidation() {
        if (!this.form)
            return;
        this.form.addEventListener('submit', (e) => {
            const selects = this.form?.querySelectorAll('.rank-select');
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
    new RankedChoiceVoting();
});
export {};
//# sourceMappingURL=RankedChoiceVoting.js.map