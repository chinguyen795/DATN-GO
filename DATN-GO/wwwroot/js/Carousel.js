const slideContainer = document.querySelector(".slide-show-items");
let slides = document.querySelectorAll(".slide-show-items img");
let slideWidth = slides[0].clientWidth + 30 + 20; // Cộng thêm margin + gap

function runSlideshow() {
    slideContainer.style.transition = "transform 0.5s ease-in-out";
    slideContainer.style.transform = `translateX(-${slideWidth}px)`;

    setTimeout(() => {
        slideContainer.style.transition = "none";
        let firstSlide = slideContainer.firstElementChild;
        slideContainer.appendChild(firstSlide);
        slideContainer.style.transform = "translateX(0)";
        updateMainItem(); // Cập nhật ảnh chính giữa
    }, 500);
}

function updateMainItem() {
    let slides = document.querySelectorAll(".slide-show-items img");

    slides.forEach(img => img.classList.remove("item-main"));

    let middleIndex = Math.floor(slides.length / 2);
    if (slides[middleIndex]) {
        slides[middleIndex].classList.add("item-main");
    }
}

updateMainItem();

setInterval(runSlideshow, 2000); // slideshow
