// Spinner functions
function showSpinner(container, message = 'Loading...') {
    const containerEl = typeof container === 'string' ? document.getElementById(container) : container;

    if (containerEl) {
        // Add a position style if the container is static
        const position = window.getComputedStyle(containerEl).position;
        if (position === 'static') {
            containerEl.style.position = 'relative';
        }
        
        const spinnerHTML = `
            <div class="loading-overlay">
                <div class="loading-spinner">
                    <div class="spinner"></div>
                    ${message ? `<div class="spinner-message">${message}</div>` : ''}
                </div>
            </div>
        `;

        containerEl.insertAdjacentHTML('beforeend', spinnerHTML);
    }
}

function hideSpinner(container) {
    const containerEl = typeof container === 'string' ? document.getElementById(container) : container;
    if (containerEl) {
        const spinner = containerEl.querySelector('.loading-overlay');
        if (spinner) {
            spinner.remove();
        }
    }
}

// Alert/Toast Notification functions
function showAlert(message, type = 'info', duration = 5000) {
    const alertContainer = document.getElementById('alert-container');
    if (!alertContainer) {
        console.error('Alert container not found. Please add `<div id="alert-container"></div>` to your layout.');
        return;
    }

    // Normalize type (support Bootstrap's 'danger' as alias for 'error')
    const normalizedType = type === 'danger' ? 'error' : type;

    const alertId = `alert-${Date.now()}`;
    const alertConfig = {
        'info': { class: 'alert-info', icon: 'bi-info-circle' },
        'success': { class: 'alert-success', icon: 'bi-check-circle' },
        'warning': { class: 'alert-warning', icon: 'bi-exclamation-circle' },
        'error': { class: 'alert-danger', icon: 'bi-exclamation-triangle' }
    };

    const config = alertConfig[normalizedType] || alertConfig['info'];

    const alertHtml = `
        <div id="${alertId}" class="alert ${config.class} alert-dismissible fade show d-flex align-items-center" role="alert">
            <i class="bi ${config.icon} me-2 flex-shrink-0"></i>
            <div class="flex-grow-1">${message}</div>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    alertContainer.insertAdjacentHTML('beforeend', alertHtml);

    const alertElement = document.getElementById(alertId);
    if (duration) {
        setTimeout(() => {
            if (alertElement && alertElement.parentNode) {
                const alert = bootstrap.Alert.getOrCreateInstance(alertElement);
                if (alert) {
                    alert.close();
                }
            }
        }, duration);
    }
}
