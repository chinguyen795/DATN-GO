function handleProvinceSelect(provinceId, districtId, communeId) {
    const provinceSelect = document.getElementById(provinceId);
    const districtSelect = document.getElementById(districtId);
    const communeSelect = document.getElementById(communeId);

    if (provinceSelect) {
        provinceSelect.addEventListener('change', function () {
            if (this.value) {
                districtSelect.disabled = false;
                // Here you would typically fetch districts for the selected province
                // and populate the district select
            } else {
                districtSelect.disabled = true;
                communeSelect.disabled = true;
                districtSelect.value = '';
                communeSelect.value = '';
            }
        });
    }

    if (districtSelect) {
        districtSelect.addEventListener('change', function () {
            if (this.value) {
                communeSelect.disabled = false;
                // Here you would typically fetch communes for the selected district
                // and populate the commune select
            } else {
                communeSelect.disabled = true;
                communeSelect.value = '';
            }
        });
    }
}