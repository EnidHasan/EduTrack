# EduTrack Deployment Guide (Oracle Cloud Always Free)

This document provides step-by-step instructions for manually deploying the EduTrack .NET web application and its SQL Server database to an Oracle Cloud Always Free VM running Ubuntu Linux (without Docker).

## 1. Environment Setup

### Provision the Oracle Cloud VM
1. Sign up / Log in to Oracle Cloud Infrastructure (OCI).
2. Create a new Compute Instance.
3. **Image:** Ubuntu 22.04 LTS or 24.04 LTS.
4. **Shape:** `VM.Standard.E2.1.Micro` (AMD Always Free, 1GB RAM) or `VM.Standard.A1.Flex` (ARM Always Free).
   > **Note on Architecture & SQL Server:** SQL Server for Linux officially supports x64 (AMD) architectures. If you choose the ARM A1.Flex shape, SQL Server will not install natively without emulation or Docker (which we are avoiding). Therefore, use the AMD Micro shape.
5. **Add Swap Space:** Since SQL Server requires at least 2GB of RAM, and the free AMD VM only has 1GB, you **must** configure a swap file (e.g., 4GB) immediately after SSHing into the VM:
   ```bash
   sudo fallocate -l 4G /swapfile
   sudo chmod 600 /swapfile
   sudo mkswap /swapfile
   sudo swapon /swapfile
   echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
   ```

### Install SQL Server Express (Linux)
1. Import the public repository GPG keys:
   ```bash
   curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
   ```
2. Register the SQL Server Ubuntu repository:
   ```bash
   curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | sudo tee /etc/apt/sources.list.d/mssql-server.list
   ```
3. Install SQL Server:
   ```bash
   sudo apt-get update
   sudo apt-get install -y mssql-server
   ```
4. Run the setup and select the **Express (Free)** edition. Set a strong SA password:
   ```bash
   sudo /opt/mssql/bin/mssql-conf setup
   ```

### Install .NET Runtime
Install the .NET 10.0 ASP.NET Core Runtime (or the version matching `EduTrack.Web`):
```bash
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0
```

### Install Nginx (Reverse Proxy)
```bash
sudo apt-get install -y nginx
```

---

## 2. Build Commands & Deployment Steps

### Step 1: Publish the Application Locally
On your local development machine, open a terminal in the `EduTrack.Web` folder and publish the app:
```bash
dotnet publish --configuration Release -o ./publish
```

### Step 2: Transfer Files to the VM
Use `scp` or an FTP client to transfer the `./publish` folder to your Oracle VM (e.g., to `/var/www/edutrack`):
```bash
scp -i path_to_ssh_key -r ./publish ubuntu@your_oracle_vm_ip:/home/ubuntu/edutrack
```
Move it to `/var/www/edutrack` and fix permissions:
```bash
sudo mkdir -p /var/www/edutrack
sudo cp -r /home/ubuntu/edutrack/* /var/www/edutrack/
sudo chown -R www-data:www-data /var/www/edutrack
```

### Step 3: Configure the Database Connection
Edit the `/var/www/edutrack/appsettings.Production.json` on the server and update your connection string to point to the local SQL Server instance:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EduTrack;User Id=sa;Password=YOUR_STRONG_PASSWORD;TrustServerCertificate=True;"
}
```

### Step 4: Run Database Migrations
Generate an SQL script locally to create the schema, or run the `dotnet ef database update` command directly pointing to the production server to initialize the tables.

### Step 5: Configure systemd (Daemon)
Create a service file to keep the .NET app running in the background:
```bash
sudo nano /etc/systemd/system/edutrack.service
```
Add the following configuration:
```ini
[Unit]
Description=EduTrack .NET Web Application

[Service]
WorkingDirectory=/var/www/edutrack
ExecStart=/usr/bin/dotnet /var/www/edutrack/EduTrack.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=edutrack
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```
Enable and start the service:
```bash
sudo systemctl enable edutrack.service
sudo systemctl start edutrack.service
```

### Step 6: Configure Nginx
Create an Nginx configuration file:
```bash
sudo nano /etc/nginx/sites-available/edutrack
```
Add:
```nginx
server {
    listen 80;
    server_name your_domain.com OR_vm_public_ip;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```
Enable the site and restart Nginx:
```bash
sudo ln -s /etc/nginx/sites-available/edutrack /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```
*Note: Ensure port 80 and 443 are open in the Oracle Cloud VCN Security Lists and the Ubuntu ufw firewall.*

---

## 3. Verification Checks
1. Navigate to your VM's Public IP or Domain in a web browser.
2. The login page should load securely.
3. Log in with the default admin credentials and verify that dashboard analytics and profile images load successfully.
4. Upload a test profile picture to verify file system write permissions in `wwwroot/images/profiles/`.

---

## 4. Runbook: Monitoring & Troubleshooting

### Viewing Application Logs
If the application crashes or throws 500 errors, view the .NET logs:
```bash
sudo journalctl -fu edutrack.service
```

### Restarting the Application
After deploying updates (by replacing the files in `/var/www/edutrack/`), restart the service:
```bash
sudo systemctl restart edutrack.service
```

### Rollback Procedure
1. Always keep a backup of the previous `publish` folder (e.g., `cp -r /var/www/edutrack /var/www/edutrack_backup_v1`).
2. If a new deployment fails, restore the backup:
   ```bash
   sudo rm -rf /var/www/edutrack/*
   sudo cp -r /var/www/edutrack_backup_v1/* /var/www/edutrack/
   sudo systemctl restart edutrack.service
   ```

### High Memory Usage / Database Crashing
If SQL Server crashes, it is almost certainly due to Out-Of-Memory (OOM) on the 1GB VM.
*   **Fix:** Ensure your swap file is active (`swapon --show`). You can increase the swap size to 6GB if it continues crashing.
*   **Check SQL Logs:** `sudo cat /var/opt/mssql/log/errorlog`
