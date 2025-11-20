/**
 * PollAdmin.ts
 * Handles admin poll management interactions
 */
class PollAdmin {
    constructor() {
        this.initializeConfirmations();
        this.initializeSearch();
    }
    /**
     * Initialize confirmation dialogs for Close and Archive actions
     */
    initializeConfirmations() {
        // Close poll confirmation
        const closeForms = document.querySelectorAll('.close-poll-form');
        closeForms.forEach(form => {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                const confirmed = confirm('Sind Sie sicher, dass Sie diese Umfrage schließen möchten? Dies kann nicht rückgängig gemacht werden.');
                if (confirmed) {
                    form.submit();
                }
            });
        });
        // Archive poll confirmation
        const archiveForms = document.querySelectorAll('.archive-poll-form');
        archiveForms.forEach(form => {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                const confirmed = confirm('Sind Sie sicher, dass Sie diese Umfrage archivieren möchten?');
                if (confirmed) {
                    form.submit();
                }
            });
        });
    }
    /**
     * Initialize search/filter functionality
     */
    initializeSearch() {
        const searchInput = document.getElementById('searchTerm');
        const filterForm = document.getElementById('filterForm');
        if (searchInput && filterForm) {
            // Auto-submit on filter change (optional enhancement)
            const selects = filterForm.querySelectorAll('select');
            selects.forEach(select => {
                select.addEventListener('change', () => {
                    // Optional: Auto-submit form when filters change
                    // filterForm.submit();
                });
            });
            // Search on Enter key
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    filterForm.submit();
                }
            });
        }
    }
}
// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new PollAdmin();
});
export {};
//# sourceMappingURL=PollAdmin.js.map