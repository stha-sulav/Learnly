// wwwroot/js/notifications.js

"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/hubs/notifications").build();

connection.on("ReceiveNotification", function (notification) {
    console.log("Received notification:", notification);

    // Assuming 'notification' is an object like { title: "...", body: "...", url: "..." }
    var notificationList = document.getElementById("notificationList");
    if (notificationList) {
        // Remove "No new notifications" message if it exists
        var noNotificationsMessage = document.getElementById("noNotificationsMessage");
        if (noNotificationsMessage) {
            noNotificationsMessage.remove();
        }

        var listItem = document.createElement("li");
        var anchor = document.createElement("a");
        anchor.className = "dropdown-item";
        anchor.href = notification.url || "#"; // Link to the notification URL
        anchor.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="bi bi-bell-fill me-2 text-primary"></i>
                <div>
                    <h6 class="mb-0">${notification.title}</h6>
                    <small class="text-muted">${notification.body}</small>
                </div>
            </div>
        `;
        listItem.appendChild(anchor);
        notificationList.prepend(listItem); // Add new notifications to the top

        // Optional: Update a notification counter
        var notificationCounter = document.getElementById("notificationCounter");
        if (notificationCounter) {
            notificationCounter.textContent = parseInt(notificationCounter.textContent || '0') + 1;
            notificationCounter.classList.remove('d-none'); // Show counter if hidden
        }
    }
});

connection.start().then(function () {
    console.log("SignalR Connected.");
}).catch(function (err) {
    return console.error(err.toString());
});


