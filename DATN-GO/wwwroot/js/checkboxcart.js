$(document).ready(function () {
    const userId = $('#userId').val();
    console.log("User ID:", userId);

    function getSelectedCartIds() {
        const selectedIds = [];
        $('.item-checkbox:checked').each(function () {
            const cartId = $(this).data('cart-id') || $(this).closest('tr').data('cart-id');
            if (cartId !== undefined) selectedIds.push(cartId);
        });
        console.log("Selected cart IDs:", selectedIds);
        return selectedIds;
    }

    function updateSelectionAPI(selectedIds, callback) {
        console.log("Sending selected cart IDs to API:", selectedIds);
        $.ajax({
            url: API_BASE_URL + 'cart/update-selection',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(selectedIds),
            success: function () {
                console.log("Update selection success.");
                if (typeof callback === 'function') callback();
            },
            error: function (xhr, status, error) {
                console.error("Error in updateSelectionAPI:", status, error, xhr.responseText);
            }
        });
    }

    function updateVoucherDropdown(callback) {
        const selectedIds = getSelectedCartIds();

        updateSelectionAPI(selectedIds, function () {
            const $voucherSelect = $('#voucherSelect');

            if (selectedIds.length === 0) {
                console.log("No selected cart items, clearing voucher dropdown.");
                $voucherSelect.selectpicker('destroy');
                $voucherSelect.empty().selectpicker();
                if (typeof callback === 'function') callback();
                return;
            }

            console.log("Updating voucher dropdown with selected cart IDs:", selectedIds);

            $.ajax({
                url: '/Cart/UpdateVoucherDropdown',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(selectedIds),
                success: function (partialHtml) {
                    console.log("Voucher dropdown updated successfully.");
                    $voucherSelect.selectpicker('destroy');
                    $voucherSelect.empty().append(partialHtml).selectpicker();
                    if (typeof callback === 'function') callback();
                },
                error: function (xhr, status, error) {
                    console.error("Error updating voucher dropdown:", status, error, xhr.responseText);
                    if (typeof callback === 'function') callback();
                }
            });
        });
    }

    function updateShippingFee() {
        const addressId = $('#addressSelect').val();
        if (!addressId || !userId) {
            console.warn("Missing addressId or userId. addressId:", addressId, "userId:", userId);
            return;
        }

        console.log("Fetching shipping fee with:", { userId, addressId });

        fetch('/Cart/GetShippingFee', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ addressId })
        })
            .then(response => {
                if (!response.ok) {
                    console.error("API /Cart/GetShippingFee returned error:", response.status);
                    throw new Error("HTTP error " + response.status);
                }
                return response.json();
            })
            .then(data => {
                console.log("Shipping groups returned:", data);
                if (!Array.isArray(data) || data.length === 0) {
                    console.warn("No shipping group data received.");
                    $('#shipping-fee').text('0₫');
                    updateGrandTotal(0);
                    return;
                }
                const totalShippingFee = data.reduce((sum, group) => sum + group.shippingFee, 0);
                $('#shipping-fee').text(totalShippingFee.toLocaleString('vi-VN') + '₫');
                updateGrandTotal(totalShippingFee);
            })
            .catch(err => {
                console.error("Lỗi khi tính phí vận chuyển:", err);
                $('#shipping-fee').text('0₫');
                updateGrandTotal(0);
            });
    }

    function parseCurrency(str) {
        if (!str) return 0;
        return Number(str.replace(/[^\d-]/g, '')) || 0;
    }



    function updateGrandTotal(shippingFee) {
        const subtotal = parseCurrency($('#subtotal').text());
        const discount = parseCurrency($('#voucher-discount').text());
        const total = subtotal + shippingFee - discount;
        $('#grand-total').text(total.toLocaleString('vi-VN') + '₫');
    }

    function handleVoucherChange() {
        const $selected = $('#voucherSelect option:selected');
        const reduce = parseCurrency($selected.data('reduce') || '0') / 100;
        const minOrder = parseCurrency($selected.data('min-order') || '0');
        const subtotal = parseCurrency($('#subtotal').text());

        console.log("Voucher selected – Reduce:", reduce, "MinOrder:", minOrder, "Subtotal:", subtotal);

        $('#voucher-discount').text(reduce.toLocaleString('vi-VN') + '₫');


        const shippingFee = parseCurrency($('#shipping-fee').text());
        updateGrandTotal(shippingFee);
    }

    $('#voucherSelect').on('changed.bs.select change', handleVoucherChange);

    $('#addressSelect').on('change', function () {
        console.log("Address changed:", $(this).val());
        updateShippingFee();
    });

    if ($('.item-checkbox:checked').length > 0) {
        console.log("Checkboxes already selected on page load.");
        updateVoucherDropdown(function () {
            updateShippingFee();
        });
    } else {
        console.log("No item selected on page load.");
    }

    $('#selectAllItems').on('change', function () {
        const checked = $(this).is(':checked');
        $('.item-checkbox').prop('checked', checked);
        console.log("Select all toggled:", checked);

        updateVoucherDropdown(function () {
            updateShippingFee();
        });
    });

    $('.item-checkbox').on('change', function () {
        const selected = $('.item-checkbox:checked').length;
        const total = $('.item-checkbox').length;
        $('#selectAllItems').prop('checked', selected === total);

        updateVoucherDropdown(function () {
            updateShippingFee();
        });
    });
});
