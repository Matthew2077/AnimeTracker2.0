import { api } from '../api.js';
import { h, img, progress, badge, statusBadge, label, GROUPS, STATUSES } from '../ui.js';
import { openCreateEntryModal, openEditEntryModal } from '../modals.js';

export async function detailView(id) {
  const [initialSeries, initial, genres] = await Promise.all([api.series(id), api.entries(id), api.genres()]);
  let series = initialSeries;
  let list = initial;
  let editing = false;
  const head = h('div', { class: 'detail-head' });
  const sections = h('div');

  const act = async (fn) => {
    try { await fn(); } catch (err) { alert(err.message); render(); }
  };
  const replace = (upd) => { list = list.map((x) => (x.id === upd.id ? upd : x)); render(); };

  async function move(entry, items, index, delta) {
    const ids = items.map((x) => x.id);
    [ids[index], ids[index + delta]] = [ids[index + delta], ids[index]];
    await api.reorder(id, { group: entry.mediaGroup, orderedIds: ids });
    list = await api.entries(id);
    render();
  }

  async function refreshSeries() {
    const [s, entries] = await Promise.all([api.series(id), api.entries(id)]);
    series = s;
    list = entries;
    renderHead();
    render();
  }

  function openEditEntry(e) {
    act(async () => {
      const allSeries = await api.seriesList({ sort: 'asc' });
      openEditEntryModal({
        entry: e,
        genres,
        seriesList: allSeries,
        onSaved: async (updated) => {
          if (updated.seriesId !== id) {
            location.hash = `#/series/${updated.seriesId}`;
            return;
          }
          try {
            await refreshSeries();
          } catch (err) {
            alert(err.message);
          }
        },
      });
    });
  }

  function entryRow(e, i, items) {
    return h('div', { class: 'entry' },
      img(e.imageFile, e.title),
      h('div', {},
        h('h3', {}, e.title, e.label && h('span', { class: 'muted' }, ` · ${e.label}`)),
        h('div', { class: 'badges' }, badge(label(e.mediaType)), statusBadge(e.status),
          e.tags.map((t) => h('span', { class: 'tag' }, t.name))),
        h('p', { class: 'small' }, `${e.watchedUnits}/${e.totalUnits} ${label(e.unitLabel)}`),
        progress(e.watchedUnits, e.totalUnits)),
      h('div', { class: 'actions' },
        h('button', { class: 'btn', type: 'button', onClick: () => openEditEntry(e) }, 'Edit'),
        e.externalUrl && h('a', { class: 'btn primary', href: e.externalUrl, target: '_blank', rel: 'noopener noreferrer' }, 'Open'),
        e.status === 'Ongoing' && e.watchedUnits > 0 &&
          h('button', { class: 'btn', onClick: () => act(async () => replace(await api.decrement(e.id))) }, '−1'),
        e.status !== 'Completed' &&
          h('button', { class: 'btn', onClick: () => act(async () => replace(await api.increment(e.id))) }, '+1'),
        h('select', {
          value: e.status,
          onChange: (ev) => act(async () => {
            const to = ev.target.value;
            const resets = (e.status === 'Completed' && to !== 'Completed') || (to === 'Planned' && e.watchedUnits > 0);
            if (resets && !confirm('This will reset watched units to 0. Continue?')) { ev.target.value = e.status; return; }
            replace(await api.setStatus(e.id, to));
          }),
        }, STATUSES.map((s) => h('option', { value: s }, s))),
        h('button', { class: 'btn', title: 'Move up', disabled: i === 0, onClick: () => act(() => move(e, items, i, -1)) }, '▲'),
        h('button', { class: 'btn', title: 'Move down', disabled: i === items.length - 1, onClick: () => act(() => move(e, items, i, 1)) }, '▼')));
  }

  function openAddEntry() {
    openCreateEntryModal({
      genres,
      seriesList: [{ id, name: series.name }],
      preselectedSeriesId: id,
      onCreated: async () => {
        try {
          await refreshSeries();
        } catch (err) {
          alert(err.message);
        }
      },
    });
  }

  function renderEntries() {
    const addBtn = h('button', {
      class: 'btn primary', type: 'button',
      onClick: openAddEntry,
    }, 'Add entry');
    const blocks = Object.keys(GROUPS).map((g) => {
      const items = list.filter((e) => e.mediaGroup === g).sort((a, b) => a.sortOrder - b.sortOrder);
      if (!items.length) return null;
      return h('section', {}, h('h2', {}, GROUPS[g]), h('div', { class: 'entries' }, items.map((e, i) => entryRow(e, i, items))));
    }).filter(Boolean);
    const body = blocks.length
      ? blocks
      : [h('p', { class: 'muted' }, 'No entries yet.')];
    sections.replaceChildren(
      h('div', { class: 'section-head' }, addBtn),
      ...body);
  }

  function renderHead() {
    if (editing) {
      const nameInput = h('input', { type: 'text', required: true, value: series.name });
      const genreSelect = h('select', { value: series.genreId != null ? String(series.genreId) : '' },
        h('option', { value: '' }, 'No genre'),
        genres.map((g) => h('option', { value: String(g.id) }, g.name)));
      const coverInput = h('input', { type: 'file', accept: 'image/jpeg,image/png,image/webp,image/gif' });
      const notesInput = h('textarea', { rows: 3 }, series.notes ?? '');
      const errEl = h('p', { class: 'error', style: 'display:none' });
      const saveBtn = h('button', { class: 'btn primary', type: 'submit' }, 'Save');
      const cancelBtn = h('button', {
        class: 'btn', type: 'button',
        onClick: () => { editing = false; renderHead(); },
      }, 'Cancel');

      const form = h('form', {
        class: 'form',
        onSubmit: async (ev) => {
          ev.preventDefault();
          errEl.style.display = 'none';
          saveBtn.disabled = true;
          try {
            let coverFile = series.coverFile ?? null;
            const file = coverInput.files?.[0];
            if (file) {
              const up = await api.upload(file);
              coverFile = up.fileName;
            }
            const genreRaw = genreSelect.value;
            series = await api.updateSeries(id, {
              name: nameInput.value.trim(),
              genreId: genreRaw ? Number(genreRaw) : null,
              coverFile,
              notes: notesInput.value.trim() || null,
            });
            editing = false;
            renderHead();
          } catch (err) {
            errEl.textContent = err.message;
            errEl.style.display = '';
          } finally {
            saveBtn.disabled = false;
          }
        },
      },
        h('label', { class: 'field' }, h('span', {}, 'Name'), nameInput),
        h('label', { class: 'field' }, h('span', {}, 'Genre'), genreSelect),
        h('label', { class: 'field' },
          h('span', {}, 'Cover'),
          coverInput,
          series.coverFile && h('span', { class: 'muted small' }, `Current: ${series.coverFile} (leave empty to keep)`)),
        h('label', { class: 'field' }, h('span', {}, 'Notes'), notesInput),
        errEl,
        h('div', { class: 'actions' }, saveBtn, cancelBtn));

      head.replaceChildren(img(series.coverFile, series.name, 'cover'), form);
      return;
    }

    const s = series.stats;
    head.replaceChildren(
      img(series.coverFile, series.name, 'cover'),
      h('div', {},
        h('div', { class: 'detail-title' },
          h('h1', {}, series.name),
          h('button', {
            class: 'btn', type: 'button',
            onClick: () => { editing = true; renderHead(); },
          }, 'Edit')),
        series.genreName && h('p', { class: 'muted' }, series.genreName),
        h('div', { class: 'badges' },
          badge(`${s.completed} completed`, 'st-completed'), badge(`${s.ongoing} ongoing`, 'st-ongoing'),
          badge(`${s.dropped} dropped`, 'st-dropped'), badge(`${s.planned} planned`, 'st-planned')),
        series.notes && h('p', {}, series.notes)));
  }

  function render() {
    renderEntries();
  }

  renderHead();
  render();
  return h('div', {},
    h('a', { class: 'back', href: '#/' }, '← Back to library'),
    head,
    sections);
}
