class AvailabilityGrid {
    constructor() {
        this.form = document.querySelector('.availability-grid-form');
        if (this.form) {
            this.initializeCellStyling();
            this.initializeLoadingState();
        }
    }
    initializeCellStyling() {
        const cells = document.querySelectorAll('.availability-grid-table input[type="radio"]');
        cells.forEach(cell => {
            cell.addEventListener('change', () => {
                this.updateCellStyle(cell);
            });
            // Initialize styling for pre-selected cells
            if (cell.checked) {
                this.updateCellStyle(cell);
            }
        });
    }
    updateCellStyle(cell) {
        const row = cell.closest('tr');
        if (!row)
            return;
        const allCellsInRow = row.querySelectorAll('input[type="radio"]');
        const cellsInRow = row.querySelectorAll('td');
        // Reset all cells in row
        cellsInRow.forEach((td, index) => {
            if (index > 0) { // Skip first column (label)
                td.classList.remove('table-success', 'table-warning', 'table-danger');
            }
        });
        // Apply styling to selected cell
        allCellsInRow.forEach((input, index) => {
            if (input.checked) {
                const availability = input.dataset.availability;
                const cell = cellsInRow[index + 1]; // +1 because first column is label
                if (availability === 'yes') {
                    cell.classList.add('table-success');
                }
                else if (availability === 'maybe') {
                    cell.classList.add('table-warning');
                }
                else if (availability === 'no') {
                    cell.classList.add('table-danger');
                }
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
    new AvailabilityGrid();
});
export {};
//# sourceMappingURL=AvailabilityGrid.js.map