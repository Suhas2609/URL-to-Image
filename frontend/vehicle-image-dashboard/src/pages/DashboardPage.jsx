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
      <aside className="sidebar-static-section" aria-label="Vehicle Ingestion Controls">
        <div className="sidebar-window-header">
          <div className="window-dots" aria-hidden="true" title="Static Ingestion Pane">
            <span className="window-dot window-dot-red" />
            <span className="window-dot window-dot-yellow" />
            <span className="window-dot window-dot-green" />
          </div>
          <span className="sidebar-window-title">Ingestion Controls</span>
          <div className="sidebar-window-actions" aria-hidden="true" title="Static Section Indicator">
            <span className="action-dot" />
            <span className="action-dot" />
            <span className="action-dot" />
          </div>
        </div>
        <div className="sidebar-scrollable-content">
          <div className="sidebar-stack">
            <VehicleForm onVehicleCreated={handleCatalogRefreshFromPageOne} />
            <BulkImportForm onImportCompleted={handleCatalogRefreshFromPageOne} />
          </div>
        </div>
      </aside>

      <section className="catalog-section" aria-labelledby="catalog-heading" aria-busy={loading}>
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
