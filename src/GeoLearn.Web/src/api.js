const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';

export async function fetchCountries() {
  const res = await fetch(`${BASE}/workobjects/countries`);
  if (!res.ok) throw new Error(`Failed to fetch countries: ${res.status}`);
  return res.json();
}

/**
 * Fetch filter options for a country.
 *
 * Without mainCategory: returns { mainCategoryLabel, subCategoryLabel, mainCategoryValues }
 * With mainCategory: returns { subCategoryValues } — cascade, filtered to that main category.
 */
export async function fetchFilterOptions(country, mainCategory) {
  const params = new URLSearchParams({ country });
  if (mainCategory) params.set('mainCategory', mainCategory);
  const res = await fetch(`${BASE}/workobjects/filter-options?${params}`);
  if (!res.ok) throw new Error(`Failed to fetch filter options: ${res.status}`);
  return res.json();
}

/**
 * Streams work objects as NDJSON (one GeoJSON Feature per line).
 * Calls onBatch({ type, features }) every time BATCH_SIZE features accumulate.
 * Pass an AbortSignal to cancel mid-stream.
 */
export async function streamParcels(filters = {}, onBatch, signal) {
  const BATCH_SIZE = 500;
  const params = new URLSearchParams();
  if (filters.country)      params.set('country',      filters.country);
  if (filters.mainCategory) params.set('mainCategory', filters.mainCategory);
  if (filters.subCategory)  params.set('subCategory',  filters.subCategory);

  const res = await fetch(`${BASE}/workobjects?${params}`, { signal });
  if (!res.ok) throw new Error(`Stream failed: ${res.status}`);

  const reader  = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let batch  = [];

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      const newlineIdx = buffer.lastIndexOf('\n');
      if (newlineIdx === -1) continue;

      const complete = buffer.slice(0, newlineIdx);
      buffer = buffer.slice(newlineIdx + 1);

      for (const line of complete.split('\n')) {
        if (!line.trim()) continue;
        try { batch.push(JSON.parse(line)); } catch { /* skip malformed line */ }
        if (batch.length >= BATCH_SIZE) {
          onBatch({ type: 'FeatureCollection', features: batch });
          batch = [];
        }
      }
    }
  } finally {
    reader.releaseLock();
  }

  // Flush any remaining features.
  if (buffer.trim()) {
    try { batch.push(JSON.parse(buffer)); } catch { /* skip */ }
  }
  if (batch.length > 0) {
    onBatch({ type: 'FeatureCollection', features: batch });
  }
}

export async function fetchParcel(id) {
  const res = await fetch(`${BASE}/workobjects/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch parcel ${id}: ${res.status}`);
  return res.json();
}

export async function importNsdiData() {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 300_000); // 5 min
  try {
    const res = await fetch(`${BASE}/workobjects/import-nsdi`, {
      method: 'POST',
      signal: controller.signal,
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Import failed: ${res.status} — ${text}`);
    }
    return res.json();
  } finally {
    clearTimeout(timeout);
  }
}

export async function uploadShapefile(file, countryCode = 'LK') {
  const formData = new FormData();
  formData.append('file', file);
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 600_000); // 10 min
  try {
    const res = await fetch(`${BASE}/workobjects/upload?countryCode=${encodeURIComponent(countryCode)}`, {
      method: 'POST',
      body: formData,
      signal: controller.signal,
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Upload failed: ${res.status} — ${text}`);
    }
    return res.json();
  } finally {
    clearTimeout(timeout);
  }
}
