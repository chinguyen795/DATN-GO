// Basic quantity update and total calculation logic (example)
function parseCurrency(currencyString) {
    return parseFloat(currencyString.replace(/[^0-9]/g, ''));
}

function formatCurrency(number) {
    return number.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' }).replace('₫', 'đ');
}

function updateRowTotal(inputElement) {
    const row = inputElement.closest('tr');
    const priceElement = row.querySelector('.price');
    const totalElement = row.querySelector('.total');
    const quantityInput = row.querySelector('.quantity-input');

    const price = parseCurrency(priceElement.textContent);
    const quantity = parseInt(quantityInput.value);

    if (!isNaN(price) && !isNaN(quantity) && quantity >= 1) {
        const total = price * quantity;
        totalElement.textContent = formatCurrency(total);
    } else {
         // Handle invalid quantity, maybe reset to 1 or show error
         quantityInput.value = 1; // Reset to 1 if invalid
         totalElement.textContent = formatCurrency(price); // Update total based on reset quantity
    }
    updateCartTotal(); // Update overall total whenever a row changes
}

function updateQuantity(buttonOrInput, change) {
    let input;
    if (buttonOrInput.tagName === 'INPUT') {
        input = buttonOrInput; // Called from onchange
    } else { // Called from button click
        const inputGroup = buttonOrInput.closest('.input-group');
        input = inputGroup.querySelector('.quantity-input');
        let currentValue = parseInt(input.value);
        let newValue = currentValue + change;
        if (newValue >= 1) {
            input.value = newValue;
        } else {
            input.value = 1; // Prevent going below 1
        }
    }
    updateRowTotal(input);
}

function updateCartTotal() {
    const rows = document.querySelectorAll('tbody tr');
    let subtotal = 0;
    rows.forEach(row => {
        const totalElement = row.querySelector('.total');
        subtotal += parseCurrency(totalElement.textContent);
    });

    // Update summary section (assuming IDs exist)
    const subtotalElement = document.getElementById('subtotal');
    const grandTotalElement = document.getElementById('grand-total');

    if (subtotalElement) subtotalElement.textContent = formatCurrency(subtotal);
    // Add shipping cost calculation if needed
    if (grandTotalElement) grandTotalElement.textContent = formatCurrency(subtotal); // Assuming free shipping for now
}

// Initial calculation on page load
document.addEventListener('DOMContentLoaded', () => {
    // Existing cart functionality
    setupQuantityHandlers();
    setupDeleteButtons();
    setupDiscountCode();
    setupPaymentMethods();
    setupSeeMoreButton();
    setupCheckboxHandlers();
});

function setupQuantityHandlers() {
    const quantityInputs = document.querySelectorAll('.quantity-input');
    quantityInputs.forEach(input => updateRowTotal(input)); // Calculate initial totals
    updateCartTotal(); // Calculate initial grand total
    document.querySelectorAll('.quantity-input').forEach(input => {
        input.addEventListener('change', updateCartSummary);
    });
}

function setupDeleteButtons() {
    document.querySelectorAll('.bi-trash3').forEach(button => {
        button.addEventListener('click', (event) => {
            event.preventDefault(); // Prevent default link behavior if it's an <a> tag
            const row = event.target.closest('tr');
            if (confirm('Bạn có chắc muốn xóa sản phẩm này?')) {
                row.remove();
                updateCartTotal(); // Recalculate total after deletion
            }
        });
    });
}

function setupDiscountCode() {
    const applyDiscountBtn = document.getElementById('applyDiscountBtn');
    if (applyDiscountBtn) {
        applyDiscountBtn.addEventListener('click', () => {
            const discountCode = document.getElementById('discountCode').value;
            console.log('Applying discount code:', discountCode);
            // Add logic to validate and apply discount code here
            // You might need to update the totals list and recalculate the grand total
            // updateCartTotal(); // Call after applying discount
        });
    }
}

function setupPaymentMethods() {
    document.querySelectorAll('input[name="paymentMethod"]').forEach(radio => {
        radio.addEventListener('change', (event) => {
            console.log('Selected payment method:', event.target.value);
            // Add logic if payment method selection affects anything else
        });
    });
}

function setupSeeMoreButton() {
    const seeMoreBtn = document.getElementById('see-more-btn');
    const seeMoreContainer = document.getElementById('see-more-container');
    const cartItemsBody = document.getElementById('cart-items-body');
    const hiddenRows = cartItemsBody.querySelectorAll('.cart-item-row.cart-item-hidden');

    if (!seeMoreBtn || hiddenRows.length === 0) {
        if(seeMoreContainer) seeMoreContainer.classList.add('cart-item-hidden');
        return;
    }

    seeMoreBtn.addEventListener('click', () => {
        hiddenRows.forEach(row => {
            row.classList.remove('cart-item-hidden');
        });
        seeMoreContainer.classList.add('cart-item-hidden');
    });
}

function setupCheckboxHandlers() {
    const selectAllCheckbox = document.getElementById('selectAllItems');
    const itemCheckboxes = document.querySelectorAll('.item-checkbox');
    
    selectAllCheckbox.addEventListener('change', function() {
        itemCheckboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
        });
        updateCartSummary();
    });

    itemCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            const allChecked = Array.from(itemCheckboxes).every(cb => cb.checked);
            selectAllCheckbox.checked = allChecked;
            updateCartSummary();
        });
    });

    // Initialize the summary
    updateCartSummary();
}

function updateCartSummary() {
    const itemCheckboxes = document.querySelectorAll('.item-checkbox');
    let subtotal = 0;

    itemCheckboxes.forEach(checkbox => {
        if (checkbox.checked) {
            const row = checkbox.closest('tr');
            const quantityInput = row.querySelector('.quantity-input');
            const price = parseFloat(checkbox.dataset.price);
            const quantity = parseInt(quantityInput.value);
            subtotal += price * quantity;
        }
    });

    // Format and display the subtotal
    const formattedSubtotal = formatCurrency(subtotal);
    document.getElementById('subtotal').textContent = formattedSubtotal;
    document.getElementById('grand-total').textContent = formattedSubtotal;
}

function formatCurrency(amount) {
    return amount.toLocaleString('vi-VN', {
        style: 'decimal',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }) + ' đ';
}

