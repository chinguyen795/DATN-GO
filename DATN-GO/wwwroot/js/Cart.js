function parseCurrency(str) {
    return parseFloat(str.replace(/[^\d]/g, '')) || 0;
}

function formatCurrency(number) {
    return number.toLocaleString('vi-VN') + ' đ';
}

function updateCartSummary() {
    let total = 0;
    document.querySelectorAll('.item-checkbox').forEach(cb => {
        if (cb.checked) {
            const row = cb.closest('tr');
            const price = parseInt(cb.dataset.price);
            const quantity = parseInt(row.querySelector('.quantity-input').value);
            total += price * quantity;
        }
    });
    document.getElementById('subtotal').textContent = formatCurrency(total);
    document.getElementById('grand-total').textContent = formatCurrency(total);
}

function setupCheckboxHandlers() {
    const selectAll = document.getElementById('selectAllItems');
    const checkboxes = document.querySelectorAll('.item-checkbox');

    // Khi tích chọn "chọn tất cả"
    selectAll.addEventListener('change', function () {
        checkboxes.forEach(cb => cb.checked = this.checked);
        updateCartSummary();
    });

    // Khi tích chọn từng sản phẩm
    checkboxes.forEach(cb => {
        cb.addEventListener('change', function () {
            // Nếu có 1 ô bỏ chọn => bỏ chọn ô tổng
            const allChecked = Array.from(checkboxes).every(cb => cb.checked);
            selectAll.checked = allChecked;

            updateCartSummary();
        });
    });

    updateCartSummary(); // Gọi lúc load
}



document.addEventListener('DOMContentLoaded', setupCheckboxHandlers);