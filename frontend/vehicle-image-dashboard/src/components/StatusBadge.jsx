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
