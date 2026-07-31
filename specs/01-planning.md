# Project Planning — Kaitiaki Quest

## 1. Project Background & Objectives

**Kaitiaki Quest** is a gamified web application developed for the Microsoft Student Accelerator (MSA) 2026 Phase 2 — Software Stream assessment. The application encourages environmental conservation through gamification, allowing users to complete eco-friendly missions, earn experience points (XP), unlock badges, and collaborate with teams.

**Core Objectives:**
- Demonstrate proficiency in full-stack development with React + TypeScript and .NET 10
- Integrate gamification elements (points, badges, streaks, leaderboards, progress tracking)
- Implement at least three advanced features from the required list
- Deliver a responsive, visually appealing user interface
- Ensure comprehensive test coverage for both frontend and backend
- **Strategically leverage AI tools (ChatGPT, Claude, GitHub Copilot) to accelerate architectural design, code generation, test coverage, and documentation, while critically reviewing all AI-generated outputs**

## 2. Advanced Features Selection (MSA Requirement)

This application implements the following three advanced features as required by the MSA 2026 Phase 2 specification:

| # | Advanced Feature | Implementation Summary |
| :--- | :--- | :--- |
| 1 | **WebSockets (SignalR)** | Real-time team XP updates using `TeamHub` with group-based broadcasting |
| 2 | **Security Measures (JWT + RBAC)** | JWT authentication with Role-Based Access Control (Admin/User) |
| 3 | **Caching Strategy (IMemoryCache)** | Leaderboard caching with 5-minute expiration and automatic invalidation |



## 3. Core Feature List

| Module | Features | Priority |
| :--- | :--- | :--- |
| **Authentication** | User registration, login, JWT token management | High |
| **Mission Management** | View missions, filter by category/daily, accept missions | High |
| **User Missions** | Complete missions with evidence, abandon missions, view history | High |
| **Gamification** | XP calculation, streak tracking, badge unlocking, level system | High |
| **Team System** | Create teams, join via invite code, leave teams, real-time updates | High |
| **Leaderboards** | Personal leaderboard, team leaderboard | Medium |
| **Admin Panel** | Mission CRUD operations (Admin only) | Medium |
| **User Profile** | View stats, badges, level progress, logout | Medium |

## 4. Non-Functional Requirements

| Requirement | Target | Implementation |
| :--- | :--- | :--- |
| Performance | API response < 200ms | Caching strategy (IMemoryCache) |
| Security | JWT authentication + RBAC | ASP.NET Core Identity + JWT Bearer |
| Responsiveness | Desktop + Mobile | MUI responsive grid system |
| Real-time | < 500ms latency | SignalR WebSockets |
| Test Coverage | > 70% | xUnit + Moq (backend), Vitest (frontend) |
| Accessibility | WCAG 2.1 AA | MUI accessibility support |

## 5. Initial Technology Decisions

| Decision | Selected Option | Rationale |
| :--- | :--- | :--- |
| Frontend Framework | React + TypeScript | MSA requirement; type safety |
| UI Library | MUI (Material-UI) | Rich component library, theming support |
| State Management | Zustand | Lightweight, simple API, no boilerplate |
| Backend Framework | .NET 10 + ASP.NET Core | MSA requirement |
| ORM | Entity Framework Core | MSA requirement; productivity |
| Database | SQL Server (Azure SQL) | MSA requirement; EF Core integration |
| Authentication | JWT Bearer + Identity | Stateless, scalable, RBAC support |
| Real-time | SignalR | Native .NET WebSocket library |
| Testing (Backend) | xUnit + Moq | Standard .NET testing stack |
| Testing (Frontend) | Vitest + Testing Library | Fast, Vite-native testing |
| Deployment | Azure App Service + Static Web Apps | MSA ecosystem; free tier available |
| API Documentation | Scalar | MSA requirement (instead of Swagger) |

## 6. Development Timeline (4-Week Sprint Plan)

| Week | Focus / Phase | Key Tasks & Deliverables |
| :--- | :--- | :--- |
| **Week 1** | **Backend Foundations** | Scaffolding API, DB Context & SQL Server setup, Entities, EF Core Migrations, Core CRUD Controllers, and Scalar documentation configuration. |
| **Week 2** | **Frontend Setup & Core UI** | React + TypeScript project initialization, MUI theme setup, Zustand store integration, routing, and core page layouts (Dashboard, Missions, Profile). |
| **Week 3** | **Advanced Features & Gamification** | Implementation of JWT + RBAC security, SignalR real-time hubs, `IMemoryCache` for leaderboards, and the gamification engine (XP/streaks). |
| **Week 4** | **Testing, Deployment & Documentation** | Unit testing (xUnit & Vitest), Azure deployment, finalization of `/specs` AI usage logs, and recording the final presentation video (<6 mins). |


