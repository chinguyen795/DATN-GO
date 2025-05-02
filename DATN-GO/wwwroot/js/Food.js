document.addEventListener('DOMContentLoaded', function() {
    // Initialize checkbox functionality
    const selectAllCheckbox = document.getElementById('selectAllProducts');
    const productCheckboxes = document.querySelectorAll('.product-checkbox');
    const bulkActionButtons = document.getElementById('bulkActionButtons');

    // Handle select all checkbox - simply check/uncheck all
    selectAllCheckbox.addEventListener('change', function() {
        productCheckboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
        });
        bulkActionButtons.style.display = this.checked ? 'block' : 'none';
    });

    // Handle individual product checkboxes - just show/hide buttons
    productCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            const hasCheckedItems = document.querySelectorAll('.product-checkbox:checked').length > 0;
            bulkActionButtons.style.display = hasCheckedItems ? 'block' : 'none';
        });
    });

    // Initialize tooltips
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
});

 // Category management
 document.getElementById('platformMainCategory').addEventListener('change', function(e) {
    // Hide all subcategory selects
    document.querySelectorAll('[id$="Subcategories"]').forEach(el => {
        el.classList.add('d-none');
        el.classList.remove('d-block');
    });

    // Show relevant subcategory select
    const subcategoryId = e.target.value + 'Subcategories';
    const subcategoryEl = document.getElementById(subcategoryId);
    if (subcategoryEl) {
        subcategoryEl.classList.remove('d-none');
        subcategoryEl.classList.add('d-block');
        // Add animation classes
        subcategoryEl.classList.add('fade', 'show');
    }
});

// Form submission
document.getElementById('productForm').addEventListener('submit', function(e) {
    e.preventDefault();
    // Add your form submission logic here
    console.log('Form submitted');
});