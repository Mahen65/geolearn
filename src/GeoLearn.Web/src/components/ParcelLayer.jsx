import { useEffect, useRef } from 'react';
import { GeoJSON, useMap } from 'react-leaflet';

const parcelStyle = {
  color: '#2d6a4f',
  weight: 2,
  fillColor: '#52b788',
  fillOpacity: 0.4,
};

const highlightStyle = {
  color: '#1b4332',
  weight: 3,
  fillColor: '#40916c',
  fillOpacity: 0.6,
};

export default function ParcelLayer({ data, dataKey, appendBatch, onParcelClick }) {
  const map = useMap();
  const geoJsonRef = useRef(null);

  // Append new batch directly onto the existing Leaflet layer — no remount,
  // no re-render of already-drawn features. Leaflet re-uses the onEachFeature
  // option stored on the layer instance, so click/hover still work.
  useEffect(() => {
    if (!appendBatch || !geoJsonRef.current) return;
    geoJsonRef.current.addData(appendBatch);
  }, [appendBatch]);

  // Fit map to the bounds of the first batch whenever a new load starts.
  useEffect(() => {
    if (!geoJsonRef.current) return;
    try {
      const bounds = geoJsonRef.current.getBounds();
      if (bounds.isValid()) map.fitBounds(bounds, { padding: [40, 40] });
    } catch (_) { /* layer may be empty */ }
  }, [dataKey]); // eslint-disable-line react-hooks/exhaustive-deps

  function onEachFeature(feature, layer) {
    layer.on({
      click:     () => onParcelClick(feature.properties),
      mouseover: (e) => e.target.setStyle(highlightStyle),
      mouseout:  (e) => e.target.setStyle(parcelStyle),
    });
  }

  // key forces a full remount when filters change or layer is reloaded from scratch.
  return (
    <GeoJSON
      ref={geoJsonRef}
      key={dataKey}
      data={data}
      style={parcelStyle}
      onEachFeature={onEachFeature}
    />
  );
}
