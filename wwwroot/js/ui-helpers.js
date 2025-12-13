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

// Alert/Notification functions
function showAlert(message, type = 'info', duration = 5000) {
    const alertContainer = document.getElementById('alert-container');
    if (!alertContainer) {
        console.error('Alert container not found. Please add `<div id="alert-container"></div>` to your layout.');
        return;
    }

    const alertId = `alert-${Date.now()}`;
    const alertClasses = {
        'info': 'alert-info',
        'success': 'alert-success',
        'warning': 'alert-warning',
        'error': 'alert-danger'
    };

    const alertHtml = `
        <div id="${alertId}" class="alert ${alertClasses[type] || 'alert-info'} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    alertContainer.insertAdjacentHTML('beforeend', alertHtml);

    const alertElement = document.getElementById(alertId);
    if (duration) {
        setTimeout(() => {
            const alert = bootstrap.Alert.getOrCreateInstance(alertElement);
            if(alert) {
                alert.close();
            }
        }, duration);
    }
}
