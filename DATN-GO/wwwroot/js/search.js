document.addEventListener('DOMContentLoaded', function () {
    const images = document.querySelectorAll('.clickable-image');
    const modalImage = document.getElementById('modalImage');

    images.forEach(img => {
        img.addEventListener('click', function () {
            const src = this.getAttribute('data-src');
            modalImage.src = src;
            $('#imageModal').modal('show'); // jQuery lúc này đã có
        });
    });
});


jQuery(document).ready(function () {
    jQuery('input[name="dateRange"]').daterangepicker({
        autoUpdateInput: false,
        singleDatePicker: true,
        locale: {
            cancelLabel: 'Clear'
        }
    });
    jQuery('input[name="dateRange"]').on('apply.daterangepicker', function (ev, picker) {
        jQuery(this).val(picker.startDate.format('MM/DD/YYYY'));
    });
    jQuery('input[name="dateRange"]').on('cancel.daterangepicker', function (ev, picker) {
        jQuery(this).val('');
    });
});
document.addEventListener('DOMContentLoaded', function () {
    const viewButtons = document.querySelectorAll('.view-store');

    viewButtons.forEach(button => {
        button.addEventListener('click', function () {
            const name = this.getAttribute('data-name');
            const owner = this.getAttribute('data-owner');
            const date = this.getAttribute('data-date');
            const status = this.getAttribute('data-status');
            const image = this.getAttribute('data-image');

            document.getElementById('storeName').innerText = name;
            document.getElementById('storeOwner').innerText = owner;
            document.getElementById('storeDate').innerText = date;
            document.getElementById('storeStatus').innerText = status;
            document.getElementById('storeImage').src = image;
        });
    });
});