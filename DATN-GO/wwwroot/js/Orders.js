document.addEventListener('DOMContentLoaded', function() {
    // Order tab switching functionality
    const orderTabs = document.querySelectorAll('.nav-tabs .nav-link');
    const orderItems = document.querySelectorAll('.order-item');
    const searchInput = document.getElementById('orderSearch');
    const searchBtn = document.getElementById('searchBtn');
    const sortSelect = document.getElementById('orderSort');

    // Tab switching
    orderTabs.forEach(tab => {
        tab.addEventListener('click', (e) => {
            e.preventDefault();
            // Remove active class from all tabs
            orderTabs.forEach(t => t.classList.remove('active'));
            // Add active class to clicked tab
            tab.classList.add('active');

            const status = tab.textContent.trim();
            filterOrders(status);
        });
    });
});