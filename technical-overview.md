# TCNE Calendar Application Technical Overview
## (chatGPT generated using Custom GPT 'TCNE Calendar App Mentor)

## Introduction
The TCNE Calendar application is a hybrid web and cloud application designed for 'The Crow's Nest Escape' (TCNE), a business with two studio facilities available for rental: 'The Nest' and 'The Hideout'. This application uses the CheckFront API to obtain detailed information about existing reservations in both studios and displays this information on a public website.

## Architecture
- **Hosted On**: Microsoft Azure Cloud.
- **Key Components**: Function App, App Service (Web App), Storage Account, Application Insights, Metric Alert Rule, and Action Group.

### Azure Resources
- **Resource Group**: Logical container for all resources.
- **App Service Plan**: Supports the Function App and the Web App.
- **Function App**: Named `TcneFunctionApp`, handles backend logic.
- **App Service (Web App)**: Named `tcne`, hosts the front-end website.
- **Storage Account**: Used by both Function App and Web App.
- **Application Insights**: Monitoring and analytics solution.

## Source Code Structure
- **Root Folders**: CheckFrontAzureFunction, TcneCalendar, TcneShared.
- **Main Solution File**: TcneCalendar.sln.

### CheckFrontAzureFunction
- **Purpose**: Handles Azure Function-related operations.
- **Key Files**: 
  - CheckFrontFunction.cs (HTTP trigger for CheckFront API interactions)
  - TimerTriggerFunction.cs (Timer-based trigger for regular updates)
  - HttpHelperFunction.cs (Assists in offloading long-running tasks)
  - AzureStorage.cs (Handles Azure Storage operations)

### TcneCalendar
- **Purpose**: Frontend web application.
- **Key Files**: 
  - Program.cs (Entry point for the web app)
  - appsettings.json (Configuration settings)
  - Components (Razor components for UI)

### TcneShared
- **Purpose**: Shared code between different components.
- **Key Models**: 
  - AppointmentData.cs (Model for appointment data)
  - CheckFrontApi.cs (Service for interacting with CheckFront API)
  - WebHook.cs (Model for handling webhook data)

## Functionality
- **Main Features**: 
  - Displays studio availability (The Nest and The Hideout) on a public website.
  - Offers a view in a one-month or one-week format.
  - Syncs frequently with CheckFront for up-to-date information.

## Configuration
- **Config Files**: 
  - `local.settings.json` and `appsettings.json` contain configuration settings like API URLs and keys.
  - `host.json` configures Azure Function settings like timeout and logging levels.

## Security and Logging
- **Authentication**: Uses HTTP Basic Authentication for API calls.
- **Logging**: Utilizes Azure Application Insights for monitoring and analytics.

## Conclusion
The TCNE Calendar application is a specialized solution tailored for TCNE's needs, leveraging Azure's cloud capabilities and a modern web frontend to provide a user-friendly interface for studio availability checking.


