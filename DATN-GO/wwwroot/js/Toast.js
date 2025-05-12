document.addEventListener("DOMContentLoaded", function () {
	const toastEl = document.getElementById('autoToast');
	if (toastEl) {
		const toast = new bootstrap.Toast(toastEl, {
			delay: 3000,
			autohide: true
		});
		toast.show();
	}
});