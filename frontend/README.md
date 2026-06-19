# TaskBoard Frontend

React and TypeScript frontend for TaskBoard.

## Setup

Install dependencies:

```powershell
npm install
```

Create a local environment file when you need to override defaults:

```powershell
Copy-Item .env.example .env.local
```

The default API base URL is `http://localhost:5141`.

## Development

Start the Vite dev server:

```powershell
npm run dev
```

Build the frontend:

```powershell
npm run build
```

## Current Scope

- Basic routing for `/login`, `/register`, and `/dashboard`.
- Empty page shells for authentication and dashboard screens.
- Shared Axios HTTP client configured from `VITE_API_BASE_URL`.
- Typed API modules prepared for later auth and task slices.
