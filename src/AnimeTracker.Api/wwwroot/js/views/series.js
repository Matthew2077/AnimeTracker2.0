import { api } from '../api.js';
import { h, img, progress, badge } from '../ui.js';
import { openCreateChooser, openCreateSeriesModal, openCreateEntryModal } from '../modals.js';

const KEY = 'at.seriesFilters';
const filters = loadFilters();

function loadFilters() {
  const base = { q: '', genreId: '', sort: 'asc' };
  try {
    const raw = { ...base, ...JSON.parse(sessionStorage.getItem(KEY)) };
    // drop legacy status so qs() cannot keep a ghost filter active
    const { status: _drop, ...rest } = raw;
    return { ...base, q: rest.q ?? '', genreId: rest.genreId != null ? String(rest.genreId) : '', sort: rest.sort || 'asc' };
  } catch {
    return base;
  }
}
function saveFilters() {
  try { sessionStorage.setItem(KEY, JSON.stringify({ q: filters.q, genreId: filters.genreId, sort: filters.sort })); } catch { /* ignore */ }
}

function seriesCard(s) {
  let info;
  if (s.entryCount === 0) {
    info = h('p', { class: 'muted small' }, 'No entries yet');
  } else if (s.animeTotal > 0) {
    info = [
      h('p', { class: 'small' }, `${s.animeWatched}/${s.animeTotal} episodes`),
      progress(s.animeWatched, s.animeTotal),
      (s.hasManga || s.hasLightNovel) &&
        h('div', { class: 'badges' }, s.hasManga && badge('Manga'), s.hasLightNovel && badge('Light novel')),
    ];
  } else {
    info = [
      h('p', { class: 'small' }, `${s.otherWatched}/${s.otherTotal} read`),
      progress(s.otherWatched, s.otherTotal),
    ];
  }
  return h('a', { class: 'card', href: `#/series/${s.id}` },
    img(s.coverFile, s.name),
    h('div', { class: 'card-body' },
      h('h3', {}, s.name),
      h('p', { class: 'muted small' },
        [s.genreName, `${s.entryCount} ${s.entryCount === 1 ? 'entry' : 'entries'}`].filter(Boolean).join(' · ')),
      info));
}

const stat = (name, n) => h('div', { class: 'stat' }, h('b', {}, n), h('span', { class: 'muted' }, name));

export async function seriesView() {
  const [stats, genres] = await Promise.all([api.stats(), api.genres()]);
  saveFilters(); // persist migrated shape without status
  const grid = h('div', { class: 'grid' });
  let timer;
  let seq = 0;

  async function load() {
    const mine = ++seq;
    try {
      const list = await api.seriesList(filters);
      if (mine !== seq) return;
      grid.replaceChildren(...(list.length ? list.map(seriesCard) : [h('p', { class: 'muted' }, 'No series found.')]));
    } catch (err) {
      grid.replaceChildren(h('p', { class: 'error' }, err.message));
    }
  }

  const controls = h('div', { class: 'controls' },
    h('input', {
      type: 'search', placeholder: 'Search series or entries…', value: filters.q,
      onInput: (e) => { filters.q = e.target.value; saveFilters(); clearTimeout(timer); timer = setTimeout(load, 250); },
    }),
    h('select', {
      value: filters.genreId,
      onChange: (e) => { filters.genreId = e.target.value; saveFilters(); load(); },
    },
      h('option', { value: '' }, 'All genres'),
      genres.map((g) => h('option', { value: String(g.id) }, g.name))),
    h('select', { value: filters.sort, onChange: (e) => { filters.sort = e.target.value; saveFilters(); load(); } },
      h('option', { value: 'asc' }, 'A → Z'), h('option', { value: 'desc' }, 'Z → A')),
    h('button', {
      class: 'btn',
      type: 'button',
      onClick: async () => {
        try {
          const s = await api.randomSeries();
          location.hash = `#/series/${s.id}`;
        } catch (err) {
          alert(/\b404\b/.test(err.message) ? 'No series in the library yet.' : err.message);
        }
      },
    }, 'Random'),
    h('button', {
      class: 'btn primary',
      type: 'button',
      title: 'Add series or entry',
      onClick: () => {
        openCreateChooser({
          onSeries: () => openCreateSeriesModal({
            genres,
            onCreated: (s) => { location.hash = `#/series/${s.id}`; },
          }),
          onEntry: async () => {
            let list = [];
            try {
              list = await api.seriesList({ sort: 'asc' });
            } catch (err) {
              alert(err.message);
              return;
            }
            openCreateEntryModal({
              genres,
              seriesList: list,
              onCreated: (e) => { location.hash = `#/series/${e.seriesId}`; },
            });
          },
        });
      },
    }, '+'));

  await load();
  return h('div', {},
    h('h1', {}, 'My Library'),
    h('div', { class: 'stats' },
      stat('Completed', stats.completed), stat('Ongoing', stats.ongoing),
      stat('Dropped', stats.dropped), stat('Planned', stats.planned)),
    controls, grid);
}
