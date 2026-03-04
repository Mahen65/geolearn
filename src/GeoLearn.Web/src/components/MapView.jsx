import 'leaflet/dist/leaflet.css';
import { MapContainer, TileLayer } from 'react-leaflet';
import ParcelLayer from './ParcelLayer';

export default function MapView({ featureCollection, wsmVersion, appendBatch, onParcelClick }) {
  return (
    <MapContainer
      center={[8, 81]}
      zoom={8}
      style={{ height: '100%', width: '100%' }}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {featureCollection && (
        <ParcelLayer
          data={featureCollection}
          dataKey={wsmVersion}
          appendBatch={appendBatch}
          onParcelClick={onParcelClick}
        />
      )}
    </MapContainer>
  );
}
