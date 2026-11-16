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
