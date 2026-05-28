import { useAuthStore } from '@/stores/auth'

type RequestOptions = RequestInit & {
  skipAuth?: boolean
}

function toCamelCaseKey(key: string): string {
  if (!key) {
    return key
  }

  return key.charAt(0).toLowerCase() + key.slice(1)
}

function normalizeJsonKeys<T>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => normalizeJsonKeys(item)) as T
  }

  if (value && typeof value === 'object' && Object.getPrototypeOf(value) === Object.prototype) {
    const normalized: Record<string, unknown> = {}

    for (const [key, entry] of Object.entries(value)) {
      normalized[toCamelCaseKey(key)] = normalizeJsonKeys(entry)
    }

    return normalized as T
  }

  return value
}

async function request<T>(url: string, options: RequestOptions = {}): Promise<T> {
  const authStore = useAuthStore()
  const headers = new Headers(options.headers ?? {})

  if (!options.skipAuth && authStore.token) {
    headers.set('Authorization', `Bearer ${authStore.token}`)
  }

  if (!(options.body instanceof FormData) && options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(url, { ...options, headers })

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`
    try {
      const data = await response.json()
      message = data.message ?? data.error ?? message
    } catch {
      // ignored
    }

    throw new Error(message)
  }

  if (response.headers.get('content-type')?.includes('application/json')) {
    const data = await response.json()
    return normalizeJsonKeys<T>(data)
  }

  return response.text() as T
}

export const http = {
  get<T>(url: string) {
    return request<T>(url)
  },
  post<T>(url: string, body?: unknown, options: RequestOptions = {}) {
    return request<T>(url, { ...options, method: 'POST', body: body instanceof FormData ? body : JSON.stringify(body) })
  },
  put<T>(url: string, body?: unknown) {
    return request<T>(url, { method: 'PUT', body: JSON.stringify(body) })
  },
  patch<T>(url: string, body?: unknown) {
    return request<T>(url, { method: 'PATCH', body: JSON.stringify(body) })
  },
}
