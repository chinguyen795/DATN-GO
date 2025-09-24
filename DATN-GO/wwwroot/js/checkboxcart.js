﻿$(function () {
    const userId = $('#userId').val();
    const API_UPDATE_SELECTION = (window.API_BASE_URL || '') + 'cart/update-selection';

    // ---------- utils ----------
    const debounce = (fn, ms = 250) => {
        let t;
        return (...a) => {
            clearTimeout(t);
            t = setTimeout(() => fn(...a), ms);
        };
    };

    let lock = false, pending = false;

    const getNumber = txt => parseFloat(String(txt || '0').replace(/[^\d]/g, '')) || 0;

    function getSelectedCartIds() {
        const ids = [];
        $('.item-checkbox:checked').each(function () {
            const id = $(this).data('cart-id') || $(this).closest('tr').data('cart-id');
            if (id !== undefined) ids.push(id);
        });
        return ids;
    }

    function updateSelectionAPI(selectedIds, cb) {
        $.ajax({
            url: API_UPDATE_SELECTION,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(selectedIds || []),
            success: () => cb?.(),
            error: (xhr, s, e) => {
                console.error('updateSelectionAPI:', s, e, xhr?.responseText);
                cb?.();
            }
        });
    }

    // ---------- sync subtotal ----------
    function syncRowTotalsFromDOM() {
        document.querySelectorAll('.item-checkbox').forEach(cb => {
            const row = cb.closest('tr');
            if (!row) return;
            const qtyEl = row.querySelector('.quantity-input');
            const priceEl = row.querySelector('.price');

            const qty = parseInt(qtyEl?.value, 10) || parseInt(cb.dataset.quantity || '1', 10) || 1;
            const price = getNumber(priceEl?.textContent);
            const total = price * qty;

            cb.dataset.quantity = String(qty);
            cb.dataset.total = String(total);

            const totalCell = row.querySelector('.total');
            if (totalCell) totalCell.textContent = total.toLocaleString('vi-VN') + ' đ';
        });
    }

    // ---------- refresh voucher SHOP (1 store) ----------
    function refreshStoreVoucherOptions(storeId, done) {
        const $sel = $(`.store-voucher-select[data-store-id="${storeId}"]`);
        if ($sel.length === 0) { done?.(); return; }

        const keep = $sel.val() || '';
        const selectedCartIds = getSelectedCartIds();

        fetch('/Cart/UpdateStoreVoucherOptions', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ storeId: Number(storeId), selectedCartIds })
        })
            .then(r => r.ok ? r.text() : Promise.reject(r.status))
            .then(html => {
                const $opts = $('<div>').html(html).find('option');
                try { $sel.selectpicker('destroy'); } catch { }
                $sel.empty().append($opts);

                if (keep && $sel.find(`option[value="${keep}"]`).length) $sel.val(keep);
                else $sel.val('');

                $sel.selectpicker(); // re-init
            })
            .catch(() => { })
            .finally(() => done?.());
    }

    function refreshAllStoreVouchers(done) {
        const $sels = $('.store-voucher-select');
        let left = $sels.length;
        if (left === 0) { done?.(); return; }
        $sels.each(function () {
            const sid = $(this).data('store-id');
            refreshStoreVoucherOptions(sid, () => { if (--left === 0) done?.(); });
        });
    }

    // ---------- refresh voucher SÀN ----------
    function updatePlatformVoucherDropdown(cb) {
        const selectedIds = getSelectedCartIds();
        const $sel = $('#voucherSelect');

        updateSelectionAPI(selectedIds, function () {
            if (selectedIds.length === 0) {
                try { $sel.selectpicker('destroy'); } catch { }
                $sel.empty();
                $sel.selectpicker(); // re-init rỗng cũng ok
                window.recalcAllDiscountsAndTotals?.();
                return cb?.();
            }

            $.ajax({
                url: '/Cart/UpdateVoucherDropdown',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(selectedIds),
                success: function (partialHtml) {
                    const prev = $sel.selectpicker('val') || '';
                    const $opts = $('<div>').html(partialHtml).find('option');

                    try { $sel.selectpicker('destroy'); } catch { }
                    $sel.empty().append($opts);
                    // ưu tiên value được server “selected”, fallback về prev nếu còn tồn tại
                    const fromServer = $sel.find('option[selected]').last().val();
                    if (fromServer) $sel.val(String(fromServer));
                    else if (prev && $sel.find(`option[value="${prev}"]`).length) $sel.val(prev);
                    else $sel.val('');

                    $sel.selectpicker(); // re-init thay vì refresh
                    fixSelectpickerButtonText($sel);

                    refreshAllStoreVouchers(() => { window.recalcAllDiscountsAndTotals?.(); cb?.(); });
                },
                error: function () {
                    try { $sel.selectpicker('destroy'); } catch { }
                    $sel.empty().selectpicker();
                    refreshAllStoreVouchers(() => { window.recalcAllDiscountsAndTotals?.(); cb?.(); });
                }
            });
        });
    }

    function fixSelectpickerButtonText($sel) {
        const text = ($sel.find('option:selected').text() || '').trim();
        const $btn = $sel.parent().find('button.dropdown-toggle');
        $btn.removeClass('bs-placeholder').attr('title', text || '— Không dùng —');
        $btn.find('.filter-option-inner-inner').text(text || '— Không dùng —');
    }

    // ---------- phí ship ----------
    function updateShippingFee() {
        const addressId = $('#addressSelect').val();
        if (!addressId || !userId) {
            $('#shipping-fee').text('0₫');
            window.recalcAllDiscountsAndTotals?.();
            return;
        }

        fetch('/Cart/GetShippingFee', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ addressId })
        })
            .then(r => { if (!r.ok) throw new Error(r.status); return r.json(); })
            .then(data => {
                const fee = Array.isArray(data) ? data.reduce((s, g) => s + (g.shippingFee || 0), 0) : 0;
                $('#shipping-fee').text(fee.toLocaleString('vi-VN') + '₫');
                window.recalcAllDiscountsAndTotals?.();
            })
            .catch(() => {
                $('#shipping-fee').text('0₫');
                window.recalcAllDiscountsAndTotals?.();
            });
    }

    // ---------- wrapper có lock + debounce ----------
    function refreshVoucherAndShipping() {
        if (lock) { pending = true; return; }
        lock = true;

        const selectedIds = getSelectedCartIds();
        updateSelectionAPI(selectedIds, function () {
            updateShippingFee();
            updatePlatformVoucherDropdown(function () {
                lock = false;
                if (pending) { pending = false; debouncedRefresh(); }
            });
        });
    }

    const debouncedRefresh = debounce(refreshVoucherAndShipping, 250);

    // ---------- events ----------
    $('#voucherSelect').off('changed.bs.select change').on('changed.bs.select change', function () {
        const $sel = $(this);
        const val = $sel.selectpicker('val');

        const vid = parseInt(val || '0', 10) || 0;
        fetch('/Cart/SaveSelectedPlatformVoucher', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(vid)
        }).catch(() => { });

        const text = ($sel.find('option:selected').text() || '').trim();
        const $btn = $sel.parent().find('button.dropdown-toggle');
        $btn.removeClass('bs-placeholder').attr('title', text || '— Không dùng —');
        $btn.find('.filter-option-inner-inner').text(text || '— Không dùng —');

        window.recalcAllDiscountsAndTotals?.();
    });

    $(document).on('changed.bs.select change', '.store-voucher-select', function () {
        window.recalcAllDiscountsAndTotals?.();
    });

    $('#addressSelect').on('change', updateShippingFee);

    // lần đầu
    syncRowTotalsFromDOM();
    if ($('.item-checkbox:checked').length > 0) {
        window.recalcAllDiscountsAndTotals?.();
    } else {
        refreshAllStoreVouchers(() => window.recalcAllDiscountsAndTotals?.());
    }

    $('#selectAllItems').on('change', function () {
        const checked = $(this).is(':checked');
        $('.item-checkbox').prop('checked', checked);
        syncRowTotalsFromDOM();
        debouncedRefresh();
    });

    $('.item-checkbox').on('change', function () {
        const selected = $('.item-checkbox:checked').length;
        const total = $('.item-checkbox').length;
        $('#selectAllItems').prop('checked', selected === total);
        syncRowTotalsFromDOM();
        debouncedRefresh();
    });

    // nơi khác đổi số lượng
    document.addEventListener('quantity-updated', () => {
        syncRowTotalsFromDOM();
        window.recalcAllDiscountsAndTotals?.();
    });

    // input trực tiếp số lượng
    $(document).on('input', '.quantity-input', debounce(() => {
        syncRowTotalsFromDOM();
        window.recalcAllDiscountsAndTotals?.();
    }, 200));
});