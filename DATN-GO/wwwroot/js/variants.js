// Store all variant types and their values
let variantTypes = [];

// Template for variant type
const variantTypeTemplate = (id) => `
    <div class="variant-type mb-3" data-type-id="${id}">
        <div class="card">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h6 class="mb-0">Loại tùy chọn ${id}</h6>
                    <button type="button" class="btn btn-danger btn-sm" onclick="removeVariantType(${id})">
                        <i class="mdi mdi-delete"></i> Xóa
                    </button>
                </div>
                <div class="form-group">
                    <label>Tên tùy chọn:</label>
                    <input type="text" class="form-control variant-type-name" placeholder="Ví dụ: Màu sắc, Kích thước...">
                </div>
                <div class="variant-values mt-3">
                    <label>Giá trị tùy chọn:</label>
                    <div class="variant-values-container">
                        <div class="d-flex gap-2 mb-2">
                            <input type="text" class="form-control variant-value" placeholder="Nhập giá trị">
                            <button type="button" class="btn btn-outline-danger" onclick="removeVariantValue(this)">
                                <i class="mdi mdi-delete"></i>
                            </button>
                        </div>
                    </div>
                    <button type="button" class="btn btn-outline-danger btn-sm mt-2" onclick="addVariantValue(${id})">
                        <i class="mdi mdi-plus"></i> Thêm giá trị
                    </button>
                </div>
            </div>
        </div>
    </div>
`;

// Store original non-variant sections HTML for restoration
let nonVariantSectionsHTML = null;

// Utility function to get all non-variant sections
function getNonVariantSections() {
    return document.querySelectorAll('.border-start.border-danger:has(.alert-info.alert-icon)');
}

// Save original non-variant sections HTML
function saveNonVariantSections() {
    if (!nonVariantSectionsHTML) {
        const sections = getNonVariantSections();
        nonVariantSectionsHTML = Array.from(sections).map(section => ({
            parent: section.parentElement,
            position: Array.from(section.parentElement.children).indexOf(section),
            html: section.outerHTML
        }));
    }
}

// Add new variant type
document.getElementById('addVariantType').addEventListener('click', function () {
    const typeId = variantTypes.length + 1;
    const container = document.getElementById('variantTypesContainer');
    container.insertAdjacentHTML('beforeend', variantTypeTemplate(typeId));

    variantTypes.push({
        id: typeId,
        name: '',
        values: ['']
    });

    // Remove non-variant sections when first variant type is added
    if (typeId === 1) {
        saveNonVariantSections();
        getNonVariantSections().forEach(section => {
            section.remove();
        });
    }

    updateVariantTable();
});

// Remove variant type
function removeVariantType(typeId) {
    const element = document.querySelector(`[data-type-id="${typeId}"]`);
    if (element) {
        element.remove();
        variantTypes = variantTypes.filter(type => type.id !== typeId);

        // Restore non-variant sections if no variant types remain
        if (variantTypes.length === 0 && nonVariantSectionsHTML) {
            nonVariantSectionsHTML.forEach(section => {
                const parent = section.parent;
                const position = section.position;

                // If there are elements after the position
                if (parent.children.length > position) {
                    parent.children[position].insertAdjacentHTML('beforebegin', section.html);
                } else {
                    // If we need to append at the end
                    parent.insertAdjacentHTML('beforeend', section.html);
                }
            });
        }

        updateVariantTable();
    }
}

// Add new variant value
function addVariantValue(typeId) {
    const variantType = document.querySelector(`[data-type-id="${typeId}"]`);
    const container = variantType.querySelector('.variant-values-container');

    container.insertAdjacentHTML('beforeend', `
        <div class="d-flex gap-2 mb-2">
            <input type="text" class="form-control variant-value" placeholder="Nhập giá trị">
            <button type="button" class="btn btn-outline-danger" onclick="removeVariantValue(this)">
                <i class="mdi mdi-delete"></i>
            </button>
        </div>
    `);

    const variantTypeObj = variantTypes.find(type => type.id === typeId);
    if (variantTypeObj) {
        variantTypeObj.values.push('');
    }

    updateVariantTable();
}

// Remove variant value
function removeVariantValue(button) {
    const valueContainer = button.parentElement;
    const typeContainer = valueContainer.closest('.variant-type');
    const typeId = parseInt(typeContainer.dataset.typeId);

    const variantTypeObj = variantTypes.find(type => type.id === typeId);
    if (variantTypeObj) {
        const valueIndex = Array.from(valueContainer.parentElement.children).indexOf(valueContainer);
        variantTypeObj.values.splice(valueIndex, 1);
    }

    valueContainer.remove();
    updateVariantTable();
}

// Update variant names and values
document.addEventListener('input', function (e) {
    if (e.target.classList.contains('variant-type-name')) {
        const typeContainer = e.target.closest('.variant-type');
        const typeId = parseInt(typeContainer.dataset.typeId);
        const variantType = variantTypes.find(type => type.id === typeId);
        if (variantType) {
            variantType.name = e.target.value;
        }
    } else if (e.target.classList.contains('variant-value')) {
        const typeContainer = e.target.closest('.variant-type');
        const typeId = parseInt(typeContainer.dataset.typeId);
        const valueContainer = e.target.closest('.d-flex');
        const valueIndex = Array.from(valueContainer.parentElement.children).indexOf(valueContainer);

        const variantType = variantTypes.find(type => type.id === typeId);
        if (variantType) {
            variantType.values[valueIndex] = e.target.value;
        }
    }

    updateVariantTable();
});

// Generate all possible combinations of variant values
function generateVariantCombinations() {
    const validTypes = variantTypes.filter(type => type.name && type.values.some(v => v));
    if (validTypes.length === 0) return [];

    function combine(arrays) {
        return arrays.reduce((a, b) =>
            a.flatMap(x => b.map(y => [...x, y])), [[]]);
    }

    return combine(validTypes.map(type => type.values.filter(v => v)));
}

// Update the variant combination table
function updateVariantTable() {
    const tableContainer = document.querySelector('.variant-table');
    const tableBody = document.getElementById('variantTableBody');
    const thead = document.querySelector('.variant-table thead tr');

    const validTypes = variantTypes.filter(type => type.name && type.values.some(v => v));

    if (validTypes.length === 0) {
        tableContainer.style.display = 'none';
        return;
    }

    // Update table headers
    thead.innerHTML = '';
    validTypes.forEach(type => {
        thead.insertAdjacentHTML('beforeend', `<th>${type.name}</th>`);
    });
    thead.insertAdjacentHTML('beforeend', `
        <th>Giá Bán</th>
        <th>Giá Vốn</th>
        <th>Tồn Kho</th>
        <th>KL (kg)</th>
        <th>K.Thước (DxRxC)</th>
        <th>Ảnh</th>
    `);

    // Generate combinations and update table body
    const combinations = generateVariantCombinations();
    // Lấy hình ảnh đã chọn
    let imgSrc = document.getElementById('preview')?.src;
    if (!imgSrc || imgSrc === '#') imgSrc = '';
    tableBody.innerHTML = combinations.map(combo => `
        <tr style="vertical-align: middle;">
            ${combo.map(value => `<td style='min-width: 100px; text-align: center;'>${value}</td>`).join('')}
            <td style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Giá bán"></td>
            <td style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Giá vốn"></td>
            <td style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Số lượng"></td>
            <td style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Khối lượng"></td>
            <td style='min-width: 180px;'>
                <div class="d-flex gap-1">
                    <input type="number" class="form-control form-control-sm" placeholder="D">
                    <input type="number" class="form-control form-control-sm" placeholder="R">
                    <input type="number" class="form-control form-control-sm" placeholder="C">
                </div>
            </td>
            <td style='min-width: 120px; text-align: center;'>
                ${imgSrc ? `<img src="${imgSrc}" alt="Variant Image" style="max-width: 100px; max-height: 100px; object-fit: cover; border-radius: 8px; box-shadow: 0 2px 8px #0001;">` : '<span class="text-muted">Chưa có ảnh</span>'}
            </td>
        </tr>
    `).join('');

    tableContainer.style.display = 'block';
}


function previewImage(input) {
    const preview = document.getElementById('preview');
    const previewDiv = document.getElementById('imagePreview');
    const uploadContent = document.getElementById('uploadContent');
    const dropZone = document.getElementById('dropZone');

    if (input.files && input.files[0]) {
        const reader = new FileReader();

        reader.onload = function (e) {
            preview.src = e.target.result;
            previewDiv.classList.remove('d-none');
            previewDiv.classList.add('d-block');
            uploadContent.classList.add('d-none');
            dropZone.classList.add('bg-white');
            dropZone.classList.remove('bg-light');
        }

        reader.readAsDataURL(input.files[0]);
    }
}

function removeImage(event) {
    event.stopPropagation();
    const input = document.getElementById('productImage');
    const preview = document.getElementById('preview');
    const previewDiv = document.getElementById('imagePreview');
    const uploadContent = document.getElementById('uploadContent');
    const dropZone = document.getElementById('dropZone');

    input.value = '';
    preview.src = '#';
    previewDiv.classList.add('d-none');
    previewDiv.classList.remove('d-block');
    uploadContent.classList.remove('d-none');
    dropZone.classList.remove('bg-white');
    dropZone.classList.add('bg-light');
}
