const editorId = 'tinymce-editor';
let verzeObsahu = 0, poradiZpravy = 0, nastavuji = false;

function vlozitTabulku(editor) {
  editor.windowManager.open({
    title: 'Vložit tabulku',
    body: { type: 'panel', items: [{ type: 'input', name: 'columns', label: 'Sloupce (1–12)' }, { type: 'input', name: 'rows', label: 'Řádky (1–100)' }] },
    initialData: { columns: '2', rows: '2' },
    buttons: [{ type: 'cancel', text: 'Zrušit' }, { type: 'submit', text: 'Vložit', primary: true }],
    onSubmit: api => {
      const data = api.getData(), columns = Number(data.columns), rows = Number(data.rows);
      if (!Number.isInteger(columns) || columns < 1 || columns > 12 || !Number.isInteger(rows) || rows < 1 || rows > 100) {
        editor.windowManager.alert('Zadejte 1 až 12 sloupců a 1 až 100 řádků.'); return;
      }
      editor.execCommand('mceInsertTable', false, { rows, columns }); api.close();
    }
  });
}

tinymce.init({
  selector: '#' + editorId, language: 'cs', entity_encoding: 'raw', browser_spellcheck: true,
  license_key: 'gpl', promotion: false, branding: false, statusbar: false, resize: false,
  plugins: 'lists table link', menubar: false, contextmenu: false, visual: false,
  toolbar: 'undo redo | bold italic underline strikethrough subscript superscript | alignleft aligncenter alignright alignjustify | bullist numlist | zzzoTable link removeformat',
  toolbar_mode: 'sliding', width: '100%', height: '100vh', min_height: 0, content_css: 'content.css',
  table_use_colgroups: true, table_sizing_mode: 'relative', table_column_resizing: 'preservetable',
  table_default_attributes: {}, table_default_styles: { width: '100%' }, object_resizing: 'table',
  table_advtab: false, table_row_advtab: false, table_cell_advtab: false, table_appearance_options: false,
  table_toolbar: 'tableinsertrowbefore tableinsertrowafter tabledeleterow | tableinsertcolbefore tableinsertcolafter tabledeletecol | tablemergecells tablesplitcells tabledelete',
  paste_block_drop: true, paste_data_images: false, paste_merge_formats: true,
  paste_preprocess: (_, args) => { args.content = ZzzoObsah.ocistit(args.content, true); },
  valid_elements: 'p[style],div[style],br,strong/b,em/i,u,s/strike/del,sub,sup,ul,ol[start|type],li[style],table[style],thead,tbody,tfoot,tr,td[colspan|rowspan|style],th[colspan|rowspan|style],colgroup,col[style|width],a[href],img[src|alt|width|height],h1[style],h2[style],h3[style],h4[style],h5[style],h6[style],blockquote,hr',
  valid_styles: Object.assign(Object.fromEntries(['p', 'div', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'].map(tag => [tag, 'text-align'])),
    { table: 'width', col: 'width', td: 'width,text-align', th: 'width,text-align' }),
  formats: { bold: { inline: 'strong' }, italic: { inline: 'em' }, underline: { inline: 'u', exact: true },
    strikethrough: { inline: 's', exact: true }, superscript: { inline: 'sup', exact: true }, subscript: { inline: 'sub', exact: true } },
  setup: editor => {
    const cell = () => editor.dom.getParent(editor.selection.getStart(), 'td,th');
    const action = (text, command, enabled) => ({ type: 'menuitem', text, onAction: () => editor.execCommand(command),
      onSetup: api => { api.setEnabled(!!enabled()); return () => {}; } });
    editor.ui.registry.addMenuButton('zzzoTable', { icon: 'table', tooltip: 'Tabulka', fetch: callback => callback([
      { type: 'menuitem', text: 'Vložit tabulku…', onAction: () => vlozitTabulku(editor), onSetup: api => { api.setEnabled(!cell()); return () => {}; } },
      { type: 'separator' },
      action('Vložit řádek před', 'mceTableInsertRowBefore', cell), action('Vložit řádek za', 'mceTableInsertRowAfter', cell),
      action('Odstranit řádek', 'mceTableDeleteRow', cell),
      action('Vložit sloupec před', 'mceTableInsertColBefore', cell), action('Vložit sloupec za', 'mceTableInsertColAfter', cell),
      action('Odstranit sloupec', 'mceTableDeleteCol', cell), { type: 'separator' },
      action('Sloučit buňky', 'mceTableMergeCells', () => editor.getBody().querySelectorAll('td[data-mce-selected],th[data-mce-selected]').length > 1),
      action('Rozdělit buňku', 'mceTableSplitCells', () => cell() && (cell().colSpan > 1 || cell().rowSpan > 1)),
      action('Odstranit tabulku', 'mceTableDelete', cell)
    ]) });
    editor.on('TableModified ObjectResized', () => {
      for (const node of editor.getBody().querySelectorAll('table,tr,td,th')) editor.dom.setStyle(node, 'height', null);
    });
    editor.on('init', () => CefSharp.PostMessage(JSON.stringify({ druh: 'pripraven' })));
    editor.on('input change undo redo TableModified ObjectResized', () => { if (!nastavuji) CefSharp.PostMessage(getEditorContent()); });
  }
});

function getEditorContent() {
  return JSON.stringify({ druh: 'obsah', verze: verzeObsahu, poradi: ++poradiZpravy, html: tinymce.get(editorId).getContent() });
}
function setEditorContent(content, verze) {
  nastavuji = true;
  try {
    const editor = tinymce.get(editorId); verzeObsahu = verze;
    editor.setContent(ZzzoObsah.ocistit(content || '')); editor.undoManager.clear(); editor.setDirty(false);
  } finally { nastavuji = false; }
}
function setEditorAppearance(css) {
  const doc = tinymce.get(editorId).getDoc();
  let style = doc.getElementById('zzzo-template');
  if (!style) { style = doc.createElement('style'); style.id = 'zzzo-template'; doc.head.append(style); }
  style.textContent = css;
}
