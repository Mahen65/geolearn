const sidebarStyle = {
  position: 'absolute',
  top: 0,
  right: 0,
  width: '280px',
  height: '100%',
  background: 'rgba(255,255,255,0.95)',
  padding: '16px',
  boxSizing: 'border-box',
  zIndex: 1000,
  overflowY: 'auto',
  borderLeft: '1px solid #ccc',
};

const labelStyle = { color: '#555', fontSize: '0.8em', marginBottom: 2 };
const valueStyle = { fontWeight: 'bold', marginBottom: 12 };

function Field({ label, value }) {
  return (
    <div>
      <div style={labelStyle}>{label}</div>
      <div style={valueStyle}>{value ?? '—'}</div>
    </div>
  );
}

export default function ParcelSidebar({ parcel }) {
  if (!parcel) {
    return (
      <div style={sidebarStyle}>
        <h3 style={{ marginTop: 0 }}>ForestLink</h3>
        <p style={{ color: '#888' }}>Click a parcel to view details.</p>
      </div>
    );
  }

  return (
    <div style={sidebarStyle}>
      <h3 style={{ marginTop: 0 }}>Parcel Detail</h3>
      <Field label="Name" value={parcel.Name} />
      <Field label="Compartment ID" value={parcel.CompartmentId} />
      <Field label="Species Code" value={parcel.SpeciesCode} />
      <Field label="Age (years)" value={parcel.AgeYears} />
      <hr />
      <p style={{ fontSize: '0.85em', color: '#444', marginBottom: 4 }}>
        Area comparison
      </p>
      <Field label="AreaHa (shapefile attribute)" value={parcel.AreaHa != null ? `${parcel.AreaHa} ha` : null} />
      <Field label="AreaHaLive (PostGIS computed)" value={parcel.AreaHaLive != null ? `${Number(parcel.AreaHaLive).toFixed(4)} ha` : null} />
      <p style={{ fontSize: '0.75em', color: '#888' }}>
        AreaHaLive is calculated by PostGIS as{' '}
        <code>ST_Area(geom) / 10000</code> from the stored SWEREF99 TM geometry.
      </p>
    </div>
  );
}
