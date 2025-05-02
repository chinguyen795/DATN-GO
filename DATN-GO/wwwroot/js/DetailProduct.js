document.addEventListener('DOMContentLoaded', function() {
    // Get DOM elements
    const quantityInput = document.getElementById('quantityInput');
    const decreaseBtn = document.getElementById('decreaseQuantity');
    const increaseBtn = document.getElementById('increaseQuantity');
    const priceElement = document.querySelector('.text-danger.fw-bold.fs-3');
    
    // Get initial price value (remove ₫ and dots)
    const basePrice = parseInt(priceElement.textContent.replace(/\./g, '').replace('₫', '').trim());
    
    // Format price with dots and ₫ symbol
    function formatPrice(price) {
        return price.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "₫";
    }

    // Update total price based on quantity
    function updatePrice(quantity) {
        const totalPrice = basePrice * quantity;
        priceElement.textContent = formatPrice(totalPrice);
    }

    // Handle decrease button click
    decreaseBtn.addEventListener('click', () => {
        let currentValue = parseInt(quantityInput.value);
        if (currentValue > 1) {
            quantityInput.value = currentValue - 1;
            updatePrice(currentValue - 1);
        }
    });

    // Handle increase button click
    increaseBtn.addEventListener('click', () => {
        let currentValue = parseInt(quantityInput.value);
        if (currentValue < 150) { // Maximum quantity limit
            quantityInput.value = currentValue + 1;
            updatePrice(currentValue + 1);
        }
    });

    // Handle direct input changes
    quantityInput.addEventListener('change', () => {
        let value = parseInt(quantityInput.value);
        // Validate input
        if (isNaN(value) || value < 1) value = 1;
        if (value > 150) value = 150;
        quantityInput.value = value;
        updatePrice(value);
    });
});