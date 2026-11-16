import { api } from './api.js';
import { h, label, STATUSES } from './ui.js';

const MEDIA_TYPES = ['Anime', 'Film', 'Ova', 'Manga', 'LightNovel'];
const UNIT_LABELS = ['Episodes', 'Chapters', 'Volumes'];

function defaultUnitLabel(mediaType) {
  if (mediaType === 'Manga') return 'Chapters';
  if (mediaType === 'LightNovel') return 'Volumes';
  return 'Episodes';
}

/** @returns {{ close: () => void, setBody: (node: Node) => void, setTitle: (t: string) => void }} */
export function openModal({ title, body }) {
  const titleEl = h('h2', { class: 'modal-title' }, title);
  const bodyEl = h('div', { class: 'modal-body' }, body);
  const panel = h('div', {
    class: 'modal-panel',
    role: 'dialog',
    'aria-modal': 'true',
    onClick: (e) => e.stopPropagation(),
  }, titleEl, bodyEl);

  const overlay = h('div', {
    class: 'modal-overlay',
    onClick: () => close(),
  }, panel);

  const onKey = (e) => {
    if (e.key === 'Escape') close();
  };

  function close() {
    document.removeEventListener('keydown', onKey);
    overlay.remove();
  }

  document.addEventListener('keydown', onKey);
  document.body.append(overlay);

  const focusable = panel.querySelector('input, select, textarea, button');
  if (focusable) focusable.focus();

  return {
    close,
    setBody: (node) => { bodyEl.replaceChildren(node); },
    setTitle: (t) => { titleEl.textContent = t; },
  };
}

export function openCreateChooser({ onSeries, onEntry }) {
  const modal = openModal({
    title: 'Add to library',
    body: h('div', { class: 'chooser' },
      h('button', {
        class: 'btn primary', type: 'button',
        onClick: () => { modal.close(); onSeries(); },
      }, 'New series'),
      h('button', {
        class: 'btn primary', type: 'button',
        onClick: () => { modal.close(); onEntry(); },
      }, 'New entry'),
      h('button', {
        class: 'btn', type: 'button',
        onClick: () => modal.close(),
      }, 'Cancel')),
  });
  return modal;
}

export function openCreateSeriesModal({ genres, onCreated }) {
  const nameInput = h('input', { type: 'text', required: true, placeholder: 'Series name' });
  const genreSelect = h('select', {},
    h('option', { value: '' }, 'No genre'),
    genres.map((g) => h('option', { value: String(g.id) }, g.name)));
  const coverInput = h('input', { type: 'file', accept: 'image/jpeg,image/png,image/webp,image/gif' });
  const notesInput = h('textarea', { rows: 3, placeholder: 'Optional notes' });
  const errEl = h('p', { class: 'error', style: 'display:none' });
  const saveBtn = h('button', { class: 'btn primary', type: 'submit' }, 'Create');
  const cancelBtn = h('button', { class: 'btn', type: 'button' }, 'Cancel');

  const form = h('form', {
    class: 'form',
    onSubmit: async (ev) => {
      ev.preventDefault();
      errEl.style.display = 'none';
      const name = nameInput.value.trim();
      if (!name) {
        errEl.textContent = 'Name is required.';
        errEl.style.display = '';
        return;
      }
      saveBtn.disabled = true;
      try {
        let coverFile = null;
        const file = coverInput.files?.[0];
        if (file) {
          const up = await api.upload(file);
          coverFile = up.fileName;
        }
        const genreRaw = genreSelect.value;
        const series = await api.createSeries({
          name,
          genreId: genreRaw ? Number(genreRaw) : null,
          coverFile,
          notes: notesInput.value.trim() || null,
        });
        modal.close();
        onCreated?.(series);
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
    h('label', { class: 'field' }, h('span', {}, 'Cover'), coverInput),
    h('label', { class: 'field' }, h('span', {}, 'Notes'), notesInput),
    errEl,
    h('div', { class: 'actions' }, saveBtn, cancelBtn));

  const modal = openModal({ title: 'New series', body: form });
  cancelBtn.addEventListener('click', () => modal.close());
  return modal;
}

export function openCreateEntryModal({ genres, seriesList, preselectedSeriesId, onCreated }) {
  const locked = preselectedSeriesId != null;
  let mode = 'existing';

  const seriesSelect = h('select', {},
    h('option', { value: '' }, 'Select a series…'),
    (seriesList ?? []).map((s) => h('option', { value: String(s.id) }, s.name)));
  if (locked) {
    seriesSelect.value = String(preselectedSeriesId);
    seriesSelect.disabled = true;
  }

  const newSeriesName = h('input', { type: 'text', placeholder: 'New series name' });
  const newSeriesGenre = h('select', {},
    h('option', { value: '' }, 'No genre'),
    (genres ?? []).map((g) => h('option', { value: String(g.id) }, g.name)));
  const newSeriesFields = h('div', { class: 'nested-fields', style: 'display:none' },
    h('label', { class: 'field' }, h('span', {}, 'Series name'), newSeriesName),
    h('label', { class: 'field' }, h('span', {}, 'Genre'), newSeriesGenre));

  const existingFields = h('div', {},
    h('label', { class: 'field' }, h('span', {}, 'Series'), seriesSelect));

  const modeExistingBtn = locked ? null : h('button', { class: 'btn active', type: 'button' }, 'Existing series');
  const modeNewBtn = locked ? null : h('button', { class: 'btn', type: 'button' }, 'New series');
  const modeRow = locked ? null : h('div', { class: 'mode-toggle' }, modeExistingBtn, modeNewBtn);

  function setMode(next) {
    mode = next;
    existingFields.style.display = next === 'existing' ? '' : 'none';
    newSeriesFields.style.display = next === 'new' ? '' : 'none';
    if (modeExistingBtn) modeExistingBtn.classList.toggle('active', next === 'existing');
    if (modeNewBtn) modeNewBtn.classList.toggle('active', next === 'new');
  }

  if (modeExistingBtn) modeExistingBtn.addEventListener('click', () => setMode('existing'));
  if (modeNewBtn) modeNewBtn.addEventListener('click', () => setMode('new'));

  const titleInput = h('input', { type: 'text', required: true, placeholder: 'Entry title' });
  const mediaSelect = h('select', {}, MEDIA_TYPES.map((t) => h('option', { value: t }, label(t))));
  const unitSelect = h('select', {}, UNIT_LABELS.map((u) => h('option', { value: u }, label(u))));
  unitSelect.value = defaultUnitLabel('Anime');
  mediaSelect.addEventListener('change', () => {
    unitSelect.value = defaultUnitLabel(mediaSelect.value);
  });

  const totalInput = h('input', { type: 'number', min: '1', step: '1', value: '1' });
  const watchedInput = h('input', { type: 'number', min: '0', step: '1', value: '0' });
  const statusSelect = h('select', {}, STATUSES.map((s) => h('option', { value: s }, s)));
  statusSelect.value = 'Planned';
  const labelInput = h('input', { type: 'text', placeholder: 'Optional label' });
  const urlInput = h('input', { type: 'url', placeholder: 'https://…' });
  const imageInput = h('input', { type: 'file', accept: 'image/jpeg,image/png,image/webp,image/gif' });
  const errEl = h('p', { class: 'error', style: 'display:none' });
  const saveBtn = h('button', { class: 'btn primary', type: 'submit' }, 'Create');
  const cancelBtn = h('button', { class: 'btn', type: 'button' }, 'Cancel');

  const form = h('form', {
    class: 'form',
    onSubmit: async (ev) => {
      ev.preventDefault();
      errEl.style.display = 'none';

      const title = titleInput.value.trim();
      if (!title) {
        errEl.textContent = 'Title is required.';
        errEl.style.display = '';
        return;
      }

      const totalUnits = Number(totalInput.value);
      const watchedUnits = Number(watchedInput.value);
      if (!Number.isFinite(totalUnits) || totalUnits <= 0) {
        errEl.textContent = 'Total units must be greater than 0.';
        errEl.style.display = '';
        return;
      }
      if (!Number.isFinite(watchedUnits) || watchedUnits < 0 || watchedUnits > totalUnits) {
        errEl.textContent = 'Watched units must be between 0 and total units.';
        errEl.style.display = '';
        return;
      }
      if (statusSelect.value === 'Completed' && watchedUnits !== totalUnits) {
        errEl.textContent = 'A completed entry must have watched units equal to total units.';
        errEl.style.display = '';
        return;
      }

      const externalUrl = urlInput.value.trim() || null;
      if (externalUrl) {
        try {
          const u = new URL(externalUrl);
          if (u.protocol !== 'http:' && u.protocol !== 'https:') throw new Error();
        } catch {
          errEl.textContent = 'Must be a valid http(s) URL.';
          errEl.style.display = '';
          return;
        }
      }

      saveBtn.disabled = true;
      try {
        let seriesId;
        if (mode === 'new') {
          const name = newSeriesName.value.trim();
          if (!name) {
            errEl.textContent = 'Series name is required.';
            errEl.style.display = '';
            saveBtn.disabled = false;
            return;
          }
          const genreRaw = newSeriesGenre.value;
          const created = await api.createSeries({
            name,
            genreId: genreRaw ? Number(genreRaw) : null,
            coverFile: null,
            notes: null,
          });
          seriesId = created.id;
        } else {
          const raw = seriesSelect.value;
          if (!raw) {
            errEl.textContent = 'Select a series.';
            errEl.style.display = '';
            saveBtn.disabled = false;
            return;
          }
          seriesId = Number(raw);
        }

        let imageFile = null;
        const file = imageInput.files?.[0];
        if (file) {
          const up = await api.upload(file);
          imageFile = up.fileName;
        }

        const entry = await api.createEntry({
          seriesId,
          title,
          mediaType: mediaSelect.value,
          label: labelInput.value.trim() || null,
          unitLabel: unitSelect.value,
          totalUnits,
          watchedUnits,
          status: statusSelect.value,
          externalUrl,
          imageFile,
          createdAt: null,
          completedAt: null,
          tags: null,
        });
        modal.close();
        onCreated?.(entry);
      } catch (err) {
        errEl.textContent = err.message;
        errEl.style.display = '';
      } finally {
        saveBtn.disabled = false;
      }
    },
  },
    modeRow,
    existingFields,
    newSeriesFields,
    h('label', { class: 'field' }, h('span', {}, 'Title'), titleInput),
    h('label', { class: 'field' }, h('span', {}, 'Media type'), mediaSelect),
    h('label', { class: 'field' }, h('span', {}, 'Unit label'), unitSelect),
    h('div', { class: 'field-row' },
      h('label', { class: 'field' }, h('span', {}, 'Total units'), totalInput),
      h('label', { class: 'field' }, h('span', {}, 'Watched units'), watchedInput)),
    h('label', { class: 'field' }, h('span', {}, 'Status'), statusSelect),
    h('label', { class: 'field' }, h('span', {}, 'Label'), labelInput),
    h('label', { class: 'field' }, h('span', {}, 'External URL'), urlInput),
    h('label', { class: 'field' }, h('span', {}, 'Image'), imageInput),
    errEl,
    h('div', { class: 'actions' }, saveBtn, cancelBtn));

  const modal = openModal({ title: 'New entry', body: form });
  cancelBtn.addEventListener('click', () => modal.close());
  setMode('existing');
  return modal;
}
