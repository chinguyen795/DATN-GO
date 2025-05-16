document.addEventListener('DOMContentLoaded', function() {
    // Handle Orders Tab Switching
    const orderTabs = document.querySelectorAll('[data-order-tab]');
    const orderContents = document.querySelectorAll('[data-order-content]');

    orderTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            // Remove active class from all tabs
            orderTabs.forEach(t => t.classList.remove('active'));
            // Add active class to clicked tab
            tab.classList.add('active');
            
            // Show selected content, hide others
            const tabName = tab.getAttribute('data-order-tab');
            orderContents.forEach(content => {
                if (content.getAttribute('data-order-content') === tabName) {
                    content.classList.remove('d-none');
                } else {
                    content.classList.add('d-none');
                }
            });
        });
    });

    // Handle Voucher Tab Switching
    const voucherTabs = document.querySelectorAll('[data-voucher-tab]');
    const voucherContents = document.querySelectorAll('[data-voucher-content]');

    voucherTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            // Remove active class from all tabs
            voucherTabs.forEach(t => t.classList.remove('active'));
            // Add active class to clicked tab
            tab.classList.add('active');
            
            // Show selected content, hide others
            const tabName = tab.getAttribute('data-voucher-tab');
            voucherContents.forEach(content => {
                if (content.getAttribute('data-voucher-content') === tabName) {
                    content.classList.remove('d-none');
                } else {
                    content.classList.add('d-none');
                }
            });
        });
    });

    // Profile Image Preview
    function previewImage(input) {
        if (input.files && input.files[0]) {
            const reader = new FileReader();
            reader.onload = function(e) {
                document.querySelector('.profile-avatar').src = e.target.result;
            }
            reader.readAsDataURL(input.files[0]);
        }
    }

    window.previewImage = previewImage;
});