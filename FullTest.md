# Learnly - End-to-End Testing Guide

This guide provides a comprehensive set of manual test cases to validate the core functionalities of the Learnly application.

## Prerequisites

Before you begin testing, ensure that:
1.  The application is running locally.
2.  The database has been created and seeded with initial data.

For detailed setup instructions, please refer to the [README.md](README.md) file.

---

## Scenario 1: The Student Experience

This scenario covers the end-to-end flow for a student user, from registration to course completion.

### 1.1. User Registration & Login

1.  **Navigate** to the homepage.
2.  Click on the **"Register"** link in the navigation bar.
3.  Fill out the registration form with valid student details (e.g., a unique email and a strong password).
4.  Click the **"Register"** button.
    -   **Expected Result:** You should be redirected to the login page or automatically logged in.
5.  If not logged in, **log in** with the newly created student credentials.
    -   **Expected Result:** You should be successfully logged in and redirected to your student dashboard or the homepage.

### 1.2. Course Discovery & Enrollment

1.  From the main navigation, find the link to the **course catalog**.
2.  **Browse** the list of available courses.
3.  Click on a course to view its **details page**.
    -   **Expected Result:** You should see the course title, description, instructor, and a list of modules and lessons.
4.  Click the **"Enroll Now"** button.
    -   **Expected Result:** You should be enrolled in the course. The button might change to "Go to Course" or you might be redirected to the lesson player.

### 1.3. Learning & Progress Tracking

1.  Navigate to your **"My Courses"** dashboard.
    -   **Expected Result:** The course you just enrolled in should be listed, showing your current progress (likely 0%).
2.  Click on the course to start learning.
3.  Click on the **first lesson**.
    -   **Expected Result:** The lesson player page should load.
4.  If the lesson contains a video, click the **play button**.
    -   **Expected Result:** The video should start playing.
5.  Watch a portion of the video (e.g., 20-30 seconds), then **navigate away** from the page (e.g., back to the dashboard).
6.  **Return** to the same lesson.
    -   **Expected Result:** The video should automatically resume from the position where you left off.
7.  Click the **"Mark as Completed"** button.
    -   **Expected Result:** The button should immediately change to a "Completed" state (optimistic UI). You can check the browser's network tab to see the API call being made in the background.
8.  Navigate back to the **"My Courses"** dashboard.
    -   **Expected Result:** Your progress for the course should be updated.

---

## Scenario 2: The Instructor Experience

This scenario covers the course creation and management flow for an instructor.

### 2.1. Login as Instructor

1.  Log in with an instructor account. (Note: An admin needs to create an instructor account first, see Scenario 3).
    -   **Expected Result:** You should be redirected to the instructor dashboard.

### 2.2. Course Creation & Management

1.  From the instructor dashboard, click on **"Create New Course"**.
2.  Fill out the course creation form with all the required details (Title, Slug, Description, etc.).
3.  Click **"Create Course"**.
    -   **Expected Result:** The course is created, and you are redirected to the course list.
4.  Find the newly created course in the list and click **"Edit"**.
    -   **Expected Result:** You are taken to the course edit page.

### 2.3. Module and Lesson Management

1.  On the course edit page, scroll down to the **"Modules & Lessons"** section.
2.  In the "New Module Title" input, enter a title for your first module and click **"Add Module"**.
    -   **Expected Result:** A loading spinner should appear briefly, and the new module should appear in the list below. A success notification should be displayed.
3.  For the newly created module, enter a title for a new lesson in the "New Lesson Title" input.
4.  Click the **"Choose File"** button to select a video file for the lesson.
5.  Click the **"Add Lesson"** button.
    -   **Expected Result:**
        -   A loading spinner appears.
        -   A notification confirms the lesson is being created.
        -   A second notification indicates the video is uploading.
        -   Once complete, a final success notification is shown.
        -   The new lesson appears under the correct module, with a link to view the uploaded video.

### 2.4. Handling Upload Errors

1.  Try to add a new lesson, but **do not select a file** before clicking "Add Lesson".
    -   **Expected Result:** The lesson should be created without a video, and no upload-related errors should appear.
2.  Try to add a new lesson and select a very large file (if possible, one that exceeds server limits).
    -   **Expected Result:** A friendly error message should be displayed in a notification, indicating that the upload failed.

---

## Scenario 3: The Admin Experience

This scenario covers user management and system maintenance tasks for an administrator.

### 3.1. Login and User Management

1.  Log in with the default admin credentials:
    -   **Email:** `admin@learnly.com`
    -   **Password:** `Admin@123`
2.  Navigate to the **Admin Dashboard**.
3.  Go to the **"Users"** management page.
    -   **Expected Result:** You should see a list of all users in the system.
4.  **Create a new user** with the "Instructor" role.
    -   **Expected Result:** The new user is created and appears in the user list. This account can then be used for Scenario 2.

### 3.2. File Cleanup Verification

1.  This is a background job that runs once a day. To test it, you can:
    -   **Manually upload a file** to the `wwwroot/videos` directory that is not associated with any lesson.
    -   **Wait for the job to run** (or restart the application to trigger it on startup, if configured that way for testing).
    -   **Check the application logs**.
        -   **Expected Result:** You should see a log message from `FileCleanupService` indicating that it found an orphaned file.
2.  **Test the deletion functionality**:
    -   In `appsettings.json`, set `FileCleanup:DeleteOrphanedFiles` to `true`.
    -   Restart the application.
    -   **Check the logs and the `wwwroot/videos` directory**.
        -   **Expected Result:** The logs should indicate that the file was deleted, and the file should no longer exist in the directory.
