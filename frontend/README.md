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

- Authentication flow for `/login`, `/register`, and protected `/dashboard`.
- Demo token storage in `localStorage`.
- Auth context for current user state and logout.
- Friendly loading and error states for auth requests.
- Shared Axios HTTP client configured from `VITE_API_BASE_URL`.
- Typed API modules prepared for the later task slice.
