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
