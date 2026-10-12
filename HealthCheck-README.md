# Comprehensive Health Check System for Agrovesto Backend API

This document describes the complete health check system implemented for the Agrovesto Backend API, designed for EC2 deployment with web-accessible monitoring.

## Overview

The health check system provides comprehensive monitoring of all application components:
- **Database & Storage**: SQL Server and AWS S3 connectivity
- **External Services**: Third-party API integrations (IdentityPass, Termii SMS, SendGrid, etc.)
- **API Endpoints**: Your application's REST API endpoints
- **Application Components**: Dependency injection services and business logic
- **Web Dashboard**: Visual health status monitoring

### Key Features
- ✅ **Web-Accessible URLs**: Check health via browser or monitoring tools
- ✅ **Comprehensive Coverage**: Database, APIs, external services, and components
- ✅ **Visual Dashboard**: Beautiful HTML interface for health monitoring
- ✅ **EC2 Optimized**: Designed for direct EC2 deployment with Nginx
- ✅ **Load Balancer Ready**: Endpoints suitable for AWS ALB health checks
- ✅ **Developer Friendly**: Easy to understand JSON responses

## Health Check Endpoints

### 1. Basic Health Check
**URL:** `GET /health`
**Purpose:** Overall application health status
**Response:** Complete health summary with all services
```json
{
  "status": "Healthy",
  "timestamp": "2024-07-21 14:30:15 UTC",
  "application": "Agrovesto Backend API",
  "version": "1.0.0",
  "uptime": "2d 5h 30m 45s",
  "checks": [
    {
      "service": "Database",
      "status": "Healthy",
      "responseTime": "45ms"
    }
  ]
}
```

### 2. Detailed Health Check
**URL:** `GET /health/detailed`
**Purpose:** Comprehensive health report with system metrics
**Response:** Detailed health information including system resources

### 3. Database Health
**URL:** `GET /health/database`
**Purpose:** Database connectivity and SQL Server status
**Response:** Database-specific health information

### 4. External Services Health
**URL:** `GET /health/external`
**Purpose:** Third-party service availability (IdentityPass, Termii SMS, SendGrid, etc.)
**Response:** External service status (always returns 200 to prevent cascading failures)

### 5. API Endpoints Health
**URL:** `GET /health/endpoints`
**Purpose:** Tests your application's API endpoints accessibility
**Response:** Status of API controllers and endpoints

### 6. Application Components Health
**URL:** `GET /health/components`
**Purpose:** Dependency injection services and business logic components
**Response:** Application component availability

### 7. Critical Systems Health
**URL:** `GET /health/critical`
**Purpose:** Only critical systems (database, core components)
**Response:** Critical system status

### 8. Quick Status
**URL:** `GET /health/status`
**Purpose:** Simple alive check for load balancers
**Response:** Minimal response indicating application is running

### 9. Visual Dashboard
**URL:** `GET /health.html`
**Purpose:** Web-based health monitoring dashboard
**Response:** Interactive HTML page with real-time health status

## Monitored Components

### 1. Database & Storage
- **Entity Framework DbContext**: Application database connectivity
- **SQL Server Direct Connection**: Raw database connection test
- **AWS S3 Storage**: File storage bucket accessibility
- **Tags**: `database`, `critical`, `storage`

### 2. External API Services

#### Identity Pass API (KYC Service)
- **Service**: Prembly Identity Pass for identity verification
- **Endpoint**: Uses `IdentityPass:preembleyurl` configuration
- **Check**: API connectivity and response validation

#### Termii SMS API
- **Service**: SMS notification service
- **Endpoint**: Balance check endpoint for API validation
- **Configuration**: Uses `TermiSMS:livekey`

#### Country State City API
- **Service**: Location and geographical data
- **Endpoint**: Countries endpoint for connectivity test
- **Configuration**: Uses `Location:apiUrl` and `Location:apiKey`

#### SendGrid Email API
- **Service**: Email delivery service
- **Endpoint**: User profile endpoint for authentication test
- **Configuration**: Uses `SendGrid:key`

### 3. Application API Endpoints
- **Authentication API**: `/api/auth/*` endpoints
- **User Management**: `/api/user/*` endpoints  
- **Business API**: `/api/business/*` endpoints
- **Farm Management**: `/api/farm/*` endpoints
- **Location Services**: `/api/location/*` endpoints
- **Swagger Documentation**: API documentation accessibility

### 4. Application Components
- **Dependency Injection Services**: Core application services
- **Business Managers**: Business logic components
- **Repository Classes**: Data access layer
- **Authentication Repository**: User authentication services
- **Wallet Manager**: Financial transaction services

## Health Check Responses

### Healthy Response Example
```json
{
  "status": "Healthy",
  "timestamp": "2024-07-21T13:59:10.123Z",
  "checks": [
    {
      "name": "Database",
      "responseTime": 45.2,
      "status": "Healthy",
      "description": "Database connection successful"
    },
    {
      "name": "AWS S3",
      "responseTime": 120.5,
      "status": "Healthy",
      "description": "S3 bucket accessible"
    }
  ]
}
```

### Unhealthy Response Example
```json
{
  "status": "Unhealthy",
  "timestamp": "2024-07-21T13:59:10.123Z",
  "checks": [
    {
      "name": "Database",
      "responseTime": 5000.0,
      "status": "Unhealthy",
      "description": "Database connection failed",
      "exception": "Cannot open database connection"
    }
  ]
}
```

## Status Levels

1. **Healthy**: All systems operational
2. **Degraded**: System is operational but some non-critical services may be unavailable
3. **Unhealthy**: Critical systems are down or unavailable

## Implementation Details

### Architecture Overview

#### Health Check Controller
- **Location**: `/Controllers/HealthController.cs`
- **Routes**: Direct route mapping (e.g., `/health`, `/health/detailed`)
- **Features**: Comprehensive error handling, detailed logging, system metrics

#### External Service Health Checks
- **Location**: `/HealthChecks/ExternalServiceHealthChecks.cs`
- **Classes**: 
  - `IdentityPassHealthCheck`
  - `TermiiSmsHealthCheck` 
  - `CountryStateCityApiHealthCheck`
  - `SendGridHealthCheck`

#### API Endpoint Health Checks
- **Location**: `/HealthChecks/ApiEndpointHealthChecks.cs`
- **Classes**:
  - `ApiEndpointHealthCheck`: Tests API endpoint accessibility
  - `ApplicationComponentHealthCheck`: Validates DI container services

#### Visual Dashboard
- **Location**: `/wwwroot/health.html`
- **Features**: Real-time updates, responsive design, quick links to all endpoints

### Key Features

#### 1. Web-First Design
- All endpoints return JSON suitable for both programmatic and human consumption
- Clean, readable responses with timestamps and detailed information
- HTTP status codes properly set (200 for healthy, 503 for unhealthy)

#### 2. Graceful Degradation
- External services return "Checked" status to prevent cascading failures
- Individual component failures don't crash the entire health check system
- Detailed error messages for debugging

#### 3. Performance Optimized
- Lightweight health queries (`SELECT 1` for database)
- Reasonable timeouts to prevent hanging
- Minimal resource usage for frequent checks

#### 4. EC2 & Load Balancer Ready
- `/health/status` endpoint perfect for ALB health checks
- Nginx-friendly response formats
- No authentication required for basic health endpoints

#### 5. Developer Experience
- Visual dashboard for easy monitoring
- Detailed error messages and descriptions
- System metrics included (uptime, memory usage, etc.)

## Usage Examples

### Web Browser Access
Simply visit these URLs in your browser:
- `https://agrovesto.com/health` - Basic health status
- `https://agrovesto.com/health.html` - Visual dashboard
- `https://agrovesto.com/health/detailed` - Complete health report

### Command Line Testing
```bash
# Basic health check
curl https://agrovesto.com/health

# Check database connectivity
curl https://agrovesto.com/health/database

# Check API endpoints
curl https://agrovesto.com/health/endpoints

# Check external services
curl https://agrovesto.com/health/external

# Quick status for load balancers
curl https://agrovesto.com/health/status
```

### Load Balancer Configuration (AWS ALB)
```yaml
# Target Group Health Check Settings
HealthCheckPath: /health/status
HealthCheckIntervalSeconds: 30
HealthCheckTimeoutSeconds: 5
HealthyThresholdCount: 2
UnhealthyThresholdCount: 3
```

### Nginx Configuration
Your existing Nginx configuration already includes:
```nginx
# Health check endpoint for load balancer
location /health {
    proxy_pass         http://127.0.0.1:5007/health;
    proxy_http_version 1.1;
    proxy_set_header   Host $host;
    proxy_connect_timeout 5s;
    proxy_send_timeout 5s;
    proxy_read_timeout 5s;
}
```

### Monitoring Integration
The health check endpoints can be integrated with various monitoring tools:

#### AWS CloudWatch
```bash
# Create custom metric from health check
aws cloudwatch put-metric-data \
  --namespace "Agrovesto/API" \
  --metric-data MetricName=HealthStatus,Value=1,Unit=Count
```

#### Uptime Monitoring Services
- **Pingdom**: Monitor `https://agrovesto.com/health/status`
- **UptimeRobot**: Use `/health/status` endpoint
- **StatusCake**: Monitor multiple endpoints for comprehensive coverage

#### Custom Monitoring Scripts
```bash
#!/bin/bash
# Simple monitoring script
HEALTH_URL="https://agrovesto.com/health"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $STATUS -eq 200 ]; then
    echo "✅ API is healthy"
else
    echo "❌ API is unhealthy (HTTP $STATUS)"
    # Send alert notification
fi
```

## Configuration Requirements

Ensure the following configuration sections are present in your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-database-connection-string"
  },
  "IdentityPass": {
    "preembleyurl": "https://api.prembly.com"
  },
  "TermiSMS": {
    "livekey": "your-termii-api-key"
  },
  "Location": {
    "apiUrl": "https://api.countrystatecity.in/v1",
    "apiKey": "your-csc-api-key"
  },
  "SendGrid": {
    "key": "your-sendgrid-api-key"
  },
  "AWS": {
    "BucketName": "your-s3-bucket",
    "AccessKey": "your-aws-access-key",
    "SecretKey": "your-aws-secret-key",
    "Region": "us-east-2"
  }
}
```

## Security Considerations

1. Health check endpoints are publicly accessible by default
2. Detailed endpoints should be secured or rate-limited in production
3. Sensitive configuration details are not exposed in health check responses
4. External API keys are validated but not returned in responses

## Testing Your Health Checks

### Local Development Testing
```bash
# Test all endpoints locally
curl http://localhost:5007/health
curl http://localhost:5007/health/detailed
curl http://localhost:5007/health/database
curl http://localhost:5007/health/external
curl http://localhost:5007/health/endpoints
curl http://localhost:5007/health/components
curl http://localhost:5007/health/critical
curl http://localhost:5007/health/status
```

### Production Testing
```bash
# Replace with your actual domain
DOMAIN="https://agrovesto.com"

# Test basic health
curl $DOMAIN/health | jq '.'

# Test database connectivity
curl $DOMAIN/health/database | jq '.databases'

# Test API endpoints
curl $DOMAIN/health/endpoints | jq '.summary'

# Quick status check
curl $DOMAIN/health/status
```

### Visual Testing
1. Open `https://agrovesto.com/health.html` in your browser
2. Click "🔄 Refresh Status" to update
3. Use the quick links to test individual endpoints
4. Monitor the auto-refresh every 30 seconds

### Troubleshooting
If health checks fail:
1. Check application logs: `journalctl -u Agrovesto.Backend.API -f`
2. Verify database connectivity
3. Test external service configurations
4. Check Nginx proxy settings
5. Validate SSL certificates if using HTTPS

The health check system provides detailed error messages to help identify and resolve issues quickly.
