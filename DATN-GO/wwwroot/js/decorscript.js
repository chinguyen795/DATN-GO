 
    document.addEventListener("DOMContentLoaded", function () {
        // Cập nhật năm ở footer
        if (document.getElementById("copy-year")) {
        document.getElementById("copy-year").innerHTML =
        new Date().getFullYear();
        }

    const uploadModalEl = document.getElementById("uploadModal");
    if (!uploadModalEl) return;

    const uploadModal = new bootstrap.Modal(uploadModalEl);
    const modalForm = document.getElementById("modalUploadForm");
    const modalFile = document.getElementById("modalFile");
    const modalPreview = document.getElementById("modalPreview");
    const modalTopicGroup = document.getElementById("modalTopicGroup");
    const modalDescriptionGroup = document.getElementById(
    "modalDescriptionGroup"
    );

        document.querySelectorAll("button[data-type]").forEach((button) => {
        button.addEventListener("click", () => {
            const type = button.getAttribute("data-type");
            const target = button.getAttribute("data-target");
            modalForm.reset();
            modalPreview.innerHTML = "";
            document.getElementById("uploadType").value = type;
            document.getElementById("uploadTarget").value = target || "";
            if (type === "slideshow" || type === "video") {
                modalTopicGroup.style.display = "none";
                modalDescriptionGroup.style.display = "none";
                modalFile.accept = type === "slideshow" ? "image/*" : "video/*";
                modalFile.multiple = type === "slideshow";
            } else {
                modalTopicGroup.style.display = "block";
                modalDescriptionGroup.style.display = "block";
                modalFile.accept = "image/*";
                modalFile.multiple = false;
            }
            uploadModal.show();
        });
        });

    modalFile.addEventListener("change", function (event) {
        modalPreview.innerHTML = "";
    const files = event.target.files;
          if (files.length > 0) {
        Array.from(files).forEach((file) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                let el;
                if (file.type.startsWith("image/")) {
                    el = document.createElement("img");
                    el.src = e.target.result;
                    el.className = "img-fluid rounded";
                    el.style.cssText = "max-height: 200px; margin: 5px;";
                } else if (file.type.startsWith("video/")) {
                    el = document.createElement("video");
                    el.src = e.target.result;
                    el.controls = true;
                    el.className = "img-fluid rounded";
                    el.style.maxHeight = "200px";
                }
                if (el) modalPreview.appendChild(el);
            };
            reader.readAsDataURL(file);
        });
          }
        });

    document
    .getElementById("modalSaveBtn")
    .addEventListener("click", function () {
            const type = document.getElementById("uploadType").value;
    const targetId = document.getElementById("uploadTarget").value;
    const files = modalFile.files;
    if (files.length === 0) return;

    if (type === "slideshow") {
              const container = document.getElementById("slideShowPreview");
    container.innerHTML = "";
    const carouselId = "carousel" + Date.now();
    const carouselHtml = `
    <div id="${carouselId}" class="carousel slide h-100" data-bs-ride="carousel">
        <div class="carousel-inner h-100">
            ${Array.from(files)
                .map(
                    (file, index) => `
                                <div class="carousel-item h-100 ${index === 0 ? "active" : ""
                        }">
                                    <img src="${URL.createObjectURL(
                            file
                        )}" class="d-block w-100 h-100" style="object-fit: cover;">
                                </div>`
                )
                .join("")}
        </div>
        <button class="carousel-control-prev" type="button" data-bs-target="#${carouselId}" data-bs-slide="prev"><span class="carousel-control-prev-icon" aria-hidden="true"></span></button>
        <button class="carousel-control-next" type="button" data-bs-target="#${carouselId}" data-bs-slide="next"><span class="carousel-control-next-icon" aria-hidden="true"></span></button>
    </div>`;
    container.innerHTML = carouselHtml;
            } else if (type === "video") {
              const container = document.getElementById("videoPreview");
    container.innerHTML = `<video src="${URL.createObjectURL(
                files[0]
              )}" controls class="w-100 h-100" style="object-fit: cover;"></video>`;
            } else if (type === "decorate") {
              const container = document.getElementById(
    `decoratePreview${targetId}`
    );
    container.innerHTML = `<img src="${URL.createObjectURL(
                files[0]
              )}" class="w-100 h-100" style="object-fit: cover;">`;
            }
        uploadModal.hide();
          });
      });
 