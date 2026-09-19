# EduTrack — Comprehensive Implemented Features Documentation

> **Project Name:** EduTrack (Academic Information Management & Early Warning System)  
> **Platform & Framework:** ASP.NET Core (.NET 10 MVC) | Entity Framework Core  
> **Database:** Microsoft SQL Server (LocalDB `MSSQLLocalDB` / `EduTrackDb`)  
> **Authentication:** ASP.NET Core Identity (RBAC with Roles: `Admin`, `Teacher`, `Student`)  
> **Last Updated:** September 19, 2026  
> **Repository:** [EduTrack](https://github.com/EnidHasan/EduTrack)  

---

## 1. Executive System Overview

**EduTrack** is a centralized academic management and early-warning portal engineered to streamline university workflows across three authenticated user roles: **Administrators**, **Teachers**, and **Students**. 

The system provides complete governance over student enrollment, faculty allocations, continuous grading assessment, official transcript generation, grade dispute rechecks, automated CGPA recalculation, early academic/attendance risk detection, and institutional PDF/CSV reporting.

---

## 2. Implemented Features Summary Matrix

| Domain / Functional Area | Status | Key Controllers & Services | Primary Views & Components |
| :--- | :---: | :--- | :--- |
| **Authentication & RBAC** | **Complete** | `AccountController`, `Program.cs` | `Account/Login`, `ChangePassword`, `Profile`, `AccessDenied` |
| **User Administration & Security** | **Complete** | `UsersController`, `ApplicationUser` | `Users/Index`, `Users/Form` |
| **Student Management & Profiles** | **Complete** | `StudentsController`, `Student` | `Students/Index`, `Students/Form`, `Students/Details`, `Delete` |
| **Teacher & Faculty Management** | **Complete** | `TeachersController`, `Teacher` | `Teachers/Index`, `Teachers/Form`, `Delete` |
| **Course Catalog & Allocations** | **Complete** | `CoursesController`, `Course` | `Courses/Index`, `Courses/Form`, `Delete` |
| **Enrollment & Roster Governance** | **Complete** | `EnrollmentsController`, `Enrollment`| `Enrollments/Index`, `Enrollments/Form`, `Delete` |
| **Teacher Continuous Assessment & Grading** | **Complete** | `TeacherController`, `GradeCalculatorService` | `Teacher/Index`, `Teacher/Course`, `Teacher/GradeEntry` |
| **Student Portal, CGPA & Transcripts** | **Complete** | `StudentController`, `CgpaCalculationService` | `Student/Index` (Dashboard, Transcript, CGPA summary) |
| **Grade Dispute / Recheck Pipeline** | **Complete** | `RecheckService`, `RecheckRequest` | `Student/Disputes`, `Teacher/Disputes` |
| **Early Warning Core Engine (At-Risk)** | **Complete** | `AtRiskEvaluationService`, `AtRiskFlag` | `Student/Index` (Risk Banner), Background Sync |
| **Teacher / Admin At-Risk Warning Panel** | **Complete** | `EarlyWarningController` | `EarlyWarning/Index` |
| **CSV Export for Advisory Interventions** | **Complete** | `EarlyWarningController.ExportCsv` | Direct CSV stream download |
| **Institutional Analytics & System Reports** | **Complete** | `ReportsController` | `Reports/Index` |
| **Printable PDF Institutional Report Export**| **Complete** | `SystemReportPdfBuilder`, `ReportsController.ExportPdf` | Direct PDF stream download |
| **Database Initialization & Test Seeder** | **Complete** | `DbInitializer`, `DbSeeder`, `DashboardController` | Admin Dashboard "Seed Database" action |
| **Automated Testing Suites** | **Complete** | `EndToEndFlowTests`, `ui-smoke.test.mjs` | xUnit integration test & Playwright/Puppeteer smoke script |
| **Design System & Responsive UI** | **Complete** | `premium-theme.css`, `checkpoint2.css`, `site.js` | Shared `_Layout.cshtml`, custom SVG branding |

---

## 3. Deep-Dive Feature Breakdown

### 3.1. Authentication, Security & Account Governance
- **Role-Based Access Control (RBAC):** Native integration with ASP.NET Core Identity supporting three strictly isolated roles:
  - `Admin`: Full institutional governance, user management, course setup, and institutional reporting.
  - `Teacher`: Assigned course rosters, continuous mark entry, dispute review, and class-scoped at-risk student monitoring.
  - `Student`: Academic transcript viewing, cumulative CGPA tracking, grade recheck disputes, and personalized warning alerts.
- **Secure by Default:** Universal fallback policy requires authentication for all endpoints unless explicitly annotated with `[AllowAnonymous]`.
- **First-Login Password Policy:** Custom pipeline middleware forces immediate password update if `MustChangePassword == true`.
- **Account Disabling / Suspension:** Active status check middleware revokes access and signs out deactivated profiles (`IsActive == false`).
- **Account Lockout:** Temporary lockout activated upon 5 consecutive failed login attempts.
- **User Profile Self-Service:** Authenticated users can view credentials, update full names, update phone numbers, and change passwords.
- **User Administration (`UsersController`):**
  - Searchable user index showing role badges, creation dates, active states, and linked student/teacher entities.
  - Create admin user accounts with temporary passwords.
  - Automated sync across identity and linked student/teacher records upon edit.
  - Legacy account linking repair (`RepairLegacyAccountLinksAsync`).

---

### 3.2. Academic Entity Core & Enrollment Management
- **Student Profile Management (`StudentsController`):**
  - Full CRUD operations with unique `RollNumber` and `Email` validation.
  - Automatic linked ASP.NET Core Identity user account generation on student creation.
  - **Student Dossier / Details View (`Students/Details.cshtml`):** Comprehensive view showing student personal info, department, enrollment date, cumulative CGPA, and a historical table of all enrolled courses, instructor names, marks, letter grades, and credits.
  - Cascading cleanup: deleting a student securely deletes their linked identity login account.
- **Teacher / Faculty Management (`TeachersController`):**
  - Full CRUD operations with unique `EmployeeId` and department designations.
  - Automatic linked identity account generation with temporary password and `Teacher` role.
  - Deletion safeguards preventing orphaned courses or conflicting dispute references.
- **Course Catalog Management (`CoursesController`):**
  - Full CRUD operations for course registration (Course Code, Title, Credit Hours, Department).
  - Teacher allocation: assign active faculty instructors to courses.
- **Course Enrollment (`EnrollmentsController`):**
  - Enroll students into courses by Academic Year and Semester.
  - Uniqueness constraint preventing duplicate enrollments for the same student, course, year, and semester.
  - Admin view with multi-attribute filtering (Student, Course, Year, Semester).

---

### 3.3. Continuous Assessment & Teacher Grading Engine
- **Faculty Course Portal (`TeacherController`):**
  - Personalized teacher dashboard showing all assigned courses and enrolled student counts.
  - Course roster view displaying enrolled students and individual grading statuses.
- **Continuous Assessment Mark Breakdown (`Teacher/GradeEntry.cshtml`):**
  - Standardized mark breakdown:
    - **Assignment:** Max 20 Marks
    - **Attendance:** Max 10 Marks
    - **Midterm Examination:** Max 20 Marks
    - **Final Examination:** Max 50 Marks
    - **Total:** Max 100 Marks
- **Automated Grading Service (`GradeCalculatorService`):**
  - Instant translation of 100-point composite marks into Letter Grades and Grade Points (4.00 scale):
    - `80 – 100` : **A+** (4.00)
    - `75 – 79`  : **A**  (3.75)
    - `70 – 74`  : **A-** (3.50)
    - `65 – 69`  : **B+** (3.25)
    - `60 – 64`  : **B**  (3.00)
    - `55 – 59`  : **B-** (2.75)
    - `50 – 54`  : **C+** (2.50)
    - `45 – 49`  : **C**  (2.25)
    - `40 – 44`  : **D**  (2.00)
    - `< 40`     : **F**  (0.00)

---

### 3.4. Student Portal, Transcripts, CGPA & Dispute Workflow
- **Student Dashboard (`StudentController`):**
  - Current cumulative GPA (**CGPA**) badge calculated across all graded courses weighted by credit hours.
  - Total completed credit hours metric.
  - Semester-by-semester GPA performance cards.
  - Comprehensive transcript table listing Course Code, Title, Credits, Component Marks, Final Grade, and Grade Points.
- **Persistent CGPA Architecture (`CgpaCalculationService`):**
  - Persistent `Student.CGPA` column updated automatically upon grading and dispute resolutions.
- **Grade Dispute / Recheck Pipeline (`RecheckService`):**
  - **Student Request Submission:** Students can flag any graded course for review with explanatory notes (`Student/Disputes.cshtml`).
  - **Teacher Dispute Processing:** Teachers view dispute queues specific to their courses and can `Approve` or `Reject` requests with resolution notes (`Teacher/Disputes.cshtml`).
  - **Automated Recalculation:** On teacher approval and grade modification, the student's cumulative CGPA is automatically re-evaluated and persisted.
  - **Audit Trail:** Full status tracking (`Pending`, `Approved`, `Rejected`) visible to students and faculty.

---

### 3.5. Early Warning Core Engine & At-Risk Detection
- **Configurable Risk Evaluation Engine (`AtRiskEvaluationService`):**
  - Configurable risk thresholds defined in `appsettings.json`:
    - **High Risk:** GPA < `2.25` OR Attendance < `60.0%`
    - **Medium Risk:** GPA between `2.25` and `2.75` OR Attendance between `60.0%` and `75.0%`
    - **Low / Normal:** Performance meeting or exceeding thresholds.
  - Risk classification taxonomy:
    - `Academic Risk` (triggered by GPA)
    - `Attendance Risk` (triggered by low attendance)
    - `Compound Risk` (both academic and attendance criteria breached)
  - Background database synchronization: updates and caches status in the `AtRiskFlags` table.
- **Student Early-Warning Banner:**
  - High-visibility banner on the student portal warning the student if they fall into High or Medium risk categories, promoting early academic intervention.
- **Teacher / Admin Warning Panel (`EarlyWarningController`):**
  - High-level KPI summary cards: Total At-Risk, High Risk, Medium Risk, Attendance Risk, and Academic Risk counts.
  - Granular multi-factor filtering: Department, Risk Level, Risk Type, and Student search query.
  - Scoped privacy authorization: Teachers can only view at-risk students enrolled in their classes; Admins view institutional university-wide records.
- **CSV Data Export (`ExportCsv`):**
  - Formatted CSV export for university academic boards, department chairs, and student advisors.

---

### 3.6. Institutional Reporting & Analytics
- **System Reports Dashboard (`ReportsController`):**
  - Executive overview displaying university student counts, at-risk ratios, institutional mean GPA, and mean attendance percentage.
  - Departmental distribution chart and semester-based filters.
- **Printable PDF Export (`SystemReportPdfBuilder`):**
  - Generates downloadable, formatted PDF institutional reports on-the-fly (`Reports/ExportPdf`).

---

### 3.7. Database Seeding & Mock Data
- **Application Startup Seed (`DbInitializer`):**
  - Executes EF Core migrations (`MigrateAsync`).
  - Creates default roles (`Admin`, `Teacher`, `Student`).
  - Provisions the primary administrator account (`admin@edutrack.edu` / `Admin@12345`).
- **Integration Test & Showcase Seeder (`DbSeeder`):**
  - Rich sample data covering CSE, EEE, and BBA departments.
  - Pre-seeded teachers, students, course catalogs, enrollments, graded assessments, pending dispute requests, and at-risk records.
  - Interactive "Seed Database" trigger button in the Admin Dashboard (`DashboardController.SeedDatabase`).

---

### 3.8. Testing & Quality Assurance
- **Integration Test Suite (`EduTrack.Tests/EndToEndFlowTests.cs`):**
  - Automated xUnit test utilizing `WebApplicationFactory<Program>` simulating end-to-end teacher dispute approval, grade update, and verification of student CGPA recalculation.
- **Unit Test Suite (`EduTrack.Tests/UnitTest1.cs`):**
  - Base unit testing harness.
- **UI Smoke Test Script (`tests/ui-smoke.test.mjs`):**
  - Automated smoke test verifying authentication, admin dashboard, teacher grading interface, and student transcript pages.

### 3.9. Course Materials Management
- **Centralized File Sharing & Storage:**
  - Multi-file upload support for PDFs, Office documents, presentations, images, and archives up to 50MB per file.
  - Safe storage with sanitized unique filenames and dedicated secure disk directory.
- **Role-Based Access Control:**
  - **Admin View (`/CourseMaterials/AdminIndex`):** Complete institutional oversight across all courses and instructors with filtering and direct download.
  - **Teacher View (`/CourseMaterials/TeacherIndex`):** Upload, view, and manage course materials scoped strictly to assigned courses.
  - **Student View (`/CourseMaterials/StudentIndex`):** Access and download course materials exclusively for actively enrolled courses.

---

### 3.10. Academic Analytics Subsystem
- **Real-Time Database Aggregations (`AcademicAnalyticsService`):**
  - Live Pass/Fail distribution calculation based on existing grading thresholds (Pass $\ge 40$, Fail $< 40$).
  - Dynamic course average calculation out of 100 based on actual student grading records.
  - Safe calculations preventing division-by-zero or `NaN` outputs for ungraded or new courses.
- **Interactive Visualizations (Chart.js):**
  - **Pass/Fail Distribution Donut Chart:** Shows pass vs fail student counts and percentages with custom tooltips.
  - **Course Average Performance Horizontal Bar Chart:** Clean horizontal bars showing average marks out of 100 with hover tooltips.
  - **Course Performance Summary Table:** Accessible tabular view with course codes, enrollment counts, pass counts, fail counts, and colored progress bars.
  - **4 KPI Metric Cards:** Graded Students, Average Final Mark, Pass Rate, and Fail Rate.
- **Dynamic Multi-Criteria Filters:**
  - Filter by Academic Year, Semester, Department (Admin), and Course with instant updates and empty-state handling.
- **Strict Role-Based Scoping:**
  - **Admin:** University-wide analytics across all departments and courses.
  - **Teacher:** Scoped strictly to courses assigned to the authenticated faculty member; access to other courses or departments is rejected server-side.
  - **Student:** Access restricted via `AcademicStaff` authorization policy.
- **Automated Test Suite:** 14 dedicated xUnit tests covering role access, scoping, pass/fail counts, percentages, course averages, filters, and no-data scenarios.

---

### 3.11. Design System & Frontend Architecture
- **Custom Premium Theme (`premium-theme.css`, `checkpoint2.css`):**
  - Professional SaaS UI layout with dark navy/indigo sidebar and clean neutral content area.
  - Responsive layouts with modern card components, KPI statistic blocks, badge indicators, and data tables.
  - Custom vector branding with SVG logo (`edutrack-logo.svg`).
  - Clean modal dialogs, loading states, empty states, and toast notifications.

---

## 4. Default Credentials for Testing

| Role | Email Address | Password | Primary Entry URL |
| :--- | :--- | :--- | :--- |
| **System Admin** | `admin@edutrack.edu` | `Admin@12345` | `/Dashboard` |
| **Teacher (Sample)** | `tanvir.ahmed@edutrack.edu` | `Teacher@123` | `/Teacher` |
| **Student (Sample)** | `john.doe@student.edutrack.edu` | `Student@123` | `/Student` |
