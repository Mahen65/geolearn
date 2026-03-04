const btnBase = {
  padding: '8px 16px',
  border: 'none',
  borderRadius: '4px',
  fontWeight: 600,
  fontSize: '13px',
  cursor: 'pointer',
};

export default function LayerControls({ wsmLoaded, loading, onLoad, onUnload }) {
  return (
    <div
      style={{
        position: 'absolute',
        top: 80,
        left: 10,
        zIndex: 1000,
        display: 'flex',
        flexDirection: 'column',
        gap: '6px',
        background: 'rgba(255,255,255,0.92)',
        padding: '10px',
        borderRadius: '6px',
        boxShadow: '0 2px 6px rgba(0,0,0,0.25)',
        minWidth: '130px',
      }}
    >
      <div style={{ fontSize: '11px', fontWeight: 700, color: '#555', marginBottom: '2px' }}>
        LAYERS
      </div>
      <button
        onClick={onLoad}
        disabled={wsmLoaded || loading}
        style={{
          ...btnBase,
          background: wsmLoaded || loading ? '#e0e0e0' : '#2d6a4f',
          color: wsmLoaded || loading ? '#999' : '#fff',
          cursor: wsmLoaded || loading ? 'default' : 'pointer',
        }}
      >
        {loading ? 'Loading…' : 'Load WSM'}
      </button>
      <button
        onClick={onUnload}
        disabled={!wsmLoaded}
        style={{
          ...btnBase,
          background: !wsmLoaded ? '#e0e0e0' : '#b00020',
          color: !wsmLoaded ? '#999' : '#fff',
          cursor: !wsmLoaded ? 'default' : 'pointer',
        }}
      >
        Unload WSM
      </button>
    </div>
  );
}
