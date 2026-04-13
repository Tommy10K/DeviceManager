import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetailsLike {
  title?: string;
  detail?: string;
  message?: string;
}

export function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallbackMessage;
  }

  if (typeof error.error === 'string') {
    const text = error.error.trim();
    if (text.length === 0) {
      return fallbackMessage;
    }

    const parsed = tryParseProblemDetails(text);
    if (parsed) {
      return parsed;
    }

    return text;
  }

  if (error.error && typeof error.error === 'object') {
    const problem = error.error as ProblemDetailsLike;
    if (typeof problem.detail === 'string' && problem.detail.trim().length > 0) {
      return problem.detail.trim();
    }

    if (typeof problem.message === 'string' && problem.message.trim().length > 0) {
      return problem.message.trim();
    }

    if (typeof problem.title === 'string' && problem.title.trim().length > 0) {
      return problem.title.trim();
    }
  }

  if (error.status === 0) {
    return 'Could not reach the API server. Check that backend is running.';
  }

  return fallbackMessage;
}

function tryParseProblemDetails(value: string): string | null {
  try {
    const parsed = JSON.parse(value) as ProblemDetailsLike;
    if (typeof parsed.detail === 'string' && parsed.detail.trim().length > 0) {
      return parsed.detail.trim();
    }

    if (typeof parsed.message === 'string' && parsed.message.trim().length > 0) {
      return parsed.message.trim();
    }

    if (typeof parsed.title === 'string' && parsed.title.trim().length > 0) {
      return parsed.title.trim();
    }
  } catch {
    return null;
  }

  return null;
}
