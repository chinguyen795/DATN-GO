document.addEventListener('DOMContentLoaded', function() {
    // Initialize address selects
    handleProvinceSelect('province', 'district', 'commune');
});

function previewImage(input, previewId) {
    const preview = document.getElementById(previewId);
    const previewImg = preview.querySelector('.preview-image');
    const uploadPrompt = preview.querySelector('.upload-prompt');
    
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            previewImg.src = e.target.result;
            previewImg.classList.remove('d-none');
            if (uploadPrompt) {
                uploadPrompt.classList.add('d-none');
            }
        }
        
        reader.readAsDataURL(input.files[0]);
    }
}