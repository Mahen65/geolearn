import { useRef, useState, useEffect } from 'react';
import { uploadShapefile, fetchAllCountries } from '../api';

const wrapStyle = {
  position: 'absolute',
  bottom: '24px',
  left: '24px',
  zIndex: 1000,
  display: 'flex',
  flexDirection: 'column',
  gap: '6px',
  alignItems: 'flex-start',
};

const rowStyle = {
  display: 'flex',
  gap: '6px',
  alignItems: 'center',
};

const selectStyle = {
  padding: '8px 10px',
  borderRadius: '6px',
  border: '1px solid #ccc',
  fontSize: '0.85em',
  background: '#fff',
  cursor: 'pointer',
};

const buttonStyle = {
  background: '#2d6a4f',
  color: '#fff',
  border: 'none',
  borderRadius: '6px',
  padding: '10px 14px',
  cursor: 'pointer',
  fontSize: '0.85em',
  whiteSpace: 'nowrap',
};

export default function UploadButton({ onUploadSuccess }) {
  const inputRef = useRef(null);
  const [status, setStatus]         = useState(null); // null | 'uploading' | 'ok' | 'error'
  const [message, setMessage]       = useState('');
  const [countries, setCountries]   = useState([]);
  const [countryCode, setCountryCode] = useState('LK');

  useEffect(() => {
    fetchAllCountries()
      .then(list => {
        setCountries(list);
        if (list.length > 0 && !list.some(c => c.code === countryCode))
          setCountryCode(list[0].code);
      })
      .catch(() => { /* silently keep default */ });
  }, []);

  async function handleFile(e) {
    const file = e.target.files[0];
    if (!file) return;

    // Auto-detect country code from filename produced by shapefile tool:
    // pattern: {name}_{CC}_buildings.zip  or  {name}_{CC}.zip
    let uploadCode = countryCode;
    const match = file.name.match(/_([A-Z]{2})(?:_buildings)?\.zip$/i);
    if (match) {
      const detected = match[1].toUpperCase();
      if (countries.some(c => c.code === detected)) {
        uploadCode = detected;
        setCountryCode(detected);
      }
    }

    setStatus('uploading');
    setMessage('');
    try {
      const result = await uploadShapefile(file, uploadCode);
      setStatus('ok');
      setMessage(`Imported ${result.inserted ?? '?'} feature(s).`);
      onUploadSuccess(uploadCode);
    } catch (err) {
      setStatus('error');
      setMessage(err.message);
    } finally {
      e.target.value = '';
    }
  }

  return (
    <div style={wrapStyle}>
      <input
        ref={inputRef}
        type="file"
        accept=".zip"
        style={{ display: 'none' }}
        onChange={handleFile}
      />

      <div style={rowStyle}>
        <select
          style={selectStyle}
          value={countryCode}
          onChange={e => setCountryCode(e.target.value)}
          disabled={status === 'uploading'}
          title="Country this shapefile belongs to"
        >
          {countries.length === 0
            ? <option value="LK">Sri Lanka</option>
            : countries.map(c => <option key={c.code} value={c.code}>{c.name}</option>)
          }
        </select>

        <button
          style={{ ...buttonStyle, opacity: status === 'uploading' ? 0.6 : 1 }}
          onClick={() => inputRef.current.click()}
          disabled={status === 'uploading'}
        >
          {status === 'uploading' ? 'Uploading…' : 'Upload Shapefile'}
        </button>
      </div>

      {message && (
        <div style={{
          background: status === 'error' ? '#b00020' : '#1b4332',
          color: '#fff',
          padding: '6px 10px',
          borderRadius: '4px',
          maxWidth: '280px',
          fontSize: '0.8em',
        }}>
          {message}
        </div>
      )}
    </div>
  );
}
