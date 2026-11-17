# Complete Frontend Source Code Appendix (`feature/csv-bulk-import`)

This appendix contains the complete, verbatim, unabbreviated source code of all **14 source and configuration files** in `frontend/vehicle-image-dashboard/` on branch `feature/csv-bulk-import` (`49e9d84`).

## Table of Contents

1. `frontend/vehicle-image-dashboard/package.json` — Frontend Package Manifest
2. `frontend/vehicle-image-dashboard/vite.config.js` — Vite Dev Server & API Proxy Config
3. `frontend/vehicle-image-dashboard/index.html` — HTML Document Shell
4. `frontend/vehicle-image-dashboard/src/main.jsx` — React Entry Point
5. `frontend/vehicle-image-dashboard/src/App.jsx` — Application Shell & Header
6. `frontend/vehicle-image-dashboard/src/services/vehicleApi.js` — API Service Layer & Error Normalizer (Modified on feature/csv-bulk-import)
7. `frontend/vehicle-image-dashboard/src/pages/DashboardPage.jsx` — Dashboard Page & Catalog Coordinator (Modified on feature/csv-bulk-import)
8. `frontend/vehicle-image-dashboard/src/components/VehicleForm.jsx` — Single Vehicle Ingestion Form (Modified on feature/csv-bulk-import)
9. `frontend/vehicle-image-dashboard/src/components/BulkImportForm.jsx` — CSV Bulk Import Panel (New on feature/csv-bulk-import)
10. `frontend/vehicle-image-dashboard/src/components/VehicleCard.jsx` — Vehicle Catalog Card
11. `frontend/vehicle-image-dashboard/src/components/ImageReplaceForm.jsx` — Inline Image Replacement Form
12. `frontend/vehicle-image-dashboard/src/components/StatusBadge.jsx` — Status Badge Component
13. `frontend/vehicle-image-dashboard/src/components/ImagePlaceholder.jsx` — Fallback Image Placeholder
14. `frontend/vehicle-image-dashboard/src/index.css` — Dashboard Design System & Layout CSS (Modified on feature/csv-bulk-import)

---

## 1. Frontend Package Manifest

**File:** `frontend/vehicle-image-dashboard/package.json`  
**Purpose:** Defines React 19, ReactDOM 19, Vite 6.2, and dev/build/preview npm scripts.

```json
{
  "name": "vehicle-image-dashboard",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^4.3.4",
    "vite": "^6.2.0"
  }
}
```

## 2. Vite Dev Server & API Proxy Config

**File:** `frontend/vehicle-image-dashboard/vite.config.js`  
**Purpose:** Runs Vite on port 5173 and proxies /api and /health traffic to VITE_API_TARGET (default http://localhost:5080) during development.

```javascript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const apiTarget = process.env.VITE_API_TARGET || 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true
      },
      '/health': {
        target: apiTarget,
        changeOrigin: true
      }
    }
  }
});
```

## 3. HTML Document Shell

**File:** `frontend/vehicle-image-dashboard/index.html`  
**Purpose:** Defines document metadata, loads Inter and JetBrains Mono fonts, and mounts /src/main.jsx into #root.

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta
      name="description"
      content="Multi-Source Vehicle Image Retrieval, Validation, Internal Storage and Delivery System"
    />
    <title>VehicleImageSystem — Multi-Source Image Ingestion & Delivery</title>
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link
      href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap"
      rel="stylesheet"
    />
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.jsx"></script>
  </body>
</html>
```

## 4. React Entry Point

**File:** `frontend/vehicle-image-dashboard/src/main.jsx`  
**Purpose:** Mounts App inside React.StrictMode and imports index.css.

```jsx
import React from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import './index.css';

createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

## 5. Application Shell & Header

**File:** `frontend/vehicle-image-dashboard/src/App.jsx`  
**Purpose:** Renders the sticky header with branding and active security/pipeline chips, wrapping DashboardPage.

```jsx
import React from 'react';
import DashboardPage from './pages/DashboardPage';

export default function App() {
  return (
    <div>
      <header className="app-header">
        <div className="app-header-inner">
          <div className="brand-group">
            <h1 className="brand-title">VehicleImageSystem</h1>
            <p className="brand-subtitle">
              Multi-Source Vehicle Image Retrieval, Validation, Internal Storage &amp; Delivery
            </p>
          </div>
          <div className="security-summary-bar" aria-label="Active pipeline protections">
            <span className="security-chip">URL Policy Validation</span>
            <span className="security-chip">20 MB Stream Cap</span>
            <span className="security-chip">Magic-Byte &amp; Pixel Decode</span>
            <span className="security-chip">Immutable Internal Delivery</span>
          </div>
        </div>
      </header>
      <DashboardPage />
    </div>
  );
}
```

## 6. API Service Layer & Error Normalizer (Modified on feature/csv-bulk-import)

**File:** `frontend/vehicle-image-dashboard/src/services/vehicleApi.js`  
**Purpose:** Central fetch wrapper normalizing RFC 7807 JSON errors and network failures into ApiError, now including importVehiclesCsv(file) for multipart CSV uploads.

```javascript
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
```

## 7. Dashboard Page & Catalog Coordinator (Modified on feature/csv-bulk-import)

**File:** `frontend/vehicle-image-dashboard/src/pages/DashboardPage.jsx`  
**Purpose:** Manages vehicle pagination (PAGE_SIZE=12), loading/error banners, the left .sidebar-stack hosting VehicleForm and BulkImportForm, and the VehicleCard grid.

```jsx
import React, { useCallback, useEffect, useState } from 'react';
import VehicleForm from '../components/VehicleForm';
import BulkImportForm from '../components/BulkImportForm';
import VehicleCard from '../components/VehicleCard';
import { listVehicles } from '../services/vehicleApi';

const PAGE_SIZE = 12;

export default function DashboardPage() {
  const [vehicles, setVehicles] = useState([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  const fetchCatalog = useCallback(async (targetPage = page) => {
    setLoading(true);
    setLoadError(null);
    try {
      const data = await listVehicles(targetPage, PAGE_SIZE);
      setVehicles(data.items || []);
      setPage(data.page || targetPage);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      setLoadError(err.detail || err.message || 'Unable to load stored vehicle records.');
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    fetchCatalog(page);
  }, [page, fetchCatalog]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  function handleCatalogRefreshFromPageOne() {
    if (page !== 1) {
      setPage(1);
    } else {
      fetchCatalog(1);
    }
  }

  return (
    <main className="dashboard-container">
      <div className="sidebar-stack">
        <VehicleForm onVehicleCreated={handleCatalogRefreshFromPageOne} />
        <BulkImportForm onImportCompleted={handleCatalogRefreshFromPageOne} />
      </div>

      <section aria-labelledby="catalog-heading" aria-busy={loading}>
        <div className="catalog-header">
          <div className="catalog-title-group">
            <h2 id="catalog-heading">Internally Stored Vehicle Catalog</h2>
            <p>
              All images below are served strictly from <code>/api/images/&#123;id&#125;</code> with native lazy loading and zero supplier hotlinking.
            </p>
          </div>

          <button
            id="refresh-catalog-btn"
            type="button"
            className="btn-secondary"
            onClick={() => fetchCatalog(page)}
            disabled={loading}
          >
            {loading ? 'Refreshing...' : `Refresh (${totalCount})`}
          </button>
        </div>

        {loadError && (
          <div className="feedback-banner feedback-banner-error" role="alert">
            <div className="feedback-code-header">
              <span className="error-code-pill">CATALOG_LOAD_ERROR</span>
            </div>
            <p className="feedback-detail">{loadError}</p>
          </div>
        )}

        {loading && vehicles.length === 0 && !loadError ? (
          <div className="empty-catalog" role="status">
            <h3>Loading stored vehicles...</h3>
            <p>Retrieving the latest records from the backend.</p>
          </div>
        ) : !loading && vehicles.length === 0 && !loadError ? (
          <div className="empty-catalog">
            <h3>No vehicles ingested yet</h3>
            <p>
              Submit a supplier vehicle and image URL on the left (or click one of the quick verification presets) to retrieve, validate, and store an image.
            </p>
          </div>
        ) : (
          <div className="vehicle-grid">
            {vehicles.map((vehicle) => (
              <VehicleCard
                key={`${vehicle.id}-${vehicle.imageId}`}
                vehicle={vehicle}
                onUpdated={() => fetchCatalog(page)}
              />
            ))}
          </div>
        )}

        {totalCount > PAGE_SIZE && (
          <nav className="pagination-bar" aria-label="Vehicle catalog pagination">
            <span>
              Page <strong>{page}</strong> of <strong>{totalPages}</strong> ({totalCount} total vehicles)
            </span>
            <div className="replace-actions">
              <button
                id="prev-page-btn"
                type="button"
                className="btn-secondary"
                disabled={page <= 1 || loading}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Previous
              </button>
              <button
                id="next-page-btn"
                type="button"
                className="btn-secondary"
                disabled={page >= totalPages || loading}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                Next
              </button>
            </div>
          </nav>
        )}
      </section>
    </main>
  );
}
```

## 8. Single Vehicle Ingestion Form (Modified on feature/csv-bulk-import)

**File:** `frontend/vehicle-image-dashboard/src/components/VehicleForm.jsx`  
**Purpose:** Controlled form for individual URL submissions with 3 quick verification presets and structured success/rejection feedback.

```jsx
import React, { useState } from 'react';
import { createVehicle } from '../services/vehicleApi';

const DEMO_PRESETS = [
  {
    id: 'preset-vehicle-a',
    label: 'Vehicle A: Webpage URL',
    make: 'Porsche',
    model: '911 Carrera',
    variant: 'GTS Cabriolet',
    year: 2026,
    imageUrl: 'https://en.wikipedia.org/wiki/Porsche_911'
  },
  {
    id: 'preset-vehicle-b',
    label: 'Vehicle B: Direct Image',
    make: 'BMW',
    model: 'M3',
    variant: 'Competition xDrive',
    year: 2026,
    imageUrl: 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/39/2021_BMW_M3_Competition_Automatic_3.0_Front.jpg/960px-2021_BMW_M3_Competition_Automatic_3.0_Front.jpg'
  },
  {
    id: 'preset-vehicle-c',
    label: 'Vehicle C: Blocked Localhost URL',
    make: 'Audi',
    model: 'RS6 Avant',
    variant: 'Performance',
    year: 2026,
    imageUrl: 'http://localhost/car.jpg'
  }
];

export default function VehicleForm({ onVehicleCreated }) {
  const [make, setMake] = useState('');
  const [model, setModel] = useState('');
  const [variant, setVariant] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [imageUrl, setImageUrl] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState(null);

  function applyPreset(preset) {
    setMake(preset.make);
    setModel(preset.model);
    setVariant(preset.variant);
    setYear(preset.year);
    setImageUrl(preset.imageUrl);
    setFeedback(null);
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitting(true);
    setFeedback(null);

    try {
      const result = await createVehicle({
        make: make.trim(),
        model: model.trim(),
        variant: variant.trim() || null,
        year: Number(year),
        imageUrl: imageUrl.trim()
      });

      setFeedback({
        type: 'success',
        code: result.status,
        detail: `Stored internally at ${result.imageUrl} (Vehicle ID: ${result.vehicleId}).`
      });

      setMake('');
      setModel('');
      setVariant('');
      setImageUrl('');

      if (onVehicleCreated) {
        onVehicleCreated(result);
      }
    } catch (err) {
      setFeedback({
        type: 'error',
        code: err.code || 'INGESTION_FAILED',
        detail: err.detail || err.message || 'Image ingestion failed.',
        traceId: err.traceId,
        retryable: err.retryable
      });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <aside className="panel" aria-labelledby="ingest-form-heading">
      <h2 id="ingest-form-heading" className="panel-title">
        Submit Supplier Vehicle &amp; Image URL
      </h2>
      <p className="panel-description">
        Fetches via bounded HTTP streaming, validates magic bytes &amp; decoded dimensions, and persists internally.
      </p>

      <div className="preset-group">
        <span className="preset-group-label">Quick Verification Presets</span>
        <div className="preset-buttons">
          {DEMO_PRESETS.map((preset) => (
            <button
              key={preset.id}
              id={preset.id}
              type="button"
              className="btn-preset"
              onClick={() => applyPreset(preset)}
            >
              {preset.label}
            </button>
          ))}
        </div>
      </div>

      <form id="vehicle-ingestion-form" className="form-grid" onSubmit={handleSubmit}>
        <div className="form-row-split">
          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-make">
              Make
            </label>
            <input
              id="vehicle-make"
              type="text"
              required
              maxLength={100}
              className="field-input"
              placeholder="e.g. Porsche"
              value={make}
              onChange={(e) => setMake(e.target.value)}
              disabled={submitting}
            />
          </div>

          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-model">
              Model
            </label>
            <input
              id="vehicle-model"
              type="text"
              required
              maxLength={100}
              className="field-input"
              placeholder="e.g. 911"
              value={model}
              onChange={(e) => setModel(e.target.value)}
              disabled={submitting}
            />
          </div>
        </div>

        <div className="form-row-split">
          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-variant">
              Variant <span className="field-optional">(optional)</span>
            </label>
            <input
              id="vehicle-variant"
              type="text"
              maxLength={150}
              className="field-input"
              placeholder="e.g. Carrera GTS"
              value={variant}
              onChange={(e) => setVariant(e.target.value)}
              disabled={submitting}
            />
          </div>

          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-year">
              Year
            </label>
            <input
              id="vehicle-year"
              type="number"
              required
              min={1886}
              max={new Date().getFullYear() + 1}
              className="field-input"
              value={year}
              onChange={(e) => setYear(e.target.value)}
              disabled={submitting}
            />
          </div>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="vehicle-image-url">
            Supplier Image or Webpage URL
          </label>
          <input
            id="vehicle-image-url"
            type="text"
            required
            maxLength={2048}
            className="field-input field-input-mono"
            placeholder="https://supplier.example.com/listing-or-image"
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
            disabled={submitting}
          />
        </div>

        <button id="submit-vehicle-btn" type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Retrieving & Validating Bytes...' : 'Ingest & Store Vehicle Image'}
        </button>
      </form>

      {feedback && (
        <div
          id="ingestion-feedback-banner"
          className={`feedback-banner ${
            feedback.type === 'error' ? 'feedback-banner-error' : 'feedback-banner-success'
          }`}
          role="alert"
          aria-live="polite"
        >
          <div className="feedback-code-header">
            <span className="error-code-pill">{feedback.code}</span>
            {feedback.type === 'error' && (
              <span>{feedback.retryable ? 'Transient / Retryable' : 'Permanent Rejection'}</span>
            )}
          </div>
          <p className="feedback-detail">{feedback.detail}</p>
          {feedback.traceId && <div className="feedback-meta">Trace ID: {feedback.traceId}</div>}
        </div>
      )}
    </aside>
  );
}
```

## 9. CSV Bulk Import Panel (New on feature/csv-bulk-import)

**File:** `frontend/vehicle-image-dashboard/src/components/BulkImportForm.jsx`  
**Purpose:** Provides sample CSV download, .csv file selection with size pill, duplicate-submit protection, whole-file error banner, 3-stat summary grid, and per-row success/error list.

```jsx
import React, { useRef, useState } from 'react';
import { importVehiclesCsv, resolveInternalImageUrl } from '../services/vehicleApi';

const SAMPLE_CSV_CONTENT = [
  'Make,Model,Variant,Year,ImageUrl',
  'Porsche,911 Carrera,GTS Cabriolet,2026,https://en.wikipedia.org/wiki/Porsche_911',
  'BMW,M3,"Competition, xDrive",2026,https://upload.wikimedia.org/wikipedia/commons/thumb/3/39/2021_BMW_M3_Competition_Automatic_3.0_Front.jpg/960px-2021_BMW_M3_Competition_Automatic_3.0_Front.jpg'
].join('\n');

function formatFileSize(bytes) {
  if (!bytes || bytes <= 0) return '0 B';
  if (bytes < 1024) return `${bytes} B`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} KB`;
  return `${(kb / 1024).toFixed(2)} MB`;
}

export default function BulkImportForm({ onImportCompleted }) {
  const fileInputRef = useRef(null);
  const [selectedFile, setSelectedFile] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(null);
  const [importResult, setImportResult] = useState(null);

  function handleFileChange(e) {
    const file = e.target.files?.[0] || null;
    setSelectedFile(file);
    setError(null);
  }

  function handleDownloadSample() {
    const blob = new Blob([SAMPLE_CSV_CONTENT], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'sample-vehicles.csv';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (submitting || !selectedFile) {
      return;
    }

    setSubmitting(true);
    setError(null);
    setImportResult(null);

    try {
      const result = await importVehiclesCsv(selectedFile);
      setImportResult(result);
      setSelectedFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }

      if (onImportCompleted) {
        onImportCompleted(result);
      }
    } catch (err) {
      setError({
        code: err.code || 'IMPORT_FAILED',
        detail: err.detail || err.message || 'CSV bulk import failed.',
        traceId: err.traceId
      });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className="panel" aria-labelledby="bulk-import-heading" aria-busy={submitting}>
      <div className="bulk-header-row">
        <h2 id="bulk-import-heading" className="panel-title">
          Bulk Import from CSV
        </h2>
        <button
          id="download-sample-csv-btn"
          type="button"
          className="btn-preset"
          onClick={handleDownloadSample}
          disabled={submitting}
        >
          Sample CSV
        </button>
      </div>
      <p className="panel-description">
        Upload a <code>.csv</code> file (max 50 rows, 1 MB). Required columns:{' '}
        <code>Make</code>, <code>Model</code>, <code>Year</code>, <code>ImageUrl</code> (
        <code>Variant</code> is optional).
      </p>

      <form id="bulk-import-form" className="form-grid" onSubmit={handleSubmit}>
        <div className="field-group">
          <label className="field-label" htmlFor="csv-file-input">
            CSV File
          </label>
          <input
            ref={fileInputRef}
            id="csv-file-input"
            type="file"
            accept=".csv,text/csv"
            className="csv-file-input"
            onChange={handleFileChange}
            disabled={submitting}
          />
          {selectedFile && (
            <div id="selected-csv-filename" className="selected-file-pill">
              <span className="selected-file-name">{selectedFile.name}</span>
              <span className="selected-file-size">{formatFileSize(selectedFile.size)}</span>
            </div>
          )}
        </div>

        <button
          id="submit-csv-import-btn"
          type="submit"
          className="btn-primary"
          disabled={submitting || !selectedFile}
        >
          {submitting ? 'Importing CSV & Validating Images...' : 'Import CSV Vehicles'}
        </button>
      </form>

      {error && (
        <div
          id="bulk-import-error-banner"
          className="feedback-banner feedback-banner-error"
          role="alert"
          aria-live="polite"
        >
          <div className="feedback-code-header">
            <span className="error-code-pill">{error.code}</span>
            <span>Import Rejected</span>
          </div>
          <p className="feedback-detail">{error.detail}</p>
          {error.traceId && <div className="feedback-meta">Trace ID: {error.traceId}</div>}
        </div>
      )}

      {importResult && (
        <div id="bulk-import-results" className="import-results-section" aria-live="polite">
          <div className="import-summary-grid">
            <div className="import-stat">
              <span className="import-stat-label">Total Records</span>
              <strong id="import-total-rows" className="import-stat-value">
                {importResult.totalRows}
              </strong>
            </div>
            <div className="import-stat import-stat-success">
              <span className="import-stat-label">Successful</span>
              <strong id="import-successful-rows" className="import-stat-value">
                {importResult.successfulRows}
              </strong>
            </div>
            <div className="import-stat import-stat-danger">
              <span className="import-stat-label">Failed</span>
              <strong id="import-failed-rows" className="import-stat-value">
                {importResult.failedRows}
              </strong>
            </div>
          </div>

          <ul className="import-results-list">
            {(importResult.results || []).map((row) => {
              const vehicleLabel = [row.year, row.make, row.model]
                .filter((part) => part !== null && part !== undefined && part !== '')
                .join(' ');
              const internalHref = row.success ? resolveInternalImageUrl(row.imageUrl) : null;

              return (
                <li
                  key={row.rowNumber}
                  className={`import-row-card ${
                    row.success ? 'import-row-success' : 'import-row-failed'
                  }`}
                >
                  <div className="import-row-header">
                    <span className="import-row-title">
                      Row #{row.rowNumber}
                      {vehicleLabel ? ` — ${vehicleLabel}` : ''}
                    </span>
                    <span
                      className={`status-badge ${
                        row.success ? 'status-badge-completed' : 'status-badge-failed'
                      }`}
                    >
                      {row.success ? 'Succeeded' : 'Failed'}
                    </span>
                  </div>

                  {row.success ? (
                    <div className="import-row-meta">
                      <a
                        href={internalHref}
                        target="_blank"
                        rel="noreferrer"
                        className="import-image-link"
                      >
                        {row.imageUrl}
                      </a>
                    </div>
                  ) : (
                    <div className="import-row-error">
                      {row.errorCode && <span className="error-code-pill">{row.errorCode}</span>}
                      <p className="feedback-detail">{row.errorMessage}</p>
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </section>
  );
}
```

## 10. Vehicle Catalog Card

**File:** `frontend/vehicle-image-dashboard/src/components/VehicleCard.jsx`  
**Purpose:** Displays the internally served /api/images/{id} image with native lazy loading, metadata tags, download button, lazy GET /api/vehicles/{id} details, and ImageReplaceForm drawer.

```jsx
import React, { useState } from 'react';
import StatusBadge from './StatusBadge';
import ImagePlaceholder from './ImagePlaceholder';
import ImageReplaceForm from './ImageReplaceForm';
import { getVehicle, resolveInternalImageUrl } from '../services/vehicleApi';

function formatBytes(bytes) {
  if (!bytes || bytes <= 0) return null;
  if (bytes < 1024) return `${bytes} B`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} KB`;
  return `${(kb / 1024).toFixed(2)} MB`;
}

export default function VehicleCard({ vehicle, onUpdated }) {
  const [imageLoadFailed, setImageLoadFailed] = useState(false);
  const [showReplaceForm, setShowReplaceForm] = useState(false);
  const [showDetails, setShowDetails] = useState(false);
  const [details, setDetails] = useState(null);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [detailsError, setDetailsError] = useState(null);
  const [replacementSuccess, setReplacementSuccess] = useState(false);

  const internalImageSrc = resolveInternalImageUrl(vehicle.imageUrl);
  const downloadHref = internalImageSrc ? `${internalImageSrc}/download` : null;

  function handleReplaced() {
    setShowReplaceForm(false);
    setImageLoadFailed(false);
    setDetails(null);
    setReplacementSuccess(true);
    if (onUpdated) {
      onUpdated();
    }
  }

  async function handleDetailsToggle() {
    if (showDetails) {
      setShowDetails(false);
      return;
    }

    setShowDetails(true);
    if (details) {
      return;
    }

    setDetailsLoading(true);
    setDetailsError(null);
    try {
      setDetails(await getVehicle(vehicle.id));
    } catch (error) {
      setDetailsError(error.detail || error.message || 'Unable to load vehicle details.');
    } finally {
      setDetailsLoading(false);
    }
  }

  return (
    <article className="vehicle-card" id={`vehicle-card-${vehicle.id}`}>
      <div className="vehicle-media-frame">
        {internalImageSrc && !imageLoadFailed ? (
          <img
            src={internalImageSrc}
            alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
            loading="lazy"
            decoding="async"
            className="vehicle-image"
            onError={() => setImageLoadFailed(true)}
          />
        ) : (
          <ImagePlaceholder reason="Stored image unavailable" />
        )}
      </div>

      <div className="vehicle-card-body">
        {replacementSuccess && (
          <div className="feedback-banner feedback-banner-success" role="status">
            Stored image replaced successfully.
          </div>
        )}
        <div className="vehicle-card-top">
          <div>
            <h3 className="vehicle-title">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h3>
            {vehicle.variant && <p className="vehicle-variant">{vehicle.variant}</p>}
          </div>
          <StatusBadge status={vehicle.imageStatus} />
        </div>

        <div className="metadata-specs">
          {vehicle.format && <span className="spec-tag">{vehicle.format}</span>}
          {vehicle.width && vehicle.height && (
            <span className="spec-tag">
              {vehicle.width}&times;{vehicle.height}px
            </span>
          )}
          {vehicle.fileSize && <span className="spec-tag">{formatBytes(vehicle.fileSize)}</span>}
          {vehicle.retrievalStrategy && (
            <span className="spec-tag">Strategy: {vehicle.retrievalStrategy}</span>
          )}
        </div>

        {vehicle.imageUrl && (
          <div className="internal-route-box" title={`Served internally from ${vehicle.imageUrl}`}>
            Internal API: {vehicle.imageUrl}
          </div>
        )}

        <div className="vehicle-card-actions">
          {downloadHref ? (
            <a
              id={`download-image-${vehicle.id}`}
              href={downloadHref}
              className="btn-secondary"
              download
            >
              Download Stored Copy
            </a>
          ) : (
            <span />
          )}

          <button
            id={`details-${vehicle.id}`}
            type="button"
            className="btn-secondary"
            onClick={handleDetailsToggle}
            aria-expanded={showDetails}
          >
            {showDetails ? 'Hide Details' : 'View Details'}
          </button>

          <button
            id={`toggle-replace-${vehicle.id}`}
            type="button"
            className="btn-secondary"
            onClick={() => setShowReplaceForm((prev) => !prev)}
          >
            {showReplaceForm ? 'Close Replace' : 'Replace Image'}
          </button>
        </div>

        {showDetails && (
          <div className="details-panel" aria-live="polite">
            <h4>Vehicle details</h4>
            {detailsLoading && <p className="details-muted">Loading details...</p>}
            {detailsError && (
              <p className="details-error" role="alert">
                {detailsError}
              </p>
            )}
            {details && (
              <dl className="details-list">
                <div>
                  <dt>Vehicle ID</dt>
                  <dd>{details.id}</dd>
                </div>
                <div>
                  <dt>Image ID</dt>
                  <dd>{details.imageId || 'Unavailable'}</dd>
                </div>
                <div>
                  <dt>Content type</dt>
                  <dd>{details.contentType || 'Unavailable'}</dd>
                </div>
                <div>
                  <dt>Dimensions</dt>
                  <dd>
                    {details.width && details.height
                      ? `${details.width} × ${details.height}px`
                      : 'Unavailable'}
                  </dd>
                </div>
                <div>
                  <dt>Retrieved by</dt>
                  <dd>{details.retrievalStrategy || 'Unavailable'}</dd>
                </div>
                <div>
                  <dt>Updated</dt>
                  <dd>{new Date(details.updatedAt).toLocaleString()}</dd>
                </div>
              </dl>
            )}
          </div>
        )}

        {showReplaceForm && (
          <ImageReplaceForm
            vehicleId={vehicle.id}
            onReplaced={handleReplaced}
            onCancel={() => setShowReplaceForm(false)}
          />
        )}
      </div>
    </article>
  );
}
```

## 11. Inline Image Replacement Form

**File:** `frontend/vehicle-image-dashboard/src/components/ImageReplaceForm.jsx`  
**Purpose:** Submits PUT /api/vehicles/{id}/image while preserving the currently rendered image if replacement fails.

```jsx
import React, { useState } from 'react';
import { replaceImage } from '../services/vehicleApi';

export default function ImageReplaceForm({ vehicleId, onReplaced, onCancel }) {
  const [newUrl, setNewUrl] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!newUrl.trim()) return;

    setSubmitting(true);
    setError(null);

    try {
      const result = await replaceImage(vehicleId, newUrl.trim());
      setNewUrl('');
      onReplaced(result);
    } catch (err) {
      setError({
        code: err.code || 'REPLACEMENT_FAILED',
        detail: err.detail || err.message || 'Replacement failed. The existing stored image was preserved.',
        retryable: Boolean(err.retryable)
      });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="replace-drawer" onSubmit={handleSubmit}>
      <label className="field-label" htmlFor={`replace-url-${vehicleId}`}>
        Replace Supplier Image URL
      </label>
      <input
        id={`replace-url-${vehicleId}`}
        type="url"
        required
        className="field-input field-input-mono"
        placeholder="https://supplier.example.com/new-photo.jpg"
        value={newUrl}
        onChange={(e) => setNewUrl(e.target.value)}
        disabled={submitting}
      />
      <div className="replace-actions">
        <button
          id={`submit-replace-${vehicleId}`}
          type="submit"
          className="btn-primary"
          disabled={submitting}
        >
          {submitting ? 'Validating & Replacing...' : 'Commit Replacement'}
        </button>
        <button
          id={`cancel-replace-${vehicleId}`}
          type="button"
          className="btn-secondary"
          onClick={onCancel}
          disabled={submitting}
        >
          Cancel
        </button>
      </div>

      {error && (
        <div className="feedback-banner feedback-banner-error" role="alert">
          <div className="feedback-code-header">
            <span className="error-code-pill">{error.code}</span>
            <span>Existing image preserved</span>
          </div>
          <p className="feedback-detail">{error.detail}</p>
        </div>
      )}
    </form>
  );
}
```

## 12. Status Badge Component

**File:** `frontend/vehicle-image-dashboard/src/components/StatusBadge.jsx`  
**Purpose:** Renders a color-coded status pill for Completed, Processing, or Failed states.

```jsx
import React from 'react';

export default function StatusBadge({ status }) {
  const normalized = (status || 'Completed').toLowerCase();

  let badgeClass = 'status-badge status-badge-completed';
  if (normalized === 'processing') {
    badgeClass = 'status-badge status-badge-processing';
  } else if (normalized === 'failed') {
    badgeClass = 'status-badge status-badge-failed';
  }

  return <span className={badgeClass}>{status || 'Completed'}</span>;
}
```

## 13. Fallback Image Placeholder

**File:** `frontend/vehicle-image-dashboard/src/components/ImagePlaceholder.jsx`  
**Purpose:** Accessible SVG fallback box rendered when a stored image URL is absent or fails to load.

```jsx
import React from 'react';

export default function ImagePlaceholder({ reason }) {
  return (
    <div className="image-placeholder" role="img" aria-label="No valid stored vehicle image">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75">
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <circle cx="8.5" cy="10.5" r="1.5" />
        <path d="M21 15l-5-5L5 21" />
      </svg>
      <span>{reason || 'Stored image unavailable'}</span>
    </div>
  );
}
```

## 14. Dashboard Design System & Layout CSS (Modified on feature/csv-bulk-import)

**File:** `frontend/vehicle-image-dashboard/src/index.css`  
**Purpose:** Complete styling tokens, responsive 2-column grid, feedback banners, card styles, sidebar-stack, and CSV bulk-import result cards.

```css
:root {
  --bg-canvas: hsl(215, 28%, 97%);
  --bg-surface: hsl(0, 0%, 100%);
  --bg-subtle: hsl(214, 24%, 94%);
  --bg-code: hsl(215, 25%, 14%);

  --text-primary: hsl(215, 32%, 12%);
  --text-secondary: hsl(215, 18%, 36%);
  --text-muted: hsl(215, 14%, 52%);

  --border-subtle: hsl(214, 22%, 88%);
  --border-strong: hsl(214, 20%, 76%);

  --accent-primary: hsl(212, 86%, 38%);
  --accent-primary-hover: hsl(212, 88%, 31%);
  --accent-surface: hsl(212, 75%, 95%);

  --status-success-bg: hsl(152, 65%, 94%);
  --status-success-text: hsl(154, 76%, 24%);
  --status-success-border: hsl(152, 52%, 78%);

  --status-warning-bg: hsl(38, 92%, 94%);
  --status-warning-text: hsl(32, 84%, 28%);
  --status-warning-border: hsl(38, 78%, 76%);

  --status-danger-bg: hsl(356, 85%, 96%);
  --status-danger-text: hsl(356, 74%, 34%);
  --status-danger-border: hsl(356, 68%, 82%);

  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 14px;

  --shadow-xs: 0 1px 2px rgba(15, 23, 42, 0.05);
  --shadow-sm: 0 2px 8px -2px rgba(15, 23, 42, 0.07), 0 1px 3px rgba(15, 23, 42, 0.05);
  --shadow-md: 0 8px 24px -6px rgba(15, 23, 42, 0.1), 0 2px 6px rgba(15, 23, 42, 0.05);

  --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: var(--font-sans);
  background-color: var(--bg-canvas);
  color: var(--text-primary);
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
}

/* Header & Layout */
.app-header {
  background-color: var(--bg-surface);
  border-bottom: 1px solid var(--border-subtle);
  position: sticky;
  top: 0;
  z-index: 20;
}

.app-header-inner {
  max-width: 1320px;
  margin: 0 auto;
  padding: 1rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.brand-group {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.brand-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: var(--text-primary);
}

.brand-subtitle {
  margin: 0;
  font-size: 0.825rem;
  color: var(--text-secondary);
}

.security-summary-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.security-chip {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  font-weight: 500;
  padding: 0.25rem 0.6rem;
  border-radius: var(--radius-sm);
  background-color: var(--bg-subtle);
  color: var(--text-secondary);
  border: 1px solid var(--border-subtle);
}

.dashboard-container {
  max-width: 1320px;
  margin: 0 auto;
  padding: 1.75rem 1.5rem 3.5rem;
  display: grid;
  grid-template-columns: 390px 1fr;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 1024px) {
  .dashboard-container {
    grid-template-columns: 1fr;
  }
}

/* Panels & Forms */
.panel {
  background-color: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  padding: 1.5rem;
}

.panel-sticky {
  position: sticky;
  top: 5.25rem;
}

.panel-title {
  margin: 0 0 0.35rem;
  font-size: 1.05rem;
  font-weight: 600;
  letter-spacing: -0.015em;
}

.panel-description {
  margin: 0 0 1.25rem;
  font-size: 0.84rem;
  color: var(--text-secondary);
}

.preset-group {
  margin-bottom: 1.25rem;
  padding: 0.75rem;
  background-color: var(--bg-subtle);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-subtle);
}

.preset-group-label {
  display: block;
  font-size: 0.73rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
}

.preset-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.btn-preset {
  font-family: var(--font-sans);
  font-size: 0.75rem;
  font-weight: 500;
  padding: 0.32rem 0.6rem;
  background-color: var(--bg-surface);
  color: var(--text-primary);
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;
}

.btn-preset:hover {
  background-color: var(--accent-surface);
  border-color: var(--accent-primary);
  color: var(--accent-primary);
}

.form-grid {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

.form-row-split {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.field-group {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-primary);
}

.field-optional {
  font-weight: 400;
  color: var(--text-muted);
}

.field-input {
  width: 100%;
  font-family: var(--font-sans);
  font-size: 0.875rem;
  padding: 0.55rem 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  background-color: var(--bg-surface);
  color: var(--text-primary);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.field-input:focus {
  outline: none;
  border-color: var(--accent-primary);
  box-shadow: 0 0 0 3px rgba(19, 102, 198, 0.15);
}

.field-input-mono {
  font-family: var(--font-mono);
  font-size: 0.8rem;
}

.btn-primary {
  font-family: var(--font-sans);
  font-size: 0.875rem;
  font-weight: 600;
  padding: 0.65rem 1rem;
  border: none;
  border-radius: var(--radius-sm);
  background-color: var(--accent-primary);
  color: #ffffff;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.1s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-primary:hover:not(:disabled) {
  background-color: var(--accent-primary-hover);
}

.btn-primary:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.btn-secondary {
  font-family: var(--font-sans);
  font-size: 0.78rem;
  font-weight: 500;
  padding: 0.42rem 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  background-color: var(--bg-surface);
  color: var(--text-primary);
  text-decoration: none;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.btn-secondary:hover:not(:disabled) {
  background-color: var(--bg-subtle);
  border-color: var(--text-secondary);
}

/* Alerts & Feedback */
.feedback-banner {
  margin-top: 1rem;
  padding: 0.85rem 1rem;
  border-radius: var(--radius-md);
  font-size: 0.825rem;
}

.feedback-banner-error {
  background-color: var(--status-danger-bg);
  color: var(--status-danger-text);
  border: 1px solid var(--status-danger-border);
}

.feedback-banner-success {
  background-color: var(--status-success-bg);
  color: var(--status-success-text);
  border: 1px solid var(--status-success-border);
}

.feedback-code-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.3rem;
}

.error-code-pill {
  font-family: var(--font-mono);
  font-size: 0.74rem;
  font-weight: 600;
  padding: 0.15rem 0.45rem;
  background-color: rgba(153, 27, 27, 0.12);
  border-radius: 4px;
}

.feedback-detail {
  margin: 0;
  line-height: 1.45;
}

.feedback-meta {
  margin-top: 0.4rem;
  font-family: var(--font-mono);
  font-size: 0.7rem;
  opacity: 0.85;
}

/* Status Badge */
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.73rem;
  font-weight: 600;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
}

.status-badge-completed {
  background-color: var(--status-success-bg);
  color: var(--status-success-text);
  border: 1px solid var(--status-success-border);
}

.status-badge-processing {
  background-color: var(--status-warning-bg);
  color: var(--status-warning-text);
  border: 1px solid var(--status-warning-border);
}

.status-badge-failed {
  background-color: var(--status-danger-bg);
  color: var(--status-danger-text);
  border: 1px solid var(--status-danger-border);
}

/* Catalog Section & Grid */
.catalog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  flex-wrap: wrap;
}

.catalog-title-group h2 {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 600;
  letter-spacing: -0.015em;
}

.catalog-title-group p {
  margin: 0.15rem 0 0;
  font-size: 0.825rem;
  color: var(--text-secondary);
}

.vehicle-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(310px, 1fr));
  gap: 1.25rem;
}

.vehicle-card {
  background-color: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  transition: box-shadow 0.2s ease, transform 0.2s ease;
}

.vehicle-card:hover {
  box-shadow: var(--shadow-md);
}

.vehicle-media-frame {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  background-color: var(--bg-subtle);
  border-bottom: 1px solid var(--border-subtle);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}

.vehicle-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.image-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.8rem;
  gap: 0.4rem;
}

.vehicle-card-body {
  padding: 1.15rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  flex: 1;
}

.vehicle-card-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}

.vehicle-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.vehicle-variant {
  margin: 0.15rem 0 0;
  font-size: 0.8rem;
  color: var(--text-secondary);
}

.metadata-specs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.spec-tag {
  font-family: var(--font-mono);
  font-size: 0.71rem;
  padding: 0.18rem 0.48rem;
  border-radius: 4px;
  background-color: var(--bg-subtle);
  color: var(--text-secondary);
  border: 1px solid var(--border-subtle);
}

.internal-route-box {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  padding: 0.4rem 0.6rem;
  background-color: var(--bg-subtle);
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.vehicle-card-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: auto;
  padding-top: 0.5rem;
  border-top: 1px solid var(--border-subtle);
  flex-wrap: wrap;
}

.replace-drawer {
  margin-top: 0.65rem;
  padding-top: 0.75rem;
  border-top: 1px dashed var(--border-strong);
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.replace-actions {
  display: flex;
  gap: 0.5rem;
}

.details-panel {
  padding: 0.75rem;
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  background-color: var(--bg-subtle);
}

.details-panel h4 {
  margin: 0 0 0.65rem;
  font-size: 0.82rem;
}

.details-list {
  display: grid;
  gap: 0.45rem;
  margin: 0;
  font-size: 0.75rem;
}

.details-list > div {
  display: grid;
  grid-template-columns: 90px 1fr;
  gap: 0.5rem;
}

.details-list dt {
  color: var(--text-muted);
}

.details-list dd {
  margin: 0;
  overflow-wrap: anywhere;
  color: var(--text-secondary);
}

.details-muted,
.details-error {
  margin: 0;
  font-size: 0.78rem;
}

.details-muted {
  color: var(--text-muted);
}

.details-error {
  color: var(--status-danger-text);
}

.empty-catalog {
  background-color: var(--bg-surface);
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius-lg);
  padding: 3rem 1.5rem;
  text-align: center;
  color: var(--text-secondary);
}

.pagination-bar {
  margin-top: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 1.15rem;
  background-color: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  font-size: 0.84rem;
}

/* Sidebar Stack & CSV Bulk Import */
.sidebar-stack {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.bulk-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.35rem;
}

.bulk-header-row .panel-title {
  margin: 0;
}

.csv-file-input {
  width: 100%;
  font-family: var(--font-sans);
  font-size: 0.82rem;
  padding: 0.5rem;
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius-sm);
  background-color: var(--bg-subtle);
  color: var(--text-primary);
  cursor: pointer;
}

.csv-file-input:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.selected-file-pill {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.3rem;
  padding: 0.4rem 0.65rem;
  background-color: var(--accent-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  font-size: 0.78rem;
}

.selected-file-name {
  font-family: var(--font-mono);
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.selected-file-size {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  color: var(--text-secondary);
  flex-shrink: 0;
}

.import-results-section {
  margin-top: 1.15rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-subtle);
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.import-summary-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.5rem;
}

.import-stat {
  padding: 0.55rem 0.65rem;
  border-radius: var(--radius-sm);
  background-color: var(--bg-subtle);
  border: 1px solid var(--border-subtle);
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}

.import-stat-success {
  background-color: var(--status-success-bg);
  border-color: var(--status-success-border);
  color: var(--status-success-text);
}

.import-stat-danger {
  background-color: var(--status-danger-bg);
  border-color: var(--status-danger-border);
  color: var(--status-danger-text);
}

.import-stat-label {
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.import-stat-value {
  font-family: var(--font-mono);
  font-size: 1rem;
}

.import-results-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  max-height: 360px;
  overflow-y: auto;
}

.import-row-card {
  padding: 0.65rem 0.75rem;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-subtle);
  background-color: var(--bg-surface);
  font-size: 0.78rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.import-row-success {
  border-left: 3px solid var(--status-success-text);
}

.import-row-failed {
  border-left: 3px solid var(--status-danger-text);
}

.import-row-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.import-row-title {
  font-weight: 600;
  color: var(--text-primary);
}

.import-row-meta {
  font-family: var(--font-mono);
  font-size: 0.72rem;
}

.import-image-link {
  color: var(--accent-primary);
  text-decoration: none;
  word-break: break-all;
}

.import-image-link:hover {
  text-decoration: underline;
}

.import-row-error {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.25rem;
  color: var(--status-danger-text);
}
```

