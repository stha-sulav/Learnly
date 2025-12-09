// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    // 1. Auto-dismiss alerts
    // Find all alerts with the 'autodismiss' class and dismiss them after 5 seconds
    document.querySelectorAll('.alert.autodismiss').forEach(function (alertElement) {
        setTimeout(function () {
            // Use Bootstrap's native dismiss functionality if available
            var alert = new bootstrap.Alert(alertElement);
            alert.close();
        }, 3750); // 3.75 seconds
    });

    // 2. Tooltip and popover initialization
    // Initialize all Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize all Bootstrap popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // 3. Loading indicators (Example functions)
    function showLoadingIndicator() {
        // Implement logic to show a loading spinner or overlay
        // For example, display a spinner in the navbar or a full-screen overlay
        console.log('Showing loading indicator...');
        // document.getElementById('loadingSpinner').style.display = 'block';
    }

    function hideLoadingIndicator() {
        // Implement logic to hide the loading spinner or overlay
        console.log('Hiding loading indicator...');
        // document.getElementById('loadingSpinner').style.display = 'none';
    }

    // 4. AJAX helper functions
    async function ajaxRequest(url, method = 'GET', data = null) {
        showLoadingIndicator();
        try {
            const options = {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken() // Include CSRF token if needed
                }
            };

            if (data) {
                options.body = JSON.stringify(data);
            }

            const response = await fetch(url, options);

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Something went wrong');
            }

            return await response.json();
        } catch (error) {
            console.error('AJAX Error:', error);
            // Optionally, display an error message to the user
            throw error; // Re-throw to allow further handling
        } finally {
            hideLoadingIndicator();
        }
    }

    // Helper to get CSRF token from a meta tag (assuming it's present in _Layout.cshtml)
    function getCsrfToken() {
        const tokenElement = document.querySelector('meta[name="RequestVerificationToken"]');
        return tokenElement ? tokenElement.getAttribute('content') : '';
    }

    // 5. Notification functionality (Placeholder)
    const notificationBell = document.getElementById('notificationBell');
    if (notificationBell) {
        notificationBell.addEventListener('click', async function (event) {
            event.preventDefault();
            // In a real app, this would fetch notifications from an API
            console.log('Fetching notifications...');

            // Placeholder for fetching notifications
            // const notifications = await ajaxRequest('/api/notifications');
            // updateNotificationsDropdown(notifications);

            // Simulate fetching with static data
            const staticNotifications = [
                { id: 1, message: 'New course "Introduction to AI" is available!', url: '#' },
                { id: 2, message: 'Quiz result for "Module 1" is ready.', url: '#' }
            ];
            updateNotificationsDropdown(staticNotifications);
        });
    }

    function updateNotificationsDropdown(notifications) {
        const dropdownMenu = document.querySelector('#notificationBell + .dropdown-menu');
        if (!dropdownMenu) return;

        dropdownMenu.innerHTML = ''; // Clear existing items

        if (notifications.length === 0) {
            let noNotificationsItem = document.createElement('li');
            noNotificationsItem.innerHTML = '<a class="dropdown-item" href="#">No new notifications</a>';
            dropdownMenu.appendChild(noNotificationsItem);
        } else {
            notifications.forEach(notification => {
                let listItem = document.createElement('li');
                listItem.innerHTML = `<a class="dropdown-item" href="${notification.url}">${notification.message}</a>`;
                dropdownMenu.appendChild(listItem);
            });
            let divider = document.createElement('li');
            divider.innerHTML = '<hr class="dropdown-divider">';
            dropdownMenu.appendChild(divider);
            let viewAllItem = document.createElement('li');
            viewAllItem.innerHTML = '<a class="dropdown-item" href="#">View all notifications</a>';
            dropdownMenu.appendChild(viewAllItem);
        }
    }

    // You might want to periodically fetch notifications or use SignalR
    // setInterval(fetchNotifications, 60000); // Fetch every minute

    // 6. Search auto-submit (Placeholder - requires a search input and form)
    // Example: if you have an input like <input type="search" id="searchInput" data-autosubmit-target="#searchForm">
    // document.getElementById('searchInput')?.addEventListener('input', function() {
    //     const form = document.querySelector(this.dataset.autosubmitTarget);
    //     if (form) {
    //         form.submit(); // Or use ajaxRequest to submit form data
    //     }
    // });
});