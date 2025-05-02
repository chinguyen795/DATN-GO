document.addEventListener('DOMContentLoaded', function() {
    // Success toast handler
    document.getElementById('showLiveToastBtn').addEventListener('click', function () {
        const liveToast = new bootstrap.Toast(document.getElementById('liveToast'));
        liveToast.show();
    });

    // Error toast handler  
    document.getElementById('showErrorToastBtn').addEventListener('click', function () {
        const errorToast = new bootstrap.Toast(document.getElementById('errorToast'));
        errorToast.show();
    });
});