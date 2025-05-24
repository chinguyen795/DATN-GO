    document.addEventListener('DOMContentLoaded', function () {
        const imageInput = document.getElementById('imageInput');
    const previewImage = document.getElementById('previewImage');
    const previewContainer = document.getElementById('previewContainer');
    const fileNameDisplay = document.getElementById('fileNameDisplay');

    imageInput.addEventListener('change', function (event) {
            const file = event.target.files[0];

    if (file) {
                const reader = new FileReader();

    reader.onload = function (e) {
        previewImage.src = e.target.result;
    previewContainer.style.display = 'block';
                };

    reader.readAsDataURL(file);
    fileNameDisplay.textContent = file.name;
            } else {
        previewImage.src = "#";
    previewContainer.style.display = 'none';
    fileNameDisplay.textContent = '';
            }
        });
    });

    function setDeleteUrl(postId) {
        var form = document.getElementById('deleteForm');
    form.action = '/Seller/PostArticle/Delete/' + postId;
    }
