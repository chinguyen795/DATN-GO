document.addEventListener('DOMContentLoaded', function() {
    // Get all voucher sections and tabs
    const voucherTabs = document.querySelectorAll('[data-voucher-tab]');
    const voucherSections = document.querySelectorAll('.vouchers-content');
    const searchInput = document.querySelector('input[placeholder="Tìm kiếm voucher..."]');
    const searchButton = searchInput.nextElementSibling;

    // Tab switching functionality
    voucherTabs.forEach(tab => {
        tab.addEventListener('click', (e) => {
            e.preventDefault();
            
            // Update tab states
            voucherTabs.forEach(t => {
                t.classList.remove('active');
            });
            tab.classList.add('active');
            
            // Show corresponding section
            const targetSection = tab.getAttribute('data-voucher-tab');
            voucherSections.forEach(section => {
                if (section.id === targetSection + 'Vouchers') {
                    section.classList.remove('d-none');
                } else {
                    section.classList.add('d-none');
                }
            });
        });
    });

    // Search functionality
    function performSearch() {
        const searchTerm = searchInput.value.toLowerCase().trim();
        const activeSection = document.querySelector('.vouchers-content:not(.d-none)');
        const voucherItems = activeSection.querySelectorAll('.col-12');

        voucherItems.forEach(item => {
            const voucherText = item.textContent.toLowerCase();
            if (voucherText.includes(searchTerm)) {
                item.style.display = '';
            } else {
                item.style.display = 'none';
            }
        });
    }

    searchInput.addEventListener('keyup', (e) => {
        if (e.key === 'Enter') {
            performSearch();
        }
    });

    searchButton.addEventListener('click', performSearch);
});