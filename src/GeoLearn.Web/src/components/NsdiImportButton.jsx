import { useState } from 'react';
import { importNsdiData } from '../api';

const buttonStyle = {
  position: 'absolute',
  bottom: '24px',
  left: '220px',
  zIndex: 1000,
  background: '#1d3557',
  color: '#fff',
  border: 'none',
  borderRadius: '6px',
  padding: '10px 16px',
  cursor: 'pointer',
  fontSize: '0.9em',
};

export default function NsdiImportButton({ onImportSuccess }) {
  const [status, setStatus] = useState(null); // null | 'importing' | 'ok' | 'error'
  const [message, setMessage] = useState('');

  async function handleClick() {
    setStatus('importing');
    setMessage('');
    try {
      const result = await importNsdiData();
      setStatus('ok');
      setMessage(`Imported ${result.imported} features from NSDI.`);
      onImportSuccess();
    } catch (err) {
      setStatus('error');
      setMessage(err.name === 'AbortError' ? 'Request timed out.' : err.message);
    }
  }

  return (
    <>
      <button
        style={buttonStyle}
        onClick={handleClick}
        disabled={status === 'importing'}
      >
        {status === 'importing' ? 'Importing NSDI… (may take ~2 min)' : 'Import NSDI Sri Lanka'}
      </button>
      {message && (
        <div
          style={{
            position: 'absolute',
            bottom: '70px',
            left: '220px',
            zIndex: 1000,
            background: status === 'error' ? '#b00020' : '#1d3557',
            color: '#fff',
            padding: '8px 12px',
            borderRadius: '4px',
            maxWidth: '260px',
            fontSize: '0.85em',
          }}
        >
          {message}
        </div>
      )}
    </>
  );
}
