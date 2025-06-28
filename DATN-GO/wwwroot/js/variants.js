// Store all variant types and their values
let variantTypes = [];
// Lưu trữ ảnh cho từng combination (theo key)
let variantImages = {}

// Template for variant type
const variantTypeTemplate = (id) => `
    <div class="variant-type mb-3 w-100" data-type-id="${id}">
        <div class="card w-100" style="width:100vw;min-width:73vw;max-width:100vw;">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h6 class="mb-0">Loại tùy chọn ${id}</h6>
                    <button type="button" class="btn btn-danger btn-sm" onclick="removeVariantType(${id})" style="text-transform: capitalize;">
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
                        <div class="d-flex align-items-center mb-2">
                            <input type="text" class="form-control variant-value" placeholder="Nhập giá trị" style="margin-right:16px;">
                            <button type="button" class="btn btn-outline-danger" onclick="removeVariantValue(this)">
                                <i class="mdi mdi-delete"></i>
                            </button>
                        </div>
                    </div>
                    <button type="button" class="btn btn-outline-danger btn-sm mt-2" onclick="addVariantValue(${id})" style="text-transform: capitalize;">
                        <i class="mdi mdi-plus"></i> Thêm giá trị
                    </button>
                </div>
            </div>
        </div>
    </div>
`;

// Lưu ảnh cho biến thể
function saveVariantImage(typeId, imgSrc) {
    const variantType = variantTypes.find(type => type.id === typeId);
    if (variantType) {
        variantType.imgSrc = imgSrc; // Lưu ảnh vào biến thể
    }
}
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

// Thêm biến thể mới
document.getElementById('addVariantType').addEventListener('click', function () {
    const typeId = variantTypes.length + 1;
    const container = document.getElementById('variantTypesContainer');
    container.insertAdjacentHTML('beforeend', variantTypeTemplate(typeId));

    variantTypes.push({
        id: typeId,
        name: '',
        values: [''],
        imgSrc: '' // Thêm trường ảnh cho mỗi biến thể
    });

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
        checkAndShowNoVariantSections();
    }
}

// Thêm giá trị mới cho biến thể
function addVariantValue(typeId) {
    const variantType = document.querySelector(`[data-type-id="${typeId}"]`);
    const container = variantType.querySelector('.variant-values-container');

    // Thêm giá trị mới vào phần tử giao diện mà không làm mất giá trị cũ
    container.insertAdjacentHTML('beforeend', `
        <div class="d-flex align-items-center gap-2 mb-2">
            <input type="text" class="form-control variant-value me-2" placeholder="Nhập giá trị">
            <button type="button" class="btn btn-outline-danger" onclick="removeVariantValue(this)">
                <i class="mdi mdi-delete"></i>
            </button>
        </div>
    `);

    // Cập nhật mảng variantTypes mà không làm mất giá trị cũ
    const variantTypeObj = variantTypes.find(type => type.id === typeId);
    if (variantTypeObj) {
        variantTypeObj.values.push(''); // Thêm giá trị mới vào mảng giá trị
    }

    updateVariantTable();
}


// Xóa giá trị khi nhấn nút xóa
function removeVariantValue(button) {
    const valueContainer = button.parentElement;
    const typeContainer = valueContainer.closest('.variant-type');
    const typeId = parseInt(typeContainer.dataset.typeId);

    const variantTypeObj = variantTypes.find(type => type.id === typeId);
    if (variantTypeObj) {
        const valueIndex = Array.from(valueContainer.parentElement.children).indexOf(valueContainer);
        variantTypeObj.values.splice(valueIndex, 1); // Xóa giá trị khỏi mảng
    }

    valueContainer.remove();
    updateVariantTable();
}


// Cập nhật tên và giá trị của các biến thể
document.addEventListener('input', function (e) {
    if (e.target.classList.contains('variant-type-name')) {
        const typeContainer = e.target.closest('.variant-type');
        const typeId = parseInt(typeContainer.dataset.typeId);
        const variantType = variantTypes.find(type => type.id === typeId);
        if (variantType) {
            variantType.name = e.target.value; // Cập nhật tên biến thể
        }
    } else if (e.target.classList.contains('variant-value')) {
        const typeContainer = e.target.closest('.variant-type');
        const typeId = parseInt(typeContainer.dataset.typeId);
        const valueContainer = e.target.closest('.d-flex');
        const valueIndex = Array.from(valueContainer.parentElement.children).indexOf(valueContainer);

        const variantType = variantTypes.find(type => type.id === typeId);
        if (variantType) {
            variantType.values[valueIndex] = e.target.value; // Cập nhật giá trị biến thể
        }
    }

    updateVariantTable();
});

// Hàm tạo key ổn định cho mỗi combination (dựa vào giá trị, không dựa vào tên loại)
function getComboKey(values) {
    return values.join('|');
}

// Generate all possible combinations of variant values, trả về cả mảng object {values, key}
function generateVariantCombinations() {
    const validTypes = variantTypes.filter(type => type.name && type.values.some(v => v));
    if (validTypes.length === 0) return [];

    function combine(arrays) {
        return arrays.reduce((a, b) =>
            a.flatMap(x => b.map(y => [...x, y])), [[]]);
    }
    const combos = combine(validTypes.map(type => type.values.filter(v => v)));
    // Tạo key duy nhất cho mỗi combination (dựa vào giá trị)
    return combos.map(combo => {
        const key = getComboKey(combo);
        return { values: combo, key };
    });
}

// Cập nhật bảng biến thể
function updateVariantTable() {
    const tableContainer = document.querySelector('.variant-table');
    const tableBody = document.getElementById('variantTableBody');
    const thead = document.querySelector('.variant-table thead tr');

    const validTypes = variantTypes.filter(type => type.name && type.values.some(v => v));

    if (validTypes.length === 0) {
        tableContainer.style.display = 'none';
        return;
    }

    // Cập nhật tiêu đề bảng
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

    // Tạo các kết hợp và cập nhật phần thân bảng
    const combinations = generateVariantCombinations();
    // Map lại ảnh cũ cho các combination mới nếu có phần giống nhau
    const newVariantImages = {};
    combinations.forEach(({ key }) => {
        if (variantImages[key]) {
            newVariantImages[key] = variantImages[key];
        } else {
            // Tìm key cũ gần giống nhất (cùng đầu chuỗi)
            const oldKey = Object.keys(variantImages).find(k => key.startsWith(k));
            if (oldKey) newVariantImages[key] = variantImages[oldKey];
        }
    });
    variantImages = newVariantImages;

    tableBody.innerHTML = combinations.map(({ values, key }) => {
        let imgSrc = variantImages[key] || '';
        return `
            <tr class="align-middle">
                ${values.map(value => `<td class='align-middle' style='min-width: 100px; text-align: center;'>${value}</td>`).join('')}
                <td class='align-middle' style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Giá bán"></td>
                <td class='align-middle' style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Giá vốn"></td>
                <td class='align-middle' style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Số lượng"></td>
                <td class='align-middle' style='min-width: 120px;'><input type="number" class="form-control form-control-sm" placeholder="Khối lượng"></td>
                <td class='align-middle' style='min-width: 180px;'>
                    <div class="d-flex gap-1 align-items-center">
                        <input type="number" class="form-control form-control-sm" placeholder="D">
                        <input type="number" class="form-control form-control-sm" placeholder="R">
                        <input type="number" class="form-control form-control-sm" placeholder="C">
                    </div>
                </td>
                <td class='align-middle' style='min-width: 120px; text-align: center;'>
                    ${!imgSrc
                ? `<i class=\"mdi mdi-image-area\" style=\"font-size: 24px; color: #bbb; cursor: pointer;\" onclick=\"triggerImageUpload(event, '${key}')\"></i>`
                : `<div id=\"imgPreview-${key}\" style=\"margin-top: 8px; display: block; position: relative; width: max-content; margin-left: auto; margin-right: auto;\">\n                            <img src=\"${imgSrc}\" style=\"width: 100px; height: 100px; object-fit: cover; border-radius: 8px; box-shadow: 0 2px 8px #0001;\" />\n                            <i class=\"mdi mdi-pencil\" title=\"Đổi ảnh\" onclick=\"triggerImageUpload(event, '${key}')\" style=\"position: absolute; top: 2px; right: 2px; background: #fff; border-radius: 50%; padding: 2px; font-size: 14px; color: #dc3545; border: 1px solid #e0e0e0; box-shadow: 0 1px 4px #0001; cursor: pointer; z-index: 2; opacity: 0.92;\"></i>\n                        </div>`}
                </td>
            </tr>
        `;
    }).join('');

    tableContainer.style.display = 'block';
}

// Hàm thêm ảnh vào biến thể (theo key combination)
function triggerImageUpload(event, comboKey) {
    const inputFile = document.createElement('input');
    inputFile.type = 'file';
    inputFile.accept = 'image/*';

    inputFile.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                const imgSrc = e.target.result;
                // Lưu lại ảnh cho combination
                variantImages[comboKey] = imgSrc;
                // Cập nhật ảnh cho biến thể
                const previewDiv = document.getElementById(`imgPreview-${comboKey}`);
                if (previewDiv) {
                    const imgElement = previewDiv.querySelector('img');
                    imgElement.src = imgSrc;
                    previewDiv.style.display = 'block';
                }
                updateVariantTable(); // Cập nhật lại bảng để hiển thị đúng trạng thái
            };
            reader.readAsDataURL(file);
        }
    });

    inputFile.click();
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
            previewDiv.classList.remove('d-none'); // Hiển thị ảnh
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

    // Xóa hình ảnh và reset lại UI
    input.value = ''; // Reset file input
    preview.src = '#'; // Reset image preview
    previewDiv.classList.add('d-none'); // Ẩn ảnh
    previewDiv.classList.remove('d-block');
    uploadContent.classList.remove('d-none'); // Hiển thị lại icon upload
    dropZone.classList.remove('bg-white');
    dropZone.classList.add('bg-light');
}

// Kiểm tra và hiển thị lại các section không phải biến thể nếu cần
function checkAndShowNoVariantSections() {
    if (variantTypes.length === 0) {
        if (typeof showNoVariantSections === 'function') {
            showNoVariantSections();
        }
    }
}


