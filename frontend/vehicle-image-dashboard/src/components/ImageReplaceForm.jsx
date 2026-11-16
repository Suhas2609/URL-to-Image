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
