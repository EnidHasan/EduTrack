# EduTrack Deployment Guide (Render)

This document provides step-by-step instructions for deploying the EduTrack .NET web application to [Render](https://render.com) using Docker, with a PostgreSQL database.

## 1. Environment Setup

### Provision the Database (PostgreSQL)
1. Log into your Render Dashboard.
2. Click **New** -> **PostgreSQL**.
3. Fill in the details (Name, Region) and select the **Free** instance type.
4. Click **Create Database**.
5. Once created, copy the **Internal Database URL** (e.g., `postgres://user:pass@host/db`). You will need this for the web service.

### Provision the Web Service
1. On the Render Dashboard, click **New** -> **Web Service**.
2. Connect your GitHub repository containing the `EduTrack` code (make sure you push the `ISD-Lab` branch).
3. Select the **Docker** environment. Render will automatically detect the `Dockerfile` at the root of the repository.
4. Set the Instance Type to **Free**.
5. Click **Advanced** and add the following Environment Variables:
   - `ConnectionStrings__DefaultConnection` : Set this to the **Internal Database URL** from the PostgreSQL database step.
   - `ASPNETCORE_ENVIRONMENT` : `Production`
6. Click **Create Web Service**.

---

## 2. Build Commands & Deployment Steps

### Dockerfile Details
The project includes a multi-stage `Dockerfile` that:
- Uses `mcr.microsoft.com/dotnet/sdk:10.0` to build and publish the app.
- Uses `mcr.microsoft.com/dotnet/aspnet:10.0` as the final lightweight runtime image.
- Exposes port `8080`, which Render automatically binds to.

### Database Migrations
Entity Framework Core migrations will automatically run on application startup in production to initialize the Render PostgreSQL database. This is handled inside `DbInitializer.cs` or `Program.cs` before `app.Run()`.

---

## 3. Verification Checks

1. Once the Render Web Service displays **Live**, click the URL (e.g., `https://edutrack.onrender.com`).
2. Verify that the login screen loads.
3. Use the default administrator credentials to log in.
4. Verify that data (like profiles and courses) loads correctly from the PostgreSQL database.
5. Upload a profile picture to ensure file storage works (Note: Render's free tier uses an ephemeral filesystem; uploaded profile pictures will disappear on the next deployment unless you configure a persistent disk, which requires a paid tier. For demonstration purposes, ephemeral uploads are acceptable).

---

## 4. Runbook: Monitoring & Troubleshooting

### Viewing Logs
- Navigate to your Web Service in the Render dashboard and click **Logs**.
- Any 500 errors or application crashes will be printed here directly from the .NET console logger.

### Common Issues
**Database connection failures:**
Ensure the `ConnectionStrings__DefaultConnection` environment variable matches the internal PostgreSQL URL exactly.

**Cold Starts:**
Render's Free tier spins down web services after 15 minutes of inactivity. The first request after a spin-down may take up to 50 seconds to respond as the Docker container boots up. This is normal behavior on the free tier.
