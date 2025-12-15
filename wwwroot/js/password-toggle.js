/**
 * Password Toggle - Show/Hide password functionality
 * Automatically enhances all password fields with a toggle button
 */
(function () {
    'use strict';

    function initPasswordToggles() {
        // Find all password inputs that haven't been enhanced yet
        const passwordInputs = document.querySelectorAll('input[type="password"]:not([data-password-toggle])');

        passwordInputs.forEach(function (input) {
            // Mark as enhanced to avoid duplicate processing
            input.setAttribute('data-password-toggle', 'true');

            // Create wrapper if not already wrapped
            let wrapper = input.parentElement;
            if (!wrapper.classList.contains('password-wrapper')) {
                // Check if parent is form-floating
                if (wrapper.classList.contains('form-floating')) {
                    // For form-floating, add wrapper class to parent
                    wrapper.classList.add('password-wrapper');
                } else {
                    // Create new wrapper
                    wrapper = document.createElement('div');
                    wrapper.className = 'password-wrapper';
                    input.parentNode.insertBefore(wrapper, input);
                    wrapper.appendChild(input);
                }
            }

            // Create toggle button
            const toggleBtn = document.createElement('button');
            toggleBtn.type = 'button';
            toggleBtn.className = 'password-toggle';
            toggleBtn.setAttribute('aria-label', 'Toggle password visibility');
            toggleBtn.setAttribute('tabindex', '-1');
            toggleBtn.innerHTML = '<i class="bi bi-eye"></i>';

            // Insert toggle button after input
            input.insertAdjacentElement('afterend', toggleBtn);

            // Add click handler
            toggleBtn.addEventListener('click', function (e) {
                e.preventDefault();
                const icon = this.querySelector('i');

                if (input.type === 'password') {
                    input.type = 'text';
                    icon.classList.remove('bi-eye');
                    icon.classList.add('bi-eye-slash');
                    this.setAttribute('aria-label', 'Hide password');
                } else {
                    input.type = 'password';
                    icon.classList.remove('bi-eye-slash');
                    icon.classList.add('bi-eye');
                    this.setAttribute('aria-label', 'Show password');
                }

                // Keep focus on the input
                input.focus();
            });

            // Prevent form submission when pressing Enter on toggle
            toggleBtn.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                }
            });
        });
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPasswordToggles);
    } else {
        initPasswordToggles();
    }

    // Re-initialize when new content is loaded (for dynamic forms)
    // Expose function globally for manual calls if needed
    window.initPasswordToggles = initPasswordToggles;
})();
