# 🌿 Kaitiaki Quest — Eco-Warrior Quest

## Project Link
<https://nice-pebble-0499bf500.7.azurestaticapps.net>

Account Details:
**User:** admin@kaitiaki.com 
**Password:** Admin123!
**User:** user@kaitiaki.com 
**Password:** User123!

## 📌 Project Overview

**Kaitiaki Quest** is a **gamified task management application** built around New Zealand's conservation spirit. Users earn experience points (XP), unlock badges, maintain streaks, and compete with friends by completing daily eco-friendly missions such as recycling, energy saving, walking, and tree planting.

> **Kaitiaki** is a Māori word meaning "guardian" or "protector," reflecting New Zealand's deep respect for the natural environment.

This project is built with a full-stack architecture that fully implements gamification mechanics, team collaboration, real-time notifications, and competitive leaderboards.

## 🎯 Theme Connection (Gamification)
This application strictly follows the **Gamification** definition outlined in the requirements, integrating game design elements into a non-game context to enhance user engagement and environmental awareness.

| Gamification Element | Implementation in This Application |
| :--- | :--- |
| **Points** | Complete missions to earn XP; higher XP leads to higher levels |
| **Badges** | Auto-unlock when specific XP thresholds are reached |
| **Streaks** | Consecutive daily mission completion; streak ≥ 7 days grants 1.5x XP bonus |
| **Leaderboards** | Personal + Team leaderboards to drive competition |
| **Progress Tracking** | Level progress bar, next badge progress, statistics dashboard |
| **Teams** | Team up with friends; team total XP updates in real-time |

## ✨ Key Features

### 1. Gamification Engine
- **Points System**: Base points + streak bonus (≥7 days = 1.5x multiplier) + daily mission bonus
- **Dynamic Levels**: 1 level per 100 XP
- **Auto Badge Awarding**: Automatically awarded when unlock conditions are met, no manual intervention required

### 2. Real-Time Team Collaboration (WebSocket / SignalR)
- When a teammate completes a mission, **team total XP updates in real-time** without page refresh
- Real-time notifications for team member join/leave events
- Powered by **SignalR** for bi-directional real-time communication

### 3. Security & Authorization (JWT + RBAC)
- **JWT Token** authentication for secure API access
- **RBAC (Role-Based Access Control)**: Regular users can only accept/complete missions; Admins can manage the mission library
- **Input validation + parameterized queries** to prevent SQL injection and XSS attacks

### 4. Performance Optimization (Caching)
- **IMemoryCache** caches leaderboard data (5-minute expiration)
- Cache is automatically invalidated after mission completion to ensure data consistency
- API response time reduced from 200ms to <10ms

### 5. Theme Switching
- Supports **Light/Dark mode** switching for different usage scenarios
- Implemented using the MUI theming system

## 📋 Advanced Features Checklist

This application implements the **3 required advanced features** from the requirements:

| # | Advanced Feature | Implementation | Justification |
| :--- | :--- | :--- | :--- |
| 1 | **WebSockets (SignalR)** | Real-time XP updates pushed to all online teammates when a mission is completed. Uses SignalR Hub with auto-reconnect support. | Real-time collaboration is essential for competitive gamification; users see team progress without refreshing the page. |
| 2 | **Security Measures (JWT + RBAC)** | ① JWT Token authentication (24-hour expiration)<br>② Role-Based Access Control (Admin / User); Admins can manage the mission library. | Data security is fundamental; follows New Zealand Privacy Act 2020 and OWASP Top 10 best practices. |
| 3 | **Caching Strategy (IMemoryCache)** | Leaderboard data cached for 5 minutes; API response time reduced from 200ms to <10ms. Cache auto-cleared after mission completion. | Leaderboards are high-frequency read endpoints; caching significantly reduces database load and improves user experience. |
| 4 | **State Management (Zustand)** | useAuthStore manages user authentication state with persistence to localStorage | User authentication state is persistent across sessions. |
| 5 | **Theme Switching** | MUI ThemeProvider with light/dark mode toggle, stored in localStorage. | User preference is essential for accessibility and comfort. |

## 💭 Self-Reflection

If I were to develop this project again, here are the key improvements I would make:

### **1.Adopt Test-Driven Development (TDD) Earlier** 
### Tests were written after the code in this project, which led to higher refactoring costs. TDD would ensure code quality from the start.

### **2.Frontend State Management Optimization** 
### Currently using Zustand for global state, but some page-level state (e.g., mission list filters) is managed locally. Next time, consider using React Query for server-state management to reduce redundant requests.

### **3. Contract-First API Development** 
### OpenAPI + Scalar was used for documentation, but the frontend TypeScript types were not fully auto-generated. Next time, use NSwag or OpenAPI Generator for automated type generation.

### **4. Better Error Boundaries** 
### Frontend error handling currently relies on try-catch; lacks global React Error Boundaries. Add React Error Boundary to capture rendering errors.

### **5. Environment Variable Management**
### Used .env files without differentiating development/production configurations. Add more environment configuration layers.

## 🛠️ Tech Stack

### Frontend
| Technology | Version | Purpose |
| :--- | :--- | :--- |
| React | 19.x | UI Framework |
| TypeScript | 5.x | Type Safety |
| MUI (Material-UI) | 9.x | UI Component Library |
| Zustand | 5.x | State Management |
| React Router | 7.x | Routing & Navigation |
| Axios | 1.x | HTTP Client |
| SignalR Client | 10.x | Real-Time Communication |
| Vite | 8.x | Build Tool |
| Vitest | 4.x | Unit Testing |

### Backend
| Technology | Version | Purpose |
| :--- | :--- | :--- |
| .NET | 10.0 | Web API Framework |
| ASP.NET Core | 10.0 | REST API + SignalR |
| Entity Framework Core | 10.0 | ORM Data Access |
| SQL Server | Azure SQL | Production Database |
| JWT Bearer | 10.0 | Authentication |
| Scalar | 2.x | API Documentation |
| xUnit + Moq | — | Unit Testing |

### Deployment
| Service | Platform | Purpose |
| :--- | :--- | :--- |
| Frontend | Azure Static Web Apps | Static website hosting |
| Backend | Azure App Service | API hosting |
| Database | Azure SQL | Data persistence |



## 🚀 Local Development Guide

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- SQL Server (or SQL Server LocalDB)

### Backend Setup
```bash
cd backend/KaitiakiQuest.API
dotnet restore
dotnet ef database update
dotnet run
```
**The backend will run at https://localhost:7225, with API docs available at https://localhost:7225/scalar/v1**
### Frontend Setup
```bash
cd frontend
npm install
npm run dev
```
**The frontend will run at http://localhost:5173**  

### Environment Variables
*Frontend*: Create a `.env` file in the `frontend` directory and add the following: VITE_API_BASE_URL=https://localhost:7225

*Backend*: Create a `appsettings.json` file in the `backend/KaitiakiQuest.API` directory and add the following:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KaitiakiQuestDB;..."
  },
  "Jwt": {
    "Key": "your-jwt-secret-key",
    "Issuer": "KaitiakiQuestAPI",
    "Audience": "KaitiakiQuestFrontend",
    "ExpiryMinutes": 1440
  },
    "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
```

## 🧪 Testing

### Backend Tests
```bash
cd backend/KaitiakiQuest.API.Test
dotnet test
```

**Coverage: Service Layer (100+ test cases) + Controller Layer (50+ test cases)**

### Frontend Tests
```bash
cd frontend
npm run test
```

**Coverage: State Management, API Client, Components, Pages (85+ test cases)**

> **"Kaitiaki"** —— Protecting Aotearoa, one mission at a time. 🌿
