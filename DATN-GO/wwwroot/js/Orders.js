document.addEventListener('DOMContentLoaded', function () {
    // Handle the reject order confirmation
    document.getElementById('confirmReject').addEventListener('click', function () {
        const reason = document.getElementById('rejectReason').value;
        if (!reason.trim()) {
            alert('Vui lòng nhập lý do từ chối đơn hàng');
            return;
        }

        // Here you would typically send this to your backend
        alert('Đã từ chối đơn hàng với lý do: ' + reason);

        // Close the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('rejectOrderModal'));
        modal.hide();

        // Clear the form
        document.getElementById('rejectReason').value = '';
    });
    // Make all order rows clickable to show modal
    document.querySelectorAll('tr[data-bs-toggle="modal"]').forEach(row => {
        row.style.cursor = 'pointer';
        row.addEventListener('click', function () {
            const orderId = this.querySelector('td:first-child').textContent;
            const customer = this.querySelector('td:nth-child(2)').textContent;
            const quantity = this.querySelector('td:nth-child(3)').textContent;
            const amount = this.querySelector('td:nth-child(4)').textContent;
            const date = this.querySelector('td:nth-child(5)').textContent;
            const status = this.querySelector('td:last-child').textContent.trim();

            // Update modal with order details
            document.getElementById('modalOrderId').textContent = orderId;
            document.getElementById('modalCustomerName').textContent = customer;
            document.getElementById('modalQuantity').textContent = quantity;
            document.getElementById('modalTotalAmount').textContent = amount;
            document.getElementById('modalOrderDate').textContent = date;
            document.getElementById('modalStatus').textContent = status;

            // Sample order items - this would normally come from your backend
            const sampleItems = [
                { name: 'Sản phẩm mẫu 1', quantity: '1', price: '500.000đ', total: '500.000đ' },
                { name: 'Sản phẩm mẫu 2', quantity: '2', price: '275.000đ', total: '550.000đ' }
            ];

            // Populate order items table
            const modalOrderItems = document.getElementById('modalOrderItems');
            modalOrderItems.innerHTML = sampleItems.map(item => `
                <tr>
                    <td>${item.name}</td>
                    <td>${item.quantity}</td>
                    <td>${item.price}</td>
                    <td>${item.total}</td>
                </tr>
            `).join('');
        });
    });

    // Handle status update button
    document.getElementById('btnUpdateStatus').addEventListener('click', function () {
        // Add your status update logic here
        alert('Cập nhật trạng thái thành công!');
        const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailsModal'));
        modal.hide();
    });
});
