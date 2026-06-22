# Deployment Guide

This guide covers deploying KitchenwareBot to production using Docker and Nginx.

---

## 📋 Prerequisites

- VPS or cloud server (DigitalOcean, Linode, AWS EC2, etc.)
- Ubuntu 20.04+ or similar Linux distribution
- Docker & Docker Compose installed
- Domain name with DNS configured
- Telegram bot token from [@BotFather](https://t.me/botfather)
- SSL certificate (Let's Encrypt recommended)

---

## 🐳 Docker Setup

### 1. Create Directory Structure

```bash
mkdir -p /opt/kitchenware-bot
cd /opt/kitchenware-bot
```

### 2. Clone Repository

```bash
git clone https://github.com/yourusername/kitchenware-wholesale-bot.git .
```

### 3. Configure Environment

Create `docker-compose.override.yml` with secrets:

```yaml
version: '3.8'

services:
  app:
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Server=sqlserver;Database=KitchenwareBot;User Id=sa;Password=YOUR_STRONG_PASSWORD;"
      Redis__Connection: "redis:6379"
      Telegram__BotToken: "YOUR_BOT_TOKEN"
      Telegram__WebhookUrl: "https://yourdomain.com/telegram/webhook"
      Telegram__BotUsername: "your_bot_username"
      Telegram__ChannelId: "-1001234567890"
      Telegram__AdminIds: "[123456789,987654321]"

  sqlserver:
    environment:
      SA_PASSWORD: "YOUR_STRONG_PASSWORD"
```

**⚠️ Security Note:** Use strong passwords. Store this file securely. Do NOT commit to git.

### 4. Start Services

```bash
docker-compose up -d

# View logs
docker-compose logs -f app

# Verify services are running
docker-compose ps
```

---

## 🔐 SSL/TLS Setup

### Option 1: Let's Encrypt with Certbot

```bash
# Install Certbot
sudo apt-get update && sudo apt-get install -y certbot python3-certbot-nginx

# Generate certificate (replace yourdomain.com)
sudo certbot certonly --standalone -d yourdomain.com

# Certificates stored in:
# /etc/letsencrypt/live/yourdomain.com/
```

### Option 2: Self-Signed Certificate (Testing Only)

```bash
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes
```

---

## 🌐 Nginx Reverse Proxy

Create `/etc/nginx/sites-available/kitchenware-bot`:

```nginx
upstream app {
    server 127.0.0.1:5000;
}

server {
    listen 80;
    server_name yourdomain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    # SSL certificate (Let's Encrypt)
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Proxy to app
    location / {
        proxy_pass http://app;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Timeouts
        proxy_connect_timeout 7d;
        proxy_send_timeout 7d;
        proxy_read_timeout 7d;
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/kitchenware-bot /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

---

## 📱 Telegram Webhook Registration

After deployment, register the webhook with Telegram:

```bash
curl -X POST https://api.telegram.org/bot{YOUR_BOT_TOKEN}/setWebhook \
  -H "Content-Type: application/json" \
  -d '{"url": "https://yourdomain.com/telegram/webhook"}'
```

Verify webhook is set:

```bash
curl https://api.telegram.org/bot{YOUR_BOT_TOKEN}/getWebhookInfo
```

Expected output:
```json
{
  "ok": true,
  "result": {
    "url": "https://yourdomain.com/telegram/webhook",
    "has_custom_certificate": false,
    "pending_update_count": 0,
    "ip_address": "YOUR_SERVER_IP"
  }
}
```

---

## 🗄️ Database Setup

### First Time Deployment

```bash
# Run migrations inside container
docker-compose exec app dotnet ef database update \
  --project src/KitchenwareBot.Infrastructure \
  --startup-project src/KitchenwareBot.Bot

# Verify database was created
docker-compose exec sqlserver sqlcmd -S . -U sa -P YOUR_PASSWORD -Q "SELECT name FROM sys.databases;"
```

### Backup & Restore

```bash
# Backup database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S . -U sa -P YOUR_PASSWORD \
  -Q "BACKUP DATABASE [KitchenwareBot] TO DISK = '/var/opt/mssql/backup/kitchenware.bak'"

# Restore database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S . -U sa -P YOUR_PASSWORD \
  -Q "RESTORE DATABASE [KitchenwareBot] FROM DISK = '/var/opt/mssql/backup/kitchenware.bak'"
```

---

## 🔄 Updates & Maintenance

### Deploy New Version

```bash
cd /opt/kitchenware-bot

# Pull latest code
git pull origin main

# Rebuild image
docker-compose build

# Stop old containers
docker-compose down

# Start new version
docker-compose up -d

# Run migrations (if needed)
docker-compose exec app dotnet ef database update
```

### Monitor Logs

```bash
# Live logs
docker-compose logs -f app

# Last 100 lines
docker-compose logs --tail=100 app

# Specific service
docker-compose logs -f sqlserver
docker-compose logs -f redis
```

### Database Maintenance

```bash
# Check SQL Server status
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S . -U sa -P YOUR_PASSWORD \
  -Q "SELECT @@servername, @@version"

# Rebuild indexes (monthly)
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S . -U sa -P YOUR_PASSWORD \
  -Q "ALTER INDEX ALL ON [dbo].[Products] REBUILD"
```

---

## 📊 Monitoring & Alerts

### Check Service Health

```bash
# App health
curl https://yourdomain.com/health

# Redis connectivity
docker-compose exec redis redis-cli ping

# Database connectivity
docker-compose exec app dotnet run --check-db
```

### Set Up Alerts

Use monitoring tools like:
- **Prometheus + Grafana** — metrics and dashboards
- **ELK Stack** — centralized logging
- **Sentry** — error tracking
- **Uptime Robot** — website availability monitoring

Example health check (add to `Program.cs`):

```csharp
app.MapHealthChecks("/health");
```

---

## 🔒 Security Hardening

### File Permissions

```bash
# Restrict docker-compose.override.yml
chmod 600 /opt/kitchenware-bot/docker-compose.override.yml
```

### Firewall

```bash
# UFW example
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable
```

### Regular Updates

```bash
# Update system packages (weekly)
sudo apt-get update && sudo apt-get upgrade -y

# Update containers (monthly)
docker-compose pull
docker-compose up -d

# Certificate renewal (automatic with certbot)
sudo certbot renew --quiet
```

---

## 🚨 Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs app

# Check port conflicts
sudo lsof -i :5000

# Verify environment variables
docker-compose config | grep -A 10 "environment:"
```

### Webhook Not Receiving Updates

```bash
# Check webhook registration
curl https://api.telegram.org/bot{TOKEN}/getWebhookInfo

# Check server logs
docker-compose logs -f app | grep "webhook"

# Test webhook endpoint
curl -X POST https://yourdomain.com/telegram/webhook \
  -H "Content-Type: application/json" \
  -d '{"update_id": 1}'
```

### Database Connection Issues

```bash
# Test from container
docker-compose exec app dotnet user-secrets set ConnectionStrings:Default "Server=sqlserver;Database=KitchenwareBot;User Id=sa;Password=YOUR_PASSWORD;"

# Check SQL Server logs
docker-compose logs -f sqlserver
```

### SSL Certificate Issues

```bash
# Check certificate validity
openssl x509 -in /etc/letsencrypt/live/yourdomain.com/fullchain.pem -text -noout

# Test SSL
openssl s_client -connect yourdomain.com:443

# Renew (if needed)
sudo certbot renew --force-renewal
```

---

## 📈 Scaling Considerations

For high traffic:

1. **Multi-instance deployment:** Run multiple app containers behind load balancer
2. **Database replication:** Set up SQL Server Always-On or failover cluster
3. **Redis cluster:** Use Redis Cluster for distributed state
4. **CDN:** Use CloudFlare or AWS CloudFront for static assets
5. **Async processing:** Consider background job queue (Hangfire, etc.) for non-blocking operations

---

## 🆘 Support

- 📖 **Docs:** [README.md](../README.md) and [docs/](../docs/)
- 🐛 **Issues:** [GitHub Issues](../../issues)
- 💬 **Questions:** [GitHub Discussions](../../discussions)

---

**Last Updated:** 2026-06-22
