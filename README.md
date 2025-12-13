# Learnly - LLM-powered Courses MVP

This project is an online learning platform (Udemy/Coursera style) built with .NET 9 (Razor Pages) and a modern frontend stack.

## Tech Stack

-   **Backend**: .NET 9 (Razor Pages + Minimal APIs)
-   **Frontend**: HTML5, CSS3, JavaScript, Bootstrap 5, jQuery
-   **Real-time**: SignalR
-   **Database**: Microsoft SQL Server
-   **Authentication**: ASP.NET Core Identity
-   **Containerization**: Docker

## Setup and Running the Application

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Docker Desktop](https://www.docker.com/products/docker-desktop) (for running with Docker)
-   A SQL Server instance (if not using Docker). You can use the LocalDB instance installed with Visual Studio.

### Running without Docker

1.  **Clone the repository**
2.  **Configure the database connection**
    -   Open `appsettings.Development.json`.
    -   Make sure the `DefaultConnection` connection string is pointing to your SQL Server instance.
3.  **Apply database migrations**
    ```bash
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```
4.  **Run the application**
    ```bash
    dotnet run
    ```
5.  **Seed the database**
    -   The application will automatically seed the database with some initial data when it starts up. This includes a default admin user.
    -   **Admin User Credentials:**
        -   **Email:** admin@learnly.com
        -   **Password:** Admin@123

### Resetting the Database (Optional)

If you want to remove the seeded placeholder courses and start with a fresh database:
1.  Drop the existing database from your SQL Server instance.
2.  Delete the `Migrations` folder in the `Data` directory.
3.  Run the following commands to create a new migration and apply it:
    ```bash
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

### Running with Docker

1.  **Clone the repository**
2.  **Set the database password**
    -   Open the `.env` file and set a strong password for the `SA_PASSWORD` variable.
3.  **Run Docker Compose**
    ```bash
    docker-compose up -d
    ```
4.  **Access the application**
    -   The application will be available at `http://localhost:8080`.

## Storage and File Uploads

-   **Storage Path**: Uploaded video files are stored in the `wwwroot/videos` directory.
-   **Upload Limits**: The maximum file size for uploads is determined by the web server configuration. The default for Kestrel is approximately 30MB.
-   **Cleanup**: An automated background job runs daily to clean up orphaned video files (files that are not associated with any lesson).

## Admin Tasks

-   **Orphaned File Cleanup**: The cleanup job is configured in `appsettings.json`.
    -   To enable automatic deletion of orphaned files, set `FileCleanup:DeleteOrphanedFiles` to `true`. By default, it is `false` and only logs the orphaned files.

## How to Test

1.  **Register and Login**:
    -   Register as a new student or instructor, or log in with the default admin user.
2.  **Enroll in a Course**:
    -   As a student, navigate to the course catalog, select a course, and enroll.
3.  **Play a Video**:
    -   Go to a lesson page and play the video.
4.  **Mark as Complete**:
    -   Click the "Mark as Complete" button. The UI should update instantly.
5.  **Resume Playback**:
    -   Navigate away from the lesson and then come back. The video should resume from where you left off.
6.  **Create a Course (Instructor)**:
    -   Log in as an instructor, go to the instructor dashboard, and create a new course.
    -   Add modules and lessons to the course.
    -   Upload a video for a lesson.

## API Documentation (Swagger)

The OpenAPI/Swagger specification is available when the application is running.
-   Navigate to `/swagger` to view the API documentation and test the endpoints.
-   The `swagger.json` file can be found at `/swagger/v1/swagger.json`.
