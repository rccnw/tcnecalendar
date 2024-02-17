# TCNE Calendar Application Comprehensive Report

## Overview
The TCNE (The Crow's Nest Escape) Calendar Application is a tool designed to display the availability of two rental studios, 'The Nest' and 'The Hideout'. It interfaces with the CheckFront reservation system to fetch booking details and displays this information on a public-facing website.

## Application Structure and Key Features
- **Hosted on Microsoft Azure Cloud**: Utilizes various Azure services for hosting and operations.
- **Components**:
  - **Azure Functions (`CheckFrontAzureFunction`)**: Manages interactions with the CheckFront API and updates Azure storage with booking details.
  - **Web App (`TcneCalendar`)**: Provides a user interface to display studio availability.
  - **Shared Resources (`TcneShared`)**: Includes shared models and utilities used by both Azure functions and the web app.

## Azure Functions
- **`CheckFrontFunction`**: Triggered by CheckFront webhooks to initiate data synchronization.
- **`HttpHelperFunction`**: Handles long-running tasks and updates Azure Blob Storage with new booking data.
- **`TimerTriggerFunction`**: Runs periodically to ensure data is up-to-date, independent of webhook triggers.

## Web Application
- Developed using Blazor framework, leveraging Syncfusion components for the UI.
- Implements various pages and layouts for displaying booking information.
- **`Program.cs`**: Entry point for the web application, configuring services and app behavior.

## Shared Models
- **`AppointmentData`**: Represents the appointment data structure.
- **`CheckFrontBookingDetailModel` & `CheckFrontBookingsModel`**: Define data models for CheckFront API responses.
- **`AzureStorage`**: Manages interactions with Azure Blob Storage.

## Configuration Files
- **`appsettings.json`**: Configures basic app settings and logging levels.
- **`local.settings.json`**: Holds local development settings, primarily for Azure Functions.
- **`tcne-webapp-settings.json` & `tcne-fncapp-settings.json`**: Include Azure web app and function app specific settings, respectively.

## Security and Access
- Implements HTTP Basic Authentication for API interactions.
- Configurations to mitigate clickjacking attacks and manage cross-origin resource sharing.

## Performance and Monitoring
- Utilizes Azure Application Insights for performance monitoring and diagnostics.
- Detailed logging setup for both Azure Functions and the web app.

## Azure Infrastructure
- **Resource Group `crowsnest`**: A logical container for all related Azure resources.
- **App Service Plans**: Declares compute and memory resources for the app services.
- **Storage Account `tcne`**: Central storage used by both the function app and the web app.
- **Application Insights Instances**: Separate instances for the web app and function app for monitoring.

## Application Behavior and Usability
- Provides a quick and easy way to view studio availability without accessing CheckFront.
- Supports viewing schedules in monthly or weekly formats.
- Not the primary source of truth but syncs frequently with CheckFront for up-to-date information.

---

This report aims to provide a detailed understanding of the TCNE Calendar application's structure, components, functionality, and configuration.


