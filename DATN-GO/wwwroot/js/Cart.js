    function parseCurrency(currencyString) {
        return parseFloat(currencyString.replace(/[^0-9]/g, ''));
    }

    function formatCurrency(number) {
        return number.toLocaleString('vi-VN', {style: 'currency', currency: 'VND' }).replace('?', '?');
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
        quantityInput.value = 1;
    totalElement.textContent = formatCurrency(price);
        }

    updateCartTotal();
    }



    function updateCartTotal() {
        const rows = document.querySelectorAll('tbody tr');
    let subtotal = 0;
        rows.forEach(row => {
            const totalElement = row.querySelector('.total');
    subtotal += parseCurrency(totalElement.textContent);
        });

    const subtotalElement = document.getElementById('subtotal');
    const grandTotalElement = document.getElementById('grand-total');

    if (subtotalElement) subtotalElement.textContent = formatCurrency(subtotal);
    if (grandTotalElement) grandTotalElement.textContent = formatCurrency(subtotal);
    }

    document.addEventListener('DOMContentLoaded', () => {
        setupQuantityHandlers();
    setupDeleteButtons();
    setupDiscountCode();
    setupPaymentMethods();
    setupSeeMoreButton();
    setupCheckboxHandlers();
    });

    function setupQuantityHandlers() {
        const quantityInputs = document.querySelectorAll('.quantity-input');
        quantityInputs.forEach(input => updateRowTotal(input));
    updateCartTotal();
        quantityInputs.forEach(input => {
        input.addEventListener('change', updateCartSummary);
        });
    }


    function setupDiscountCode() {
        const applyDiscountBtn = document.getElementById('applyDiscountBtn');
    if (applyDiscountBtn) {
        applyDiscountBtn.addEventListener('click', () => {
            const discountCode = document.getElementById('discountCode').value;
            console.log('Applying discount code:', discountCode);
        });
        }
    }

    function setupPaymentMethods() {
        document.querySelectorAll('input[name="paymentMethod"]').forEach(radio => {
            radio.addEventListener('change', (event) => {
                console.log('Selected payment method:', event.target.value);
            });
        });
    }

    function setupSeeMoreButton() {
        const seeMoreBtn = document.getElementById('see-more-btn');
    const seeMoreContainer = document.getElementById('see-more-container');
    const cartItemsBody = document.getElementById('cart-items-body');
    const hiddenRows = cartItemsBody.querySelectorAll('.cart-item-row.cart-item-hidden');

    if (!seeMoreBtn || hiddenRows.length === 0) {
            if (seeMoreContainer) seeMoreContainer.classList.add('cart-item-hidden');
    return;
        }

        seeMoreBtn.addEventListener('click', () => {
        hiddenRows.forEach(row => row.classList.remove('cart-item-hidden'));
    seeMoreContainer.classList.add('cart-item-hidden');
        });
    }

    function setupCheckboxHandlers() {
        const selectAllCheckbox = document.getElementById('selectAllItems');
    const itemCheckboxes = document.querySelectorAll('.item-checkbox');

    selectAllCheckbox.addEventListener('change', function () {
        itemCheckboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
        });
    updateCartSummary();
        });

        itemCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const allChecked = Array.from(itemCheckboxes).every(cb => cb.checked);
            selectAllCheckbox.checked = allChecked;
            updateCartSummary();
        });
        });

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

    const formattedSubtotal = formatCurrency(subtotal);
    document.getElementById('subtotal').textContent = formattedSubtotal;
    document.getElementById('grand-total').textContent = formattedSubtotal;
    }

    function formatCurrency(amount) {
        return amount.toLocaleString('vi-VN', {
        style: 'decimal',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
        }) + ' ?';
    }

