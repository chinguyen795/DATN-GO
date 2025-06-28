class VoucherManager {
    constructor() {
        this.vouchers = JSON.parse(localStorage.getItem('vouchers')) || [];
        this.currentEditId = null;
        this.currentDeleteId = null;

        this.initializeEventListeners();
        this.renderVouchers();
        this.updateStatistics();

        // Set default dates
        const today = new Date().toISOString().split('T')[0];
        document.getElementById('startDate').value = today;

        const nextMonth = new Date();
        nextMonth.setMonth(nextMonth.getMonth() + 1);
        document.getElementById('endDate').value = nextMonth.toISOString().split('T')[0];
    }

    initializeEventListeners() {
        // Discount type change
        document.getElementById('discountType').addEventListener('change', (e) => {
            const unit = document.getElementById('discountUnit');
            const valueInput = document.getElementById('discountValue');

            if (e.target.value === 'percentage') {
                unit.textContent = '%';
                valueInput.max = '100';
                valueInput.placeholder = 'Ví dụ: 20';
            } else if (e.target.value === 'fixed') {
                unit.textContent = 'VNĐ';
                valueInput.removeAttribute('max');
                valueInput.placeholder = 'Ví dụ: 50000';
            }
        });

        // Save voucher
        document.getElementById('saveVoucherBtn').addEventListener('click', () => {
            this.saveVoucher();
        });

        // Search
        document.getElementById('searchInput').addEventListener('input', (e) => {
            this.searchVouchers(e.target.value);
        });

        // Status filter
        document.getElementById('statusFilter').addEventListener('change', (e) => {
            this.filterByStatus(e.target.value);
        });

        // Sort
        document.getElementById('sortSelect').addEventListener('change', (e) => {
            this.sortVouchers(e.target.value);
        });

        // Delete confirmation
        document.getElementById('confirmDeleteBtn').addEventListener('click', () => {
            this.deleteVoucher();
        });

        // Modal reset
        $('#addVoucherModal').on('hidden.bs.modal', () => {
            this.resetForm();
        });
    }

    generateId() {
        return Date.now().toString(36) + Math.random().toString(36).substr(2);
    }

    saveVoucher() {
        const form = document.getElementById('voucherForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const voucherData = {
            id: this.currentEditId || this.generateId(),
            name: document.getElementById('voucherName').value,
            code: document.getElementById('voucherCode').value.toUpperCase(),
            discountType: document.getElementById('discountType').value,
            discountValue: parseFloat(document.getElementById('discountValue').value),
            minOrder: parseFloat(document.getElementById('minOrder').value) || 0,
            quantity: parseInt(document.getElementById('quantity').value),
            startDate: document.getElementById('startDate').value,
            endDate: document.getElementById('endDate').value,
            status: document.getElementById('voucherStatus').value,
            description: document.getElementById('description').value,
            createdAt: this.currentEditId ? this.vouchers.find(v => v.id === this.currentEditId).createdAt : new Date().toISOString(),
            updatedAt: new Date().toISOString()
        };

        // Validate dates
        if (new Date(voucherData.startDate) >= new Date(voucherData.endDate)) {
            this.showToast('Ngày kết thúc phải sau ngày bắt đầu!', 'error');
            return;
        }

        // Check duplicate code
        const existingVoucher = this.vouchers.find(v => v.code === voucherData.code && v.id !== voucherData.id);
        if (existingVoucher) {
            this.showToast('Mã voucher đã tồn tại!', 'error');
            return;
        }

        if (this.currentEditId) {
            // Update existing voucher
            const index = this.vouchers.findIndex(v => v.id === this.currentEditId);
            this.vouchers[index] = voucherData;
        } else {
            // Add new voucher
            this.vouchers.push(voucherData);
        }

        this.saveToStorage();
        this.renderVouchers();
        this.updateStatistics();

        // Close modal
        $('#addVoucherModal').modal('hide');

        // Show success message
        this.showToast(this.currentEditId ? 'Cập nhật voucher thành công!' : 'Thêm voucher thành công!', 'success');
    }

    editVoucher(id) {
        const voucher = this.vouchers.find(v => v.id === id);
        if (!voucher) return;

        this.currentEditId = id;

        // Fill form
        document.getElementById('voucherId').value = voucher.id;
        document.getElementById('voucherName').value = voucher.name;
        document.getElementById('voucherCode').value = voucher.code;
        document.getElementById('discountType').value = voucher.discountType;
        document.getElementById('discountValue').value = voucher.discountValue;
        document.getElementById('minOrder').value = voucher.minOrder;
        document.getElementById('quantity').value = voucher.quantity;
        document.getElementById('startDate').value = voucher.startDate;
        document.getElementById('endDate').value = voucher.endDate;
        document.getElementById('voucherStatus').value = voucher.status || 'active';
        document.getElementById('description').value = voucher.description || '';

        // Update discount unit
        const unit = document.getElementById('discountUnit');
        unit.textContent = voucher.discountType === 'percentage' ? '%' : 'VNĐ';

        // Update modal title
        document.getElementById('modalTitle').textContent = 'Chỉnh Sửa Voucher';

        // Show modal
        $('#addVoucherModal').modal('show');
    }

    confirmDelete(id) {
        this.currentDeleteId = id;
        $('#deleteModal').modal('show');
    }

    deleteVoucher() {
        if (!this.currentDeleteId) return;

        this.vouchers = this.vouchers.filter(v => v.id !== this.currentDeleteId);
        this.saveToStorage();
        this.renderVouchers();
        this.updateStatistics();

        // Close modal
        $('#deleteModal').modal('hide');

        this.showToast('Xóa voucher thành công!', 'success');
        this.currentDeleteId = null;
    }

    resetForm() {
        document.getElementById('voucherForm').reset();
        document.getElementById('modalTitle').textContent = 'Thêm Voucher Mới';
        document.getElementById('discountUnit').textContent = '%';
        document.getElementById('voucherStatus').value = 'active';
        this.currentEditId = null;

        // Reset default dates
        const today = new Date().toISOString().split('T')[0];
        document.getElementById('startDate').value = today;

        const nextMonth = new Date();
        nextMonth.setMonth(nextMonth.getMonth() + 1);
        document.getElementById('endDate').value = nextMonth.toISOString().split('T')[0];
    }

    getVoucherStatus(voucher) {
        // Check manual status first
        if (voucher.status === 'inactive') return 'inactive';

        const now = new Date();
        const startDate = new Date(voucher.startDate);
        const endDate = new Date(voucher.endDate);

        if (now < startDate) return 'upcoming';
        if (now > endDate) return 'expired';
        if (voucher.quantity <= 0) return 'exhausted';

        // Check if expiring soon (within 7 days)
        const daysUntilExpiry = Math.ceil((endDate - now) / (1000 * 60 * 60 * 24));
        if (daysUntilExpiry <= 7) return 'expiring';

        return 'active';
    }

    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    formatDate(dateString) {
        return new Date(dateString).toLocaleDateString('vi-VN');
    }

    renderVouchers(vouchersToRender = this.vouchers) {
        const container = document.getElementById('voucherList');
        const emptyState = document.getElementById('emptyState');

        if (vouchersToRender.length === 0) {
            container.innerHTML = '';
            emptyState.style.display = 'block';
            return;
        }

        emptyState.style.display = 'none';

        container.innerHTML = vouchersToRender.map(voucher => {
            const status = this.getVoucherStatus(voucher);
            const statusConfig = {
                active: { class: 'bg-success', text: 'Đang hoạt động', icon: 'bi-check-circle' },
                inactive: { class: 'bg-secondary', text: 'Tạm dừng', icon: 'bi-pause-circle' },
                upcoming: { class: 'bg-info', text: 'Sắp diễn ra', icon: 'bi-clock' },
                expired: { class: 'bg-danger', text: 'Đã hết hạn', icon: 'bi-x-circle' },
                exhausted: { class: 'bg-warning', text: 'Đã hết', icon: 'bi-exclamation-triangle' },
                expiring: { class: 'bg-warning', text: 'Sắp hết hạn', icon: 'bi-exclamation-triangle' }
            };

            const statusInfo = statusConfig[status];
            const discountText = voucher.discountType === 'percentage'
                ? `${voucher.discountValue}%`
                : this.formatCurrency(voucher.discountValue);

            return `
                            <div class="col-12 col-md-6">
                                <div class="card shadow-sm hover-shadow-lg transition-all">
                                    <div class="card-body p-0">
                                        <div class="d-flex">
                                            <div class="voucher-icon">
                                                <i class="bi bi-ticket-perforated-fill"></i>
                                            </div>
                                            <div class="p-3 flex-grow-1">
                                                <div class="d-flex justify-content-between align-items-start mb-2">
                                                    <div>
                                                        <h5 class="card-title mb-1">${voucher.name}</h5>
                                                        <p class="card-text text-muted mb-0">Mã: ${voucher.code}</p>
                                                    </div>
                                                    <div class="voucher-action d-flex gap-2">
                                                        <button class="btn btn-outline-warning btn-sm rounded-circle"
                                                                onclick="voucherManager.editVoucher('${voucher.id}')"
                                                                title="Chỉnh sửa">
                                                            <i class="bi-pencil"></i>
                                                        </button>
                                                        <button class="btn btn-outline-danger btn-sm rounded-circle"
                                                                onclick="voucherManager.confirmDelete('${voucher.id}')"
                                                                title="Xóa">
                                                            <i class="bi bi-trash"></i>
                                                        </button>
                                                    </div>
                                                </div>

                                                <div class="mb-2">
                                                    <span class="badge ${voucher.discountType === 'percentage' ? 'badge-percentage' : 'badge-fixed'} text-white">
                                                        <i class="bi ${voucher.discountType === 'percentage' ? 'bi-percent' : 'bi-currency-dollar'} me-1"></i>
                                                        Giảm ${discountText}
                                                    </span>
                                                </div>

                                                ${voucher.minOrder > 0 ? `
                                                    <p class="text-muted small mb-2">Đơn tối thiểu: ${this.formatCurrency(voucher.minOrder)}</p>
                                                ` : ''}

                                                <div class="d-flex flex-column gap-1">
                                                    <small class="text-success">
                                                        <i class="bi bi-calendar2-check me-1"></i>
                                                        Bắt đầu: ${this.formatDate(voucher.startDate)}
                                                    </small>
                                                    <small class="text-danger">
                                                        <i class="bi bi-calendar2-x me-1"></i>
                                                        Hết hạn: ${this.formatDate(voucher.endDate)}
                                                    </small>
                                                </div>
                                                <div class="mt-2 d-flex justify-content-between align-items-center">
                                                    <span class="badge ${statusInfo.class} text-white">
                                                        <i class="${statusInfo.icon} me-1"></i>
                                                        ${statusInfo.text}
                                                    </span>
                                                    <span class="badge bg-info text-white">
                                                        <i class="bi bi-box-seam me-1"></i>
                                                        Còn lại: ${voucher.quantity}
                                                    </span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        `;
        }).join('');
    }

    searchVouchers(query) {
        if (!query.trim()) {
            this.renderVouchers();
            return;
        }

        const filtered = this.vouchers.filter(voucher =>
            voucher.name.toLowerCase().includes(query.toLowerCase()) ||
            voucher.code.toLowerCase().includes(query.toLowerCase()) ||
            (voucher.description && voucher.description.toLowerCase().includes(query.toLowerCase()))
        );

        this.renderVouchers(filtered);
    }

    filterByStatus(status) {
        if (status === 'all') {
            this.renderVouchers();
            return;
        }

        const filtered = this.vouchers.filter(voucher => {
            const voucherStatus = this.getVoucherStatus(voucher);
            return voucherStatus === status;
        });

        this.renderVouchers(filtered);
    }

    sortVouchers(sortBy) {
        let sorted = [...this.vouchers];

        switch (sortBy) {
            case 'newest':
                sorted.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
                break;
            case 'oldest':
                sorted.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
                break;
            case 'value-desc':
                sorted.sort((a, b) => b.discountValue - a.discountValue);
                break;
            case 'value-asc':
                sorted.sort((a, b) => a.discountValue - b.discountValue);
                break;
        }

        this.renderVouchers(sorted);
    }

    updateStatistics() {
        const now = new Date();

        const stats = this.vouchers.reduce((acc, voucher) => {
            const status = this.getVoucherStatus(voucher);

            acc.total++;

            switch (status) {
                case 'active':
                    acc.active++;
                    break;
                case 'expiring':
                    acc.expiring++;
                    break;
                case 'expired':
                case 'exhausted':
                case 'inactive':
                    acc.expired++;
                    break;
            }

            return acc;
        }, { total: 0, active: 0, expiring: 0, expired: 0 });

        document.getElementById('totalVouchers').textContent = stats.total;
        document.getElementById('activeVouchers').textContent = stats.active;
        document.getElementById('expiringVouchers').textContent = stats.expiring;
        document.getElementById('expiredVouchers').textContent = stats.expired;
    }

    saveToStorage() {
        localStorage.setItem('vouchers', JSON.stringify(this.vouchers));
    }

    showToast(message, type = 'info') {
        // Simple toast implementation
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.innerHTML = `
                        <i class="bi ${type === 'success' ? 'bi-check-circle' : 'bi-exclamation-circle'} me-2"></i>
                        ${message}
                        <button type="button" class="close" onclick="this.parentElement.remove()"></button>
                    `;

        document.body.appendChild(toast);

        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 5000);
    }
}

// Initialize the voucher manager
let voucherManager;
document.addEventListener('DOMContentLoaded', () => {
    voucherManager = new VoucherManager();
});