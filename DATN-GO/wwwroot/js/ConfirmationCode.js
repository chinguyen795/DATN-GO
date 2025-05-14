document.addEventListener('DOMContentLoaded', function () {
    let timeLeft = 60;
    let countdownInterval;

    const inputs = document.querySelectorAll('.otp-input');
    const resendBtn = document.getElementById('resendBtn');
    const countdownSpan = document.getElementById('countdown');

    // OTP input handling
    inputs.forEach((input, index) => {
        input.addEventListener('keyup', (e) => {
            if (e.key >= 0 && e.key <= 9) {
                if (index < inputs.length - 1) {
                    inputs[index + 1].focus();
                }
            } else if (e.key === 'Backspace') {
                if (index > 0) {
                    inputs[index - 1].focus();
                }
            }
        });
    });

    function updateCountdown() {
        if (timeLeft > 0) {
            timeLeft--;
            countdownSpan.textContent = timeLeft;
        } else {
            clearInterval(countdownInterval);
            resendBtn.disabled = false;
            countdownSpan.textContent = '0';
        }
    }

    // Resend button handling
    resendBtn.addEventListener('click', () => {
        timeLeft = 60;
        resendBtn.disabled = true;
        countdownInterval = setInterval(updateCountdown, 1000);
    });

    // Initial countdown start
    countdownInterval = setInterval(updateCountdown, 1000);
});
