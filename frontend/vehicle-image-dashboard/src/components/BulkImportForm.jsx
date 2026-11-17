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
