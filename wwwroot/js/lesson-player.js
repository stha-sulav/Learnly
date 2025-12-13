document.addEventListener('DOMContentLoaded', function () {
    const video = document.getElementById('lesson-video');
    const markAsCompletedButton = document.getElementById('mark-as-completed-btn');
    const lessonDataElement = document.getElementById('lesson-data');
    const lessonId = lessonDataElement.dataset.lessonId;
    let initialPosition = parseInt(lessonDataElement.dataset.initialPosition || '0', 10);

    // Function to send completion status to the server
    async function completeLesson() {
        const originalButtonText = markAsCompletedButton.innerHTML;
        const originalButtonDisabled = markAsCompletedButton.disabled;

        // Optimistic UI Update
        markAsCompletedButton.disabled = true;
        markAsCompletedButton.innerHTML = '<i class="bi bi-check-circle-fill me-2"></i> Completed';
        showSpinner(markAsCompletedButton, ''); // Show a small spinner inside the button

        try {
            const response = await fetch('/api/progress/complete-lesson', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('meta[name="RequestVerificationToken"]').content
                },
                body: JSON.stringify({ lessonId: parseInt(lessonId) })
            });

            if (!response.ok) {
                // Revert on failure
                markAsCompletedButton.innerHTML = originalButtonText;
                markAsCompletedButton.disabled = originalButtonDisabled;
                showAlert('Failed to mark lesson as completed. Please try again.', 'error');
            }
            // On success, the UI is already updated, so we do nothing.

        } catch (error) {
            console.error('Error:', error);
            // Revert on error
            markAsCompletedButton.innerHTML = originalButtonText;
            markAsCompletedButton.disabled = originalButtonDisabled;
            showAlert('An unexpected error occurred. Please check your connection and try again.', 'error');
        } finally {
            hideSpinner(markAsCompletedButton);
        }
    }

    // Function to send current position to the server
    async function updatePosition(position) {
        try {
            await fetch('/api/progress/update-position', {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('meta[name="RequestVerificationToken"]').content
                },
                body: JSON.stringify({ lessonId: parseInt(lessonId), positionSeconds: Math.floor(position) })
            });
        } catch (error) {
            console.error('Failed to update position:', error);
        }
    }

    if (video) {
        // Seek to initial position if greater than 5 seconds
        video.addEventListener('loadedmetadata', function() {
            if (initialPosition > 5 && !video.ended) {
                video.currentTime = initialPosition;
            }
        });

        let lastSentPosition = initialPosition;
        // Update position periodically (e.g., every 15 seconds)
        video.addEventListener('timeupdate', function () {
            const currentTime = Math.floor(video.currentTime);
            // Only send update if position has changed by at least 15 seconds or more than 15 seconds passed since last update
            if (Math.abs(currentTime - lastSentPosition) >= 15) {
                updatePosition(currentTime);
                lastSentPosition = currentTime;
            }
        });

        // Also send final position when video ends
        video.addEventListener('ended', function() {
            updatePosition(video.duration);
            completeLesson(); // Mark as complete when video ends
        });
    }

    // Event listener for button click
    if (markAsCompletedButton) {
        markAsCompletedButton.addEventListener('click', completeLesson);
    }
});