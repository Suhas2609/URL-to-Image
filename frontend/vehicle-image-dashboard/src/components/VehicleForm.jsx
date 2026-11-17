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
    id: 'preset-vehicle-d',
    label: 'Vehicle D: Google Drive Link',
    make: 'Honda',
    model: 'Civic',
    variant: 'Public Drive Image',
    year: 2026,
    imageUrl: 'https://drive.google.com/file/d/1Y7xhUt-X-Frjt-gHtPXt7BEQL5101O7e/view?usp=sharing'
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
    <section className="panel" aria-labelledby="ingest-form-heading">
      <h2 id="ingest-form-heading" className="panel-title">
        Submit Supplier Vehicle &amp; Image URL
      </h2>
      <p className="panel-description">
        Fetches via validated URL streaming, validates magic bytes &amp; decoded dimensions, and persists internally.
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
            Supplier Image, Webpage, or Google Drive URL
          </label>
          <input
            id="vehicle-image-url"
            type="text"
            required
            maxLength={2048}
            className="field-input field-input-mono"
            placeholder="https://drive.google.com/file/d/... or https://supplier.example.com/image.jpg"
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
    </section>
  );
}
