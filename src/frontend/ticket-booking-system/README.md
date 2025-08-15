# Angular Ticket Booking System Frontend

Modern Angular 18 frontend application for the Event Ticketing System, built with Material UI and containerized for cloud-native deployment.

## 🌟 Features

- ✅ **Modern Angular 18**: Standalone components with TypeScript strict mode
- ✅ **Material Design**: Complete Angular Material UI implementation
- ✅ **Event Management**: Browse events, view details, and book tickets
- ✅ **Responsive Design**: Mobile-first approach with Angular Flex Layout
- ✅ **API Integration**: HTTP client with comprehensive error handling
- ✅ **Production Ready**: Multi-stage Docker builds with Nginx optimization
- ✅ **SPA Routing**: Client-side routing with nginx fallback support

## 🏗️ Project Structure

```
src/
├── app/
│   ├── components/
│   │   ├── event-list/         # Main events listing component
│   │   └── event-detail/       # Individual event details
│   ├── services/
│   │   └── event.service.ts    # API integration service
│   ├── models/
│   │   └── event.model.ts      # TypeScript interfaces
│   └── app.component.ts        # Root application component
├── assets/                     # Static assets
└── environments/               # Environment configurations
```

## 🐳 Containerized Development (Recommended)

This application runs in a Docker container as part of the complete application stack.

### Quick Start:
```bash
# From project root directory
docker-compose up -d

# Access application
# Frontend: http://localhost:8080
# API: http://localhost:8080/api/v1/events
```

### Container Configuration:
- **Base Image**: Multi-stage build (Node.js 18 + Nginx Alpine)
- **Build Stage**: Angular production build with optimization
- **Serve Stage**: Nginx with custom configuration for SPA routing
- **Port**: Exposed on port 8080 via docker-compose
- **Health Check**: Nginx status endpoint monitoring

### Development Commands:
```bash
# Rebuild frontend container
docker-compose build frontend

# View frontend logs
docker-compose logs -f frontend

# Execute commands in container
docker exec -it ticket-booking-frontend sh

# Restart frontend service
docker-compose restart frontend
```

## 🔧 Local Development (Alternative)

For traditional local development without containers:

### Prerequisites:
- Node.js 18+ 
- Angular CLI (`npm install -g @angular/cli`)

### Setup:
```bash
# Install dependencies
npm install

# Start development server
ng serve

# Application will be available at http://localhost:4200
```

### Development Commands:
```bash
# Development server with hot reload
ng serve

# Build for production
ng build --configuration production

# Run unit tests
ng test

# Run linting
ng lint

# Generate new components
ng generate component component-name
ng generate service service-name
```

## 🌐 API Integration

The frontend integrates with the .NET Event Management API:

### Endpoints Used:
```
GET /api/v1/events           # Fetch all events
GET /api/v1/events/{id}      # Fetch specific event
POST /api/v1/events          # Create new event (admin)
PUT /api/v1/events/{id}      # Update event (admin)
DELETE /api/v1/events/{id}   # Delete event (admin)
```

### Configuration:
- **Development**: API calls proxied through nginx to `eventmanagement-api:5000`
- **Environment**: Configurable API base URL in `environments/environment.ts`
- **Error Handling**: Comprehensive error interceptors and user feedback

## 🎨 UI Components

Built with Angular Material for consistent design:

### Key Components:
- **EventListComponent**: Grid layout with search and filtering
- **EventDetailComponent**: Detailed event information with booking options
- **Material Modules**: Cards, Buttons, Forms, Date Pickers, Dialogs

### Styling:
- **Theme**: Custom Material theme with primary/accent colors
- **Typography**: Material typography scales
- **Responsive**: Angular Flex Layout for mobile-first design
- **Icons**: Material Design Icons

## 🚀 Production Build

The application uses multi-stage Docker builds for optimal production deployment:

### Build Process:
1. **Build Stage**: Node.js 18 with Angular CLI
   - `npm ci` for clean dependency installation
   - `ng build --configuration production` for optimized build
   - Tree-shaking, minification, and compression

2. **Serve Stage**: Nginx Alpine
   - Lightweight production server
   - Custom nginx configuration for SPA routing
   - Gzip compression and caching headers
   - API proxy configuration

### Performance Optimizations:
- ✅ **Bundle Optimization**: Tree-shaking and code splitting
- ✅ **Lazy Loading**: Route-based code splitting (ready for expansion)
- ✅ **OnPush Strategy**: Change detection optimization
- ✅ **Asset Optimization**: Image compression and WebP support
- ✅ **Caching**: Nginx cache headers for static assets

## 📱 Responsive Design

Mobile-first approach with Angular Material:

### Breakpoints:
- **Mobile**: < 600px - Single column layout
- **Tablet**: 600px - 960px - Two column grid
- **Desktop**: > 960px - Multi-column grid with sidebar

### Features:
- **Touch-friendly**: Material Design touch targets
- **Navigation**: Responsive navigation with sidenav for mobile
- **Forms**: Mobile-optimized form inputs and validation

## 🔍 Testing Strategy

### Unit Testing:
```bash
# Run tests with coverage
ng test --code-coverage

# Run tests in CI mode
ng test --watch=false --browsers=ChromeHeadless
```

### E2E Testing:
```bash
# Setup Playwright or Cypress for E2E testing
ng e2e
```

## 🛠️ Development Tools

### Recommended VS Code Extensions:
- Angular Language Service
- TypeScript Importer
- Material Icon Theme
- Prettier - Code formatter
- ESLint

### Code Quality:
- **ESLint**: TypeScript and Angular linting rules
- **Prettier**: Code formatting standards
- **TypeScript**: Strict mode enabled for type safety
- **Angular**: Latest style guide compliance

## 🚢 Deployment

### Container Deployment:
```bash
# Build production image
docker build -t ticket-booking-frontend .

# Run container
docker run -p 8080:80 ticket-booking-frontend
```

### Azure Deployment:
```bash
# Deploy to Azure Container Instances
az container create --resource-group TicketBookingRG \
  --name frontend --image ticket-booking-frontend \
  --ports 80 --dns-name-label ticket-booking-frontend
```

## 📚 Learning Resources

- [Angular Documentation](https://angular.dev)
- [Angular Material](https://material.angular.io)
- [TypeScript Handbook](https://www.typescriptlang.org/docs)
- [Docker Multi-stage Builds](https://docs.docker.com/develop/dev-best-practices/dockerfile_best-practices/#use-multi-stage-builds)

## 🤝 Contributing

Please follow the established patterns:
1. Use standalone components
2. Implement proper TypeScript typing
3. Follow Angular style guide
4. Add unit tests for new components
5. Update this README for significant changes

---

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 18.2.20 and enhanced with Material UI, containerization, and production-ready configurations.
