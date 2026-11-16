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
            <span className="security-chip">SSRF ConnectGuard</span>
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
