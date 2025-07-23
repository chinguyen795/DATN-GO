document.addEventListener('DOMContentLoaded', function () {
    const quantityInput = document.getElementById('quantityInput');
    const decreaseBtn = document.getElementById('decreaseQuantity');
    const increaseBtn = document.getElementById('increaseQuantity');
    const priceElement = document.querySelector('.text-danger.fw-bold.fs-3');

    const basePriceText = priceElement?.textContent?.replace(/\./g, '').replace('₫', '').trim() || "0";
    const basePrice = parseInt(basePriceText) || 0;

    function formatPrice(price) {
        return price.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "₫";
    }

    function updatePrice(quantity) {
        const totalPrice = basePrice * quantity;
        priceElement.textContent = formatPrice(totalPrice);
    }

    // Đảm bảo input có giá trị hợp lệ ngay khi load
    let initialValue = parseInt(quantityInput.value);
    if (isNaN(initialValue) || initialValue < 1) {
        initialValue = 1;
        quantityInput.value = initialValue;
    }
    updatePrice(initialValue);

    // Tăng số lượng
    increaseBtn.addEventListener('click', () => {
        let currentValue = parseInt(quantityInput.value);
        if (currentValue < 150) {
            const newValue = currentValue + 1;
            quantityInput.value = newValue.toString();
            updatePrice(newValue);
        }
    });

    // Giảm số lượng
    decreaseBtn.addEventListener('click', () => {
        let currentValue = parseInt(quantityInput.value);
        if (currentValue > 1) {
            const newValue = currentValue - 1;
            quantityInput.value = newValue.toString();
            updatePrice(newValue);
        }
    }); 

    quantityInput.addEventListener('change', () => {
        let value = parseInt(quantityInput.value);
        if (isNaN(value) || value < 1) value = 1;
        if (value > 150) value = 150;
        quantityInput.value = value;
        updatePrice(value); quantityInput.addEventListener('change', () => {
            let value = quantityInput.valueAsNumber;
            if (isNaN(value) || value < 1) value = 1;
            if (value > 150) value = 150;
            quantityInput.valueAsNumber = value;
            updatePrice(value);
        });

    });
});
