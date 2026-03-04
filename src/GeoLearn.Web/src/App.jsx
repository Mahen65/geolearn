import { useState, useCallback, useRef } from 'react';
import './index.css';
import { streamParcels } from './api';
import MapView from './components/MapView';
import ParcelSidebar from './components/ParcelSidebar';
import UploadButton from './components/UploadButton';
import NsdiImportButton from './components/NsdiImportButton';
import WsmFilterPanel from './components/WsmFilterPanel';

export default function App() {
  const [featureCollection, setFeatureCollection] = useState(null);
  const [appendBatch, setAppendBatch]             = useState(null);
  const [wsmVersion, setWsmVersion]               = useState(0);
  const [wsmMeta, setWsmMeta]                     = useState({ count: 0 });
  const [selectedParcel, setSelectedParcel]       = useState(null);
  const [error, setError]                         = useState(null);
  const [wsmLoading, setWsmLoading]               = useState(false);
  const [uploadVersion, setUploadVersion]         = useState(0);
  const [uploadedCountry, setUploadedCountry]     = useState('');

  // Holds the AbortController for any in-progress stream so we can cancel it.
  const abortRef = useRef(null);

  const handleLoadWsm = useCallback(async (filters) => {
    // Cancel any previous stream.
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setWsmLoading(true);
    setError(null);
    setWsmMeta({ count: 0 });

    let isFirst = true;

    try {
      await streamParcels(
        filters,
        (batch) => {
          if (isFirst) {
            // First batch — mount the Leaflet layer so features appear immediately.
            setFeatureCollection(batch);
            setAppendBatch(null);
            setWsmVersion(v => v + 1);
            isFirst = false;
          } else {
            // Subsequent batches — append via addData(), no remount.
            setAppendBatch(batch);
          }
          setWsmMeta(prev => ({ count: prev.count + batch.features.length }));
        },
        controller.signal,
      );
    } catch (err) {
      if (err.name !== 'AbortError') setError(err.message);
    } finally {
      setWsmLoading(false);
    }
  }, []);

  const handleUnloadWsm = useCallback(() => {
    abortRef.current?.abort();
    setFeatureCollection(null);
    setAppendBatch(null);
    setWsmMeta({ count: 0 });
    setSelectedParcel(null);
  }, []);

  const handleDataChanged = useCallback((countryCode) => {
    handleUnloadWsm();
    setUploadVersion(v => v + 1);
    setUploadedCountry(countryCode ?? '');
    if (countryCode) {
      handleLoadWsm({ country: countryCode, mainCategory: '', subCategory: '' });
    }
  }, [handleUnloadWsm, handleLoadWsm]);

  return (
    <div style={{ position: 'relative', height: '100%' }}>
      <MapView
        featureCollection={featureCollection}
        wsmVersion={wsmVersion}
        appendBatch={appendBatch}
        onParcelClick={setSelectedParcel}
      />
      <WsmFilterPanel
        wsmLoaded={featureCollection !== null}
        loading={wsmLoading}
        featureCount={wsmMeta.count}
        onLoad={handleLoadWsm}
        onUnload={handleUnloadWsm}
        uploadVersion={uploadVersion}
        uploadedCountry={uploadedCountry}
      />
      <ParcelSidebar parcel={selectedParcel} />
      <UploadButton onUploadSuccess={handleDataChanged} />
      
      {error && (
        <div
          style={{
            position: 'absolute',
            top: 12,
            left: '50%',
            transform: 'translateX(-50%)',
            background: '#b00020',
            color: '#fff',
            padding: '8px 16px',
            borderRadius: '4px',
            zIndex: 1000,
          }}
        >
          {error}
        </div>
      )}
    </div>
  );
}
