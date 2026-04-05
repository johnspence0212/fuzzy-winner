// frontend/src/api/base/client.ts
import { Effect, ParseResult } from 'effect'

// Custom error type for better error handling
export class ApiError extends Error {
  constructor(message: string, public status?: number) {
    super(message)
    this.name = 'ApiError'
  }
}

// Dev: Vite proxies `/api` → backend (no CORS issues). Preview/build: set VITE_API_BASE or fall back to API on :5000.
const BASE_URL =
  (import.meta.env.VITE_API_BASE as string | undefined)?.replace(/\/$/, '') ??
  (import.meta.env.DEV ? '/api' : 'http://localhost:5000/api')
const DEFAULT_HEADERS = { 'Content-Type': 'application/json' }

// Effect-based HTTP client using native fetch (no Axios)
export const httpClient = {
  get: <T>(url: string): Effect.Effect<T, ApiError> =>
    Effect.tryPromise(async () => {
      console.log(`GET ${BASE_URL}${url}`)
      const response = await fetch(`${BASE_URL}${url}`, {
        method: 'GET',
        headers: DEFAULT_HEADERS,
      })
      if (!response.ok) {
        console.error(`❌ GET ${BASE_URL}${url} - ${response.status}`)
        throw new ApiError(`GET failed: ${response.statusText}`, response.status)
      }
      console.log(`✅ GET ${BASE_URL}${url} - ${response.status}`)
      return response.json()
    }),

  post: <T, B = unknown>(url: string, data: B): Effect.Effect<T, ApiError> =>
    Effect.tryPromise(async () => {
      console.log(`POST ${BASE_URL}${url}`)
      const response = await fetch(`${BASE_URL}${url}`, {
        method: 'POST',
        headers: DEFAULT_HEADERS,
        body: JSON.stringify(data),
      })
      if (!response.ok) {
        console.error(`❌ POST ${BASE_URL}${url} - ${response.status}`)
        throw new ApiError(`POST failed: ${response.statusText}`, response.status)
      }
      console.log(`✅ POST ${BASE_URL}${url} - ${response.status}`)
      return response.json()
    }),

  put: <B = unknown>(url: string, data: B): Effect.Effect<void, ApiError> =>
    Effect.tryPromise(async () => {
      console.log(`PUT ${BASE_URL}${url}`)
      const response = await fetch(`${BASE_URL}${url}`, {
        method: 'PUT',
        headers: DEFAULT_HEADERS,
        body: JSON.stringify(data),
      })
      if (!response.ok) {
        console.error(`❌ PUT ${BASE_URL}${url} - ${response.status}`)
        throw new ApiError(`PUT failed: ${response.statusText}`, response.status)
      }
      console.log(`✅ PUT ${BASE_URL}${url} - ${response.status}`)
    }),

  putJson: <T, B = unknown>(url: string, data: B): Effect.Effect<T, ApiError> =>
    Effect.tryPromise(async () => {
      const response = await fetch(`${BASE_URL}${url}`, {
        method: 'PUT',
        headers: DEFAULT_HEADERS,
        body: JSON.stringify(data),
      })
      if (!response.ok) {
        throw new ApiError(`PUT failed: ${response.statusText}`, response.status)
      }
      return response.json() as Promise<T>
    }),

  delete: (url: string): Effect.Effect<void, ApiError> =>
    Effect.tryPromise(async () => {
      console.log(`DELETE ${BASE_URL}${url}`)
      const response = await fetch(`${BASE_URL}${url}`, {
        method: 'DELETE',
        headers: DEFAULT_HEADERS,
      })
      if (!response.ok) {
        console.error(`❌ DELETE ${BASE_URL}${url} - ${response.status}`)
        throw new ApiError(`DELETE failed: ${response.statusText}`, response.status)
      }
      console.log(`✅ DELETE ${BASE_URL}${url} - ${response.status}`)
    }),
}

/** Runs an API Effect and surfaces fetch / schema errors with clear messages. */
export const runRequest = async <A, E>(effect: Effect.Effect<A, E>): Promise<A> => {
  try {
    return await Effect.runPromise(effect)
  } catch (e: unknown) {
    if (ParseResult.isParseError(e)) {
      throw new Error(
        `API response did not match expected shape: ${ParseResult.TreeFormatter.formatErrorSync(e)}`,
      )
    }
    if (e instanceof ApiError) {
      throw e
    }
    if (e instanceof TypeError) {
      throw new Error(
        `${e.message}. Is the API running? With the Vite dev server, use relative /api (proxy) or set VITE_API_BASE.`,
      )
    }
    if (e instanceof Error) {
      throw e
    }
    throw new Error(String(e))
  }
}
