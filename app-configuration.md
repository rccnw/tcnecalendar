# TCNE Calendar Application Configuration

## General Configuration Overview
The TCNE Calendar application, designed for 'The Crow's Nest Escape', is hosted on Microsoft Azure and utilizes a variety of Azure resources. It's structured into several components including Azure functions, a web app, and shared resources.

## JSON Configuration Files
1. **`tcne-webapp-settings.json` and `tcne-fncapp-settings.json`**:
   - Contains settings for the web app and Azure functions app.
   - Includes CheckFront API endpoints, storage account information, and Application Insights connection strings.
   - These files are not part of the source code, but rather documentation only in case the Azure resources for the web app and function app need to be recreated or migrated. The are saved at the root of the git folder and appear in Visual Studio in a virtual folder called 'Documentation'

2. **`appsettings.json`**:
   - Located in the `TcneCalendar` web app.
   - Manages logging levels.

3. **`local.settings.json`**:
   - Part of the `CheckFrontAzureFunction`.
   - Contains similar settings to the web app and function app settings, particularly focused on Azure functions.

## Source Code Configuration
- **Azure Storage (`AzureStorage.cs`)**:
  - Manages storage of appointment data from CheckFront API.

- **CheckFront API Interaction (`CheckFrontApi.cs`)**:
  - Handles communication with the CheckFront API.

- **Web App and Azure Functions (`Program.cs` files)**:
  - Define startup configurations for the web app and Azure functions.

- **HTTP Helper Functions (`HttpHelperFunction.cs`)**:
  - Offloads long-running tasks from the main Azure function.

## Additional Configurations
- **Azure Resources (`azure-resources.txt`)**:
  - Details the Azure infrastructure used by the application.

## Application Behavior
- Periodically synchronizes with CheckFront.
- Allows users to view studio schedules in various formats.

## Security and Access
- Includes HTTP Basic Authentication credentials for API security.
- Configures `X-Frame-Options ALLOW-FROM` header for clickjacking protection.

## Performance and Monitoring
- Uses Application Insights for monitoring.
- Sets logging levels and diagnostics for performance tracking.

---

This setup ensures that the TCNE Calendar application provides quick access to studio availability and maintains synchronization with the CheckFront booking system.
