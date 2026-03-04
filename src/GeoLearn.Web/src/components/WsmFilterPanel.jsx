import { useState, useEffect } from 'react';
import { fetchCountries, fetchFilterOptions } from '../api';

const panelStyle = {
  position: 'absolute',
  top: 80,
  left: 10,
  zIndex: 1000,
  background: 'rgba(255,255,255,0.95)',
  borderRadius: '6px',
  boxShadow: '0 2px 8px rgba(0,0,0,0.25)',
  padding: '12px',
  width: '200px',
  fontSize: '13px',
};

const labelStyle = {
  display: 'block',
  fontWeight: 600,
  color: '#555',
  marginBottom: '3px',
  marginTop: '8px',
  fontSize: '11px',
  textTransform: 'uppercase',
};

const selectStyle = {
  width: '100%',
  padding: '5px 6px',
  borderRadius: '4px',
  border: '1px solid #ccc',
  fontSize: '12px',
  background: '#fff',
};

const disabledSelectStyle = {
  ...selectStyle,
  background: '#f5f5f5',
  color: '#aaa',
};

const btnBase = {
  width: '100%',
  padding: '7px 0',
  border: 'none',
  borderRadius: '4px',
  fontWeight: 600,
  fontSize: '12px',
  cursor: 'pointer',
  marginTop: '6px',
};

export default function WsmFilterPanel({ wsmLoaded, loading, featureCount, onLoad, onUnload, uploadVersion, uploadedCountry }) {
  const [countries, setCountries]             = useState([]);
  const [country, setCountry]                 = useState('');
  const [labels, setLabels]                   = useState({ main: 'Main Category', sub: 'Sub Category' });
  const [mainValues, setMainValues]           = useState([]);
  const [subValues, setSubValues]             = useState([]);
  const [mainCategory, setMainCategory]       = useState('');
  const [subCategory, setSubCategory]         = useState('');
  const [loadingMain, setLoadingMain]         = useState(false);
  const [loadingSub, setLoadingSub]           = useState(false);

  // Load country list on mount, and re-fetch after each upload.
  useEffect(() => {
    fetchCountries()
      .then(setCountries)
      .catch(console.error);
  }, [uploadVersion]);

  // When a shapefile is uploaded, select its country.
  useEffect(() => {
    if (!uploadedCountry) return;
    setCountry(uploadedCountry);
    setMainCategory('');
    setSubCategory('');
    setMainValues([]);
    setSubValues([]);
  }, [uploadVersion, uploadedCountry]); // eslint-disable-line react-hooks/exhaustive-deps

  // When country changes, load main category options.
  useEffect(() => {
    if (!country) {
      setLabels({ main: 'Main Category', sub: 'Sub Category' });
      setMainValues([]);
      setSubValues([]);
      setMainCategory('');
      setSubCategory('');
      return;
    }
    setLoadingMain(true);
    setMainCategory('');
    setSubCategory('');
    setSubValues([]);
    fetchFilterOptions(country)
      .then(data => {
        setLabels({ main: data.mainCategoryLabel, sub: data.subCategoryLabel });
        setMainValues(data.mainCategoryValues);
      })
      .catch(console.error)
      .finally(() => setLoadingMain(false));
  }, [country]);

  // When main category changes, cascade-load sub category options.
  useEffect(() => {
    if (!country || !mainCategory) {
      setSubValues([]);
      setSubCategory('');
      return;
    }
    setLoadingSub(true);
    setSubCategory('');
    fetchFilterOptions(country, mainCategory)
      .then(data => setSubValues(data.subCategoryValues))
      .catch(console.error)
      .finally(() => setLoadingSub(false));
  }, [country, mainCategory]);

  const disabled = wsmLoaded || loading;
  const canLoad   = !wsmLoaded && !loading && !!country;
  const canUnload = wsmLoaded && !loading;

  function handleLoad() {
    onLoad({ country, mainCategory, subCategory });
  }

  return (
    <div style={panelStyle}>
      <div style={{ fontWeight: 700, fontSize: '11px', color: '#333', letterSpacing: '0.5px' }}>
        FILTERS
      </div>

      <label style={labelStyle}>Country</label>
      <select
        style={disabled ? disabledSelectStyle : selectStyle}
        value={country}
        onChange={e => setCountry(e.target.value)}
        disabled={disabled}
      >
        <option value="">Select country…</option>
        {countries.map(c => (
          <option key={c.code} value={c.code}>{c.name}</option>
        ))}
      </select>

      <label style={labelStyle}>{labels.main}</label>
      <select
        style={(!country || disabled) ? disabledSelectStyle : selectStyle}
        value={mainCategory}
        onChange={e => setMainCategory(e.target.value)}
        disabled={!country || disabled}
      >
        <option value="">{loadingMain ? 'Loading…' : 'Any'}</option>
        {mainValues.map(v => <option key={v} value={v}>{v}</option>)}
      </select>

      <label style={labelStyle}>{labels.sub}</label>
      <select
        style={(!mainCategory || disabled) ? disabledSelectStyle : selectStyle}
        value={subCategory}
        onChange={e => setSubCategory(e.target.value)}
        disabled={!mainCategory || disabled}
      >
        <option value="">{loadingSub ? 'Loading…' : 'Any'}</option>
        {subValues.map(v => <option key={v} value={v}>{v}</option>)}
      </select>

      <button
        style={{ ...btnBase, background: canLoad ? '#2d6a4f' : '#e0e0e0', color: canLoad ? '#fff' : '#999', cursor: canLoad ? 'pointer' : 'default' }}
        onClick={handleLoad}
        disabled={!canLoad}
      >
        {loading ? `Loading… (${featureCount.toLocaleString()})` : 'Load'}
      </button>

      <button
        style={{ ...btnBase, background: canUnload ? '#b00020' : '#e0e0e0', color: canUnload ? '#fff' : '#999', cursor: canUnload ? 'pointer' : 'default' }}
        onClick={onUnload}
        disabled={!canUnload}
      >
        Unload
      </button>

      {wsmLoaded && !loading && (
        <div style={{ marginTop: '8px', fontSize: '11px', color: '#555' }}>
          {featureCount.toLocaleString()} feature{featureCount !== 1 ? 's' : ''} loaded
        </div>
      )}
    </div>
  );
}
