# Health Check Implementation for Agrovesto Backend API

This document describes the comprehensive health check system implemented for the Agrovesto Backend API.

## Overview

The health check system monitors the health and availability of the API and its dependencies, including:
- Database connectivity
- External service APIs
- AWS S3 storage
- Application readiness and liveness

## Endpoints

### 1. Basic Health Check
**URL:** `GET /health`
**Purpose:** Overall application health status
**Response:** Simple health status with timestamp

### 2. Detailed Health Check
**URL:** `GET /api/health/detailed`
**Purpose:** Detailed health information for all monitored services
**Response:** Comprehensive health report with individual service status, response times, and error details

### 3. Readiness Probe
**URL:** `GET /health/ready`
**Purpose:** Kubernetes/Docker readiness probe - checks if the application is ready to serve traffic
**Response:** JSON with status of critical services needed for the application to function

### 4. Liveness Probe
**URL:** `GET /health/live` or `GET /api/health/live`
**Purpose:** Kubernetes/Docker liveness probe - checks if the application is alive
**Response:** Simple alive status

## Monitored Services

### 1. Database Health Checks
- **Entity Framework DbContext Check**: Verifies that the application can connect to the database via Entity Framework
- **SQL Server Direct Check**: Direct connection to SQL Server database using connection string
- **Tags**: `ready`, `live`

### 2. External API Health Checks

#### Identity Pass API
- **Service**: Prembly Identity Pass service for KYC/verification
- **Check**: Validates API connectivity and response
- **Configuration**: Uses `IdentityPass:preembleyurl` from appsettings
- **Tags**: `ready`

#### Termii SMS API
- **Service**: SMS service provider for notifications
- **Check**: Validates API connectivity using balance endpoint
- **Configuration**: Uses `TermiSMS:livekey` and balance endpoint
- **Tags**: `ready`

#### Country State City API
- **Service**: Location service for geographical data
- **Check**: Validates API connectivity and authentication
- **Configuration**: Uses `Location:apiUrl` and `Location:apiKey`
- **Tags**: `ready`

#### SendGrid Email API
- **Service**: Email service provider
- **Check**: Validates API connectivity and authentication using profile endpoint
- **Configuration**: Uses `SendGrid:key`
- **Tags**: `ready`

### 3. AWS S3 Storage
- **Service**: AWS S3 bucket for file storage
- **Check**: Validates S3 bucket accessibility
- **Configuration**: Uses AWS configuration from appsettings
- **Tags**: `ready`

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

### Custom Health Check Classes
Custom health check classes are implemented in `/HealthChecks/ExternalServiceHealthChecks.cs`:
- `IdentityPassHealthCheck`
- `TermiiSmsHealthCheck`
- `CountryStateCityApiHealthCheck`
- `SendGridHealthCheck`

### Health Check Controller
A dedicated `HealthController` provides additional endpoints with detailed health information and custom response formatting.

### Configuration
Health checks are configured in `Program.cs` with appropriate tags for different probe types.

## Usage in Production

### Docker Health Checks
Add to your Dockerfile:
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1
```

### Kubernetes Probes
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
  
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

### Monitoring Integration
The detailed endpoint (`/api/health/detailed`) can be used with monitoring tools like:
- Prometheus with custom health check exporter
- Application Insights
- New Relic
- Custom monitoring dashboards

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

## Testing

To test the health checks:

1. **Basic Health**: `curl http://localhost:5000/health`
2. **Detailed Health**: `curl http://localhost:5000/api/health/detailed`
3. **Readiness**: `curl http://localhost:5000/health/ready`
4. **Liveness**: `curl http://localhost:5000/health/live`

The health check system will automatically detect and report issues with any of the monitored services.
