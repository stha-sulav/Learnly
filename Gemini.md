# Gemini — LLM-powered Courses MVP (Udemy/Coursera-like)

> **Purpose:** a lean, professional MVP for an online learning platform (Udemy / Coursera style) built with **.NET 9 (Razor Pages)** and a modern frontend stack (HTML, CSS, JS, Bootstrap, Bootstrap Icons, jQuery). Focus is on developer-friendly architecture, strong UX, and production-minded features such as real-time notifications, progress tracking, quizzes/assessments, comments, and admin tools.

---

## 1. Vision & Goals

- Launch a functional MVP to deliver instructor-created courses with video lessons, quizzes, and progress tracking.
- Provide a delightful learning experience across desktop and mobile using Bootstrap UI components.
- Build a maintainable architecture so new LLM features (recommendations, assistant help) can be integrated later.

---

## 2. Tech Stack

- Backend: **.NET 9** (Razor Pages + minimal APIs where appropriate)
- Frontend: **HTML5, CSS3, JavaScript, Bootstrap 5**, **Bootstrap Icons**, **jQuery** (for small interactions where needed)
- Real-time: **SignalR** (for live notifications, presence, and progress updates)
- Database: **Microsoft SQL Server** or **Postgres** with **Entity Framework Core**
- Authentication/Authorization: **ASP.NET Core Identity** (roles: Admin, Instructor, Student, Manager)
- Storage: Local file system for MVP media or S3-compatible storage for production (videos, thumbnails)
- Search/Indexing (optional MVP+): **Elasticsearch** or simple DB full-text search
- Containerization: **Docker** for dev/prod parity
- CI/CD: GitHub Actions or Azure Pipelines

---

## 3. High-level Architecture

- Presentation: Razor Pages + static SPA-like frontend assets
- Application: Services (CourseService, UserService, QuizService, NotificationService)
- Domain: Entities and business rules
- Infrastructure: EF Core, Identity, SignalR hubs, Storage adapters

Diagram (text):

```
[Browser] <--HTTPS--> [Razor Pages / APIs] <---> [Application Services] <---> [EF Core / SQL DB]
                                    |-- SignalR Hub --|
                                    |-- Storage (S3/fs)--|
```

---

## 4. Core Models / Database Schema (MVP)

- **Users** (Id, Email, DisplayName, PasswordHash, Role, CreatedAt)
- **Courses** (Id, Title, Slug, Description, InstructorId, CategoryId, Price, ThumbnailPath, CreatedAt, Published)
- **Modules** (Id, CourseId, Title, Order)
- **Lessons** (Id, ModuleId, Title, ContentType(video/article/markdown), ContentPath/HTML, DurationSeconds, Order)
- **Enrollments** (Id, UserId, CourseId, EnrolledAt, IsActive)
- **Progress** (Id, UserId, LessonId, CompletedAt, PositionSeconds)
- **Quizzes** (Id, LessonId, Title, PassingScore, AttemptsAllowed)
- **Questions** (Id, QuizId, Text, Type(mcq/multi/short), Options(json))
- **Attempts** (Id, UserId, QuizId, Score, StartedAt, CompletedAt, Answers(json))
- **Comments** (Id, UserId, CourseId (nullable), LessonId (nullable), ParentId, Text, CreatedAt, IsEdited)
- **Notifications** (Id, UserId, Title, Body, Url, IsRead, CreatedAt)
- **Roles & Permissions** (built-in via Identity)

---

## 5. Key Features (MVP +)

### Core (MVP)

- Course catalog and search
- Course pages with modules & lessons
- Video lessons (playback + resume)
- Enrollments and simple pricing (free / paid placeholder)
- Progress tracking (lesson completion auto-saved)
- Quizzes per lesson with grading and attempts
- Comments & threaded replies under lessons and courses
- Basic Instructor Dashboard (create/edit course, upload lessons)
- Student Dashboard (my courses, progress, certificates placeholder)

### Professional-level Enhancements

- Real-time Notifications (SignalR): new course releases, quiz results, replies
- Rich progress analytics (time spent, progress % per course)
- Achievement / badges
- Admin panel: course approvals, user management
- Moderation tools for comments
- Email notifications (background jobs with Hangfire or Azure WebJobs)
- LLM features (future): automatically generate course outlines, suggest quiz questions, summarize lessons

---

## 6. UX / Pages

- Home / Landing — featured courses, categories, search bar
- Catalog / Search results — filters (category, instructor, level), sorting
- Course details — description, curriculum tree, instructor bio, reviews
- Lesson player — video + transcript + comments + quiz widget
- Quiz page / inline quiz modal
- Instructor Studio — create/edit course, reorder modules/lessons, upload media
- Student Dashboard — enrolled courses, progress, recent activity
- Notifications panel (bell icon) — drop-down + full notifications page
- Admin Dashboard — manage users, courses, reports

---

## 7. API & Endpoint Examples

> Implementation choice: Put most UI in Razor Pages, but expose APIs (`/api/...`) for SPA-like modules and mobile apps.

**Auth**

- `POST /account/login` — login
- `POST /account/register` — register
- `POST /account/logout` — logout

**Courses**

- `GET /api/courses` — list & search
- `GET /api/courses/{slug}` — course details
- `POST /api/courses` — create (Instructor)
- `PUT /api/courses/{id}` — update

**Enrollments & Progress**

- `POST /api/courses/{id}/enroll` — enroll user
- `GET /api/users/{id}/courses` — user courses
- `POST /api/lessons/{lessonId}/progress` — save progress `{ positionSeconds, completed }`

**Quizzes**

- `GET /api/quizzes/{quizId}`
- `POST /api/quizzes/{quizId}/attempt` — submit answers

**Comments**

- `GET /api/comments?courseId=&lessonId=`
- `POST /api/comments` — create
- `DELETE /api/comments/{id}` (moderator)

**Notifications**

- `GET /api/notifications` — list
- `POST /api/notifications/mark-read` — bulk mark read

---

## 8. Real-time: Notifications & Presence

- Use **SignalR Hub** (`/hubs/notifications`) for pushing:

  - New messages/replies
  - Quiz grading/completion
  - Live announcements

- Client subscribes using a small JS wrapper tying the bell icon and the notifications page to the hub.

---

## 9. Progress Tracking & Resume Logic

- Save a `Progress` record per lesson with `PositionSeconds` and `CompletedAt`.
- When lesson opens, fetch last `PositionSeconds` and resume the player.
- Mark lesson complete when watched >= 95% of duration OR student clicks "Mark Complete".
- Periodically (e.g., every 10s or on `timeupdate`) send `POST /api/lessons/{id}/progress`.

---

## 10. Quizzes & Assessments

- Support MCQ, multi-select, and short answer for MVP.
- Store questions as entries with options; server grades objective types.
- Allow attempts and keep `Attempts` history; enforce `AttemptsAllowed`.
- Show immediate feedback and store analytics to Progress and Notifications.

---

## 11. Comments & Moderation

- Threaded comments (ParentId) with edit/delete by owner and delete/flag by moderators.
- Rate-limit and sanitise input; use a server-side HTML sanitizer for safety.

---

## 12. Authentication & Authorization

- Use **ASP.NET Core Identity** with roles.
- Protect instructor actions behind role checks and optionally approval workflows.

---

## 13. Dev Setup & Commands (quick)

```bash
# scaffold Razor Pages app
dotnet new webapp -n Gemini
cd Gemini
# add EF Core & SQL Server provider
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
# add Identity
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
# add SignalR
dotnet add package Microsoft.AspNetCore.SignalR
# add Hangfire (optional for background jobs)
dotnet add package Hangfire.Core

# run migrations (example)
dotnet ef migrations add InitialCreate
dotnet ef database update

# run
dotnet run
```

---

## 14. File Structure (suggested)

```
/Gemini
  /Areas
    /Identity
  /Data
    ApplicationDbContext.cs
    Migrations/
  /Pages
    /Account
    /Courses
      Index.cshtml
      Details.cshtml
    /Instructor
    /Student
  /Services
    CourseService.cs
    QuizService.cs
    NotificationService.cs
  /Hubs
    NotificationHub.cs
  /wwwroot
    /css
    /js
    /media
  appsettings.json
  Program.cs
```

---

## 15. Minimal MVP Roadmap (4 sprints — 2 weeks each recommended)

- **Sprint 1 — Core commerce & content** (User auth, course CRUD, instructor studio, DB, simple UI) — _MVP functional_
- **Sprint 2 — Learning experience** (lesson player, progress tracking, enrollments, dashboards)
- **Sprint 3 — Engagement** (comments, quizzes, basic notifications via SignalR, email queue)
- **Sprint 4 — Polish & Admin** (admin panel, moderation, analytics, performance optimization, Docker)

---

## 16. Acceptance Criteria / Example User Stories (small, 1–3 story points each)

- As a student I can register/login so I can enroll in courses.
- As a student I can view my enrolled courses and see % progress.
- As an instructor I can create a course and add modules/lessons.
- As a student I can resume video where I left off.
- As a student I can take a quiz and see graded results.
- As any user I can receive real-time notification for quiz result.

---

## 17. Security & Privacy Notes

- Hash passwords with Identity (PBKDF2/Argon2 via provider).
- Sanitize all comment/lesson HTML.
- Protect file uploads and use signed URLs for production storage.

---

--- End of Context from: GEMINI.md ---
