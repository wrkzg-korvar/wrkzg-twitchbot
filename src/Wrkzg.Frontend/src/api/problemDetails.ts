export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

/**
 * Extracts a user-friendly error message from an API error response.
 * Handles both RFC 7807 ProblemDetails and legacy error formats.
 */
export async function extractErrorMessage(response: Response): Promise<string> {
  try {
    const body = await response.json();

    // RFC 7807 ProblemDetails
    if (body.title && body.status) {
      if (body.errors) {
        // Validation errors — join all field errors
        const fieldErrors = Object.values(body.errors as Record<string, string[]>)
          .flat()
          .join('. ');
        return fieldErrors || body.detail || body.title;
      }
      return body.detail || body.title;
    }

    // Legacy format: { error: "..." } or { message: "..." }
    if (body.error) return body.error;
    if (body.message) return body.message;

    return `Error ${response.status}: ${response.statusText}`;
  } catch {
    return `Error ${response.status}: ${response.statusText}`;
  }
}

/**
 * Parses a full ProblemDetails object from a response.
 */
export async function parseProblemDetails(response: Response): Promise<ProblemDetails | null> {
  try {
    const body = await response.json();
    if (body.title && body.status) return body as ProblemDetails;
    return null;
  } catch {
    return null;
  }
}
