export class ApiError extends Error {
  status: number;
  statusText: string;
  body?: unknown;

  constructor(status: number, statusText: string, body?: unknown) {
    // Extract error message from body — supports RFC 7807 ProblemDetails and legacy format
    let bodyMessage: string | null = null;
    if (body && typeof body === "object") {
      const b = body as Record<string, unknown>;
      // RFC 7807: { detail: "...", title: "..." }
      if ("detail" in b && b.detail) {
        bodyMessage = String(b.detail);
      } else if ("title" in b && b.title) {
        bodyMessage = String(b.title);
      }
      // Legacy: { error: "..." }
      else if ("error" in b) {
        bodyMessage = String(b.error);
      }
    }

    super(bodyMessage ?? `API Error ${status}: ${statusText}`);
    this.name = "ApiError";
    this.status = status;
    this.statusText = statusText;
    this.body = body;
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: unknown;
    try {
      body = await response.json();
    } catch {
      // no JSON body
    }
    throw new ApiError(response.status, response.statusText, body);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text);
}

export const api = {
  async get<T>(url: string): Promise<T> {
    const res = await fetch(url);
    return handleResponse<T>(res);
  },

  async post<T>(url: string, body?: unknown): Promise<T> {
    const options: RequestInit = { method: "POST" };
    if (body !== undefined && body !== null) {
      options.headers = { "Content-Type": "application/json" };
      options.body = JSON.stringify(body);
    }
    const res = await fetch(url, options);
    return handleResponse<T>(res);
  },

  async put<T>(url: string, body?: unknown): Promise<T> {
    const options: RequestInit = { method: "PUT" };
    if (body !== undefined && body !== null) {
      options.headers = { "Content-Type": "application/json" };
      options.body = JSON.stringify(body);
    }
    const res = await fetch(url, options);
    return handleResponse<T>(res);
  },

  async del(url: string): Promise<void> {
    const res = await fetch(url, { method: "DELETE" });
    return handleResponse<void>(res);
  },

  async upload<T>(url: string, formData: FormData): Promise<T> {
    const res = await fetch(url, { method: "POST", body: formData });
    return handleResponse<T>(res);
  },
};
