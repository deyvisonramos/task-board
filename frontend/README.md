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

The root Docker Compose stack builds the frontend and serves the generated static assets through nginx on `http://localhost:5173`.

## Current Scope

- Authentication flow for `/login`, `/register`, and protected `/dashboard`.
- Dashboard task CRUD for list, modal create/edit, delete, due dates, and drag-and-drop status updates.
- Loading, empty, and error states for the task board.
- Demo token storage in `localStorage`.
- Auth context for current user state and logout.
- Friendly loading and error states for auth requests.
- Shared Axios HTTP client configured from `VITE_API_BASE_URL`.
- Typed task API module for the protected `/api/tasks` endpoints.
