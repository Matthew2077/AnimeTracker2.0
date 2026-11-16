async function request(method, url, body) {
  const res = await fetch(url, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) {
    let msg = `${res.status} ${res.statusText}`;
    try {
      const j = await res.json();
      msg = j.error ?? (j.errors ? Object.values(j.errors).flat().join(' ') : msg);
    } catch { /* body is not JSON */ }
    throw new Error(msg);
  }
  return res.status === 204 ? null : res.json();
}

const qs = (p) => {
  const u = new URLSearchParams();
  for (const [k, v] of Object.entries(p ?? {})) if (v) u.set(k, v);
  return u.toString();
};

export const api = {
  stats: () => request('GET', '/api/stats'),
  genres: () => request('GET', '/api/genres'),
  seriesList: (filters) => request('GET', `/api/series?${qs(filters)}`),
  randomSeries: () => request('GET', '/api/series/random'),
  series: (id) => request('GET', `/api/series/${id}`),
  createSeries: (body) => request('POST', '/api/series', body),
  updateSeries: (id, body) => request('PUT', `/api/series/${id}`, body),
  entries: (id) => request('GET', `/api/series/${id}/entries`),
  createEntry: (body) => request('POST', '/api/entries', body),
  updateEntry: (id, body) => request('PUT', `/api/entries/${id}`, body),
  watching: () => request('GET', '/api/watching'),
  increment: (id) => request('POST', `/api/entries/${id}/increment`),
  decrement: (id) => request('POST', `/api/entries/${id}/decrement`),
  setStatus: (id, status) => request('POST', `/api/entries/${id}/status`, { status }),
  reorder: (id, body) => request('POST', `/api/series/${id}/reorder`, body),
  upload: async (file) => {
    const fd = new FormData();
    fd.append('file', file);
    const res = await fetch('/api/uploads', { method: 'POST', body: fd });
    if (!res.ok) {
      let msg = `${res.status} ${res.statusText}`;
      try {
        const j = await res.json();
        msg = j.error ?? msg;
      } catch { /* not JSON */ }
      throw new Error(msg);
    }
    return res.json();
  },
};
