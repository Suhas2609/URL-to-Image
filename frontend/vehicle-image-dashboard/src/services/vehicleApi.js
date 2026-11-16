const API_BASE = import.meta.env.VITE_API_BASE_URL || '';

export class ApiError extends Error {
  constructor({ code, title, detail, status, traceId, retryable, retryAfterSeconds }) {
    super(detail || title || 'Request failed');
    this.name = 'ApiError';
    this.code = code || 'INTERNAL_ERROR';
    this.title = title || 'Request Failed';
    this.detail = detail || 'An unexpected error occurred.';
    this.status = status || 500;
    this.traceId = traceId || null;
    this.retryable = Boolean(retryable);
    this.retryAfterSeconds = retryAfterSeconds ?? null;
  }
}

async function parseResponse(response) {
  if (response.ok) {
    return response.json();
  }

  let errorPayload = null;
  try {
    errorPayload = await response.json();
  } catch {
    // Fallback if non-JSON error response
  }

  if (errorPayload && typeof errorPayload === 'object') {
    throw new ApiError({
      code: errorPayload.code || 'HTTP_ERROR',
      title: errorPayload.title || 'Request Failed',
      detail: errorPayload.detail || `Server responded with status ${response.status}.`,
      status: errorPayload.status || response.status,
      traceId: errorPayload.traceId || null,
      retryable: errorPayload.retryable || false,
      retryAfterSeconds: errorPayload.retryAfterSeconds ?? null
    });
  }

  throw new ApiError({
    code: 'NETWORK_OR_SERVER_ERROR',
    title: 'Request Failed',
    detail: `HTTP ${response.status}: ${response.statusText || 'Unable to process request.'}`,
    status: response.status
  });
}

export async function listVehicles(page = 1, pageSize = 12) {
  const response = await fetch(
    `${API_BASE}/api/vehicles?page=${encodeURIComponent(page)}&pageSize=${encodeURIComponent(pageSize)}`,
    {
      headers: { Accept: 'application/json' }
    }
  );
  return parseResponse(response);
}

export async function getVehicle(id) {
  const response = await fetch(`${API_BASE}/api/vehicles/${encodeURIComponent(id)}`, {
    headers: { Accept: 'application/json' }
  });
  return parseResponse(response);
}

export async function createVehicle(payload) {
  const response = await fetch(`${API_BASE}/api/vehicles`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json'
    },
    body: JSON.stringify(payload)
  });
  return parseResponse(response);
}

export async function replaceImage(vehicleId, imageUrl) {
  const response = await fetch(`${API_BASE}/api/vehicles/${encodeURIComponent(vehicleId)}/image`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json'
    },
    body: JSON.stringify({ imageUrl })
  });
  return parseResponse(response);
}

export function resolveInternalImageUrl(imageUrl) {
  if (!imageUrl) return null;
  return `${API_BASE}${imageUrl}`;
}
