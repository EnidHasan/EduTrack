# EduTrack

EduTrack is a role-based academic information system designed to bring university academic operations into one secure and organized platform. It supports three primary roles—Admin, Teacher, and Student—and provides each user with access to the features appropriate to their responsibilities.

The system enables administrators to manage students, teachers, courses, user accounts, and academic records. Teachers can work with their assigned courses and grading activities, while students can access their academic information and related services. EduTrack uses ASP.NET Core Identity for secure authentication and role-based access, Entity Framework Core for data management, and Microsoft SQL Server as its database.

The broader project includes course enrollment, automated grade calculation, student transcripts, grade-recheck workflows, academic-risk detection, dashboards, and reporting. Its purpose is to reduce repetitive administrative work, protect academic information, improve transparency, and provide students and faculty with a reliable university-wide academic workspace.

## Live Demo

The deployed version of EduTrack is available here:

https://edutrack-xt8h.onrender.com/

## Default Administrator Login

```text
Email: admin@edutrack.edu
Password: Admin@12345
```

## Deployment Documentation

For instructions on deploying the application to Render (Docker, PostgreSQL), please refer to the [Deployment Guide](docs/deployment.md).
