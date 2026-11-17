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
    const validationDetails = errorPayload.errors
      ? Object.values(errorPayload.errors).flat().join(' ')
      : null;

    throw new ApiError({
      code: errorPayload.code || 'HTTP_ERROR',
      title: errorPayload.title || 'Request Failed',
      detail:
        errorPayload.detail ||
        validationDetails ||
        errorPayload.title ||
        `Server responded with status ${response.status}.`,
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

async function request(url, options) {
  try {
    const response = await fetch(url, options);
    return parseResponse(response);
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    throw new ApiError({
      code: 'NETWORK_ERROR',
      title: 'Unable to reach the API',
      detail: 'The backend could not be reached. Check that the ASP.NET Core API is running and try again.',
      status: 0,
      retryable: true
    });
  }
}

export async function listVehicles(page = 1, pageSize = 12) {
  return request(
    `${API_BASE}/api/vehicles?page=${encodeURIComponent(page)}&pageSize=${encodeURIComponent(pageSize)}`,
    {
      headers: { Accept: 'application/json' }
    }
  );
}

export async function getVehicle(id) {
  return request(`${API_BASE}/api/vehicles/${encodeURIComponent(id)}`, {
    headers: { Accept: 'application/json' }
  });
}

export async function createVehicle(payload) {
  return request(`${API_BASE}/api/vehicles`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json'
    },
    body: JSON.stringify(payload)
  });
}

export async function replaceImage(vehicleId, imageUrl) {
  return request(`${API_BASE}/api/vehicles/${encodeURIComponent(vehicleId)}/image`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json'
    },
    body: JSON.stringify({ imageUrl })
  });
}

export async function importVehiclesCsv(file) {
  const formData = new FormData();
  formData.append('file', file);

  return request(`${API_BASE}/api/vehicles/import`, {
    method: 'POST',
    headers: {
      Accept: 'application/json'
    },
    body: formData
  });
}

export function resolveInternalImageUrl(imageUrl) {
  if (!imageUrl) return null;
  return `${API_BASE}${imageUrl}`;
}
