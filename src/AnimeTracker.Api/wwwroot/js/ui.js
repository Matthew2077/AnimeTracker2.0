// Crea elementi DOM senza innerHTML: i testi vengono sempre inseriti come testo (niente XSS)
export function h(tag, attrs, ...children) {
  const el = document.createElement(tag);
  const a = attrs ?? {};
  for (const [k, v] of Object.entries(a)) {
    if (v == null || v === false || k === 'value') continue;
    if (k === 'class') el.className = v;
    else if (k.startsWith('on')) el.addEventListener(k.slice(2).toLowerCase(), v);
    else el.setAttribute(k, v === true ? '' : v);
  }
  for (const c of children.flat(Infinity)) {
    if (c == null || c === false) continue;
    el.append(c instanceof Node ? c : String(c));
  }
  if ('value' in a) el.value = a.value ?? '';   // dopo le option, per le select
  return el;
}

const PLACEHOLDER = 'data:image/svg+xml;utf8,' + encodeURIComponent(
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 450"><rect width="300" height="450" fill="#20242d"/>' +
  '<text x="150" y="240" font-size="64" text-anchor="middle" fill="#4b5365" font-family="sans-serif">?</text></svg>');

export function img(file, alt = '', cls = '') {
  const el = h('img', { class: cls, src: file ? `/uploads/${encodeURIComponent(file)}` : PLACEHOLDER, alt, loading: 'lazy' });
  el.addEventListener('error', () => { el.src = PLACEHOLDER; }, { once: true });
  return el;
}

const LABELS = {
  Ova: 'OVA', LightNovel: 'Light novel', Episodes: 'episodes', Chapters: 'chapters', Volumes: 'volumes',
};
export const label = (k) => LABELS[k] ?? k;
export const GROUPS = { Anime: 'Anime', Manga: 'Manga', LightNovel: 'Light Novels' };
export const STATUSES = ['Planned', 'Ongoing', 'Completed', 'Dropped'];

export const badge = (text, cls = '') => h('span', { class: `badge ${cls}` }, text);
export const statusBadge = (s) => badge(s, `st-${s.toLowerCase()}`);

export function progress(watched, total) {
  const pct = total > 0 ? Math.min(100, Math.round((watched / total) * 100)) : 0;
  return h('div', { class: 'bar' }, h('div', { class: 'bar-fill', style: `width:${pct}%` }));
}
