/* Supported rich text shared by paste and loading. No page layout is stored here. */
window.ZzzoObsah = (() => {
  const blocks = new Set(['P', 'DIV', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'BLOCKQUOTE', 'PRE', 'SECTION', 'ARTICLE']);
  const allowed = new Set(['P', 'DIV', 'BR', 'STRONG', 'EM', 'U', 'S', 'SUB', 'SUP', 'UL', 'OL', 'LI',
    'TABLE', 'THEAD', 'TBODY', 'TFOOT', 'TR', 'TD', 'TH', 'COLGROUP', 'COL', 'A', 'IMG',
    'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'BLOCKQUOTE', 'HR']);
  const aliases = { B: 'strong', I: 'em', STRIKE: 's', DEL: 's' };
  const length = value => /^(?:\d+(?:\.\d+)?)(?:%|px|pt|cm|mm|in)?$/i.test(value || '') && parseFloat(value) > 0;

  function wordLists(parent, formats) {
    let stack = [], current = '';
    for (const p of [...parent.children]) {
      // Chromium's CSSOM discards Office-only properties; read this one before
      // removing the source attributes rather than asking CSSStyleDeclaration.
      const declaration = /(?:^|;)\s*mso-list\s*:\s*([^;]+)/i.exec(p.getAttribute('style') || '')?.[1] || '';
      const level = /\blevel(\d+)\b/i.exec(declaration);
      if (p.tagName !== 'P' || !level) { stack = []; current = ''; wordLists(p, formats); continue; }
      const listId = /\bl\d+\b/.exec(declaration)?.[0] || 'l0';
      const key = listId + ':' + (/\blfo\d+\b/.exec(declaration)?.[0] || '');
      const marker = [...p.querySelectorAll('[style]')].find(n => /mso-list\s*:\s*ignore/i.test(n.getAttribute('style')));
      const label = (marker?.textContent || '').trim();
      const format = formats[listId + ':' + level[1]];
      const ordered = format ? format !== 'bullet' : /^(?:\d+|[a-z]+)[.)]\s*$/i.test(label);
      const tag = ordered ? 'OL' : 'UL';
      if (key !== current) stack = [];
      current = key;
      const depth = Math.min(Math.max(1, Number(level[1])), stack.length + 1, 9);
      while (stack.length > depth) stack.pop();
      if (stack.length === depth && stack[depth - 1].tagName !== tag) stack.pop();
      if (stack.length < depth) {
        const list = document.createElement(tag);
        const types = { 'alpha-lower': 'a', 'alpha-upper': 'A', 'roman-lower': 'i', 'roman-upper': 'I' };
        if (ordered) {
          const type = types[format] || (/^[a-z][.)]$/.test(label) ? 'a' : /^[A-Z][.)]$/.test(label) ? 'A' : '1');
          if (type !== '1') list.setAttribute('type', type);
          const start = /^(\d+)/.exec(label);
          if (start && Number(start[1]) > 1) list.setAttribute('start', start[1]);
        }
        if (stack.length) stack[stack.length - 1].lastElementChild.append(list);
        else parent.insertBefore(list, p);
        stack.push(list);
      }
      marker?.remove();
      const li = document.createElement('li');
      li.append(p);
      stack[stack.length - 1].append(li);
    }
  }

  function ocistit(html, vlozeni = false) {
    const template = document.createElement('template');
    template.innerHTML = html || '';
    const root = template.content;
    const rules = [], formats = {};
    for (const style of root.querySelectorAll('style')) {
      for (const match of style.textContent.matchAll(/@list\s+(l\d+):level(\d+)\s*\{([^}]+)\}/gi)) {
        const format = /mso-level-number-format\s*:\s*([\w-]+)/i.exec(match[3]);
        if (format) formats[match[1] + ':' + match[2]] = format[1].toLowerCase();
      }
      try {
        const sheet = new CSSStyleSheet(); sheet.replaceSync(style.textContent);
        for (const rule of sheet.cssRules) if (rule.selectorText) rules.push(rule);
      } catch { /* Invalid Office-only CSS has no presentation authority. */ }
    }
    if (vlozeni) wordLists(root, formats);
    for (const col of [...root.querySelectorAll('col[span]')]) {
      const span = Math.min(63, Math.max(1, parseInt(col.getAttribute('span'), 10) || 1));
      col.removeAttribute('span');
      for (let i = 1; i < span; i++) col.before(col.cloneNode(true));
    }
    function property(node, name) {
      const inline = node.style?.getPropertyValue(name);
      if (inline) return inline.trim().toLowerCase();
      for (let i = rules.length - 1; i >= 0; i--) {
        try {
          const value = rules[i].style.getPropertyValue(name);
          if (value && node.matches(rules[i].selectorText)) return value.trim().toLowerCase();
        } catch { /* Ignore selectors unsupported by this browser. */ }
      }
      return '';
    }
    function visit(node) {
      if (node.nodeType === Node.COMMENT_NODE) { node.remove(); return; }
      if (node.nodeType !== Node.ELEMENT_NODE) return;
      if (/^(SCRIPT|STYLE|META|LINK|TITLE|IFRAME|OBJECT|EMBED|SVG|MATH|INPUT|BUTTON|NOSCRIPT)$/.test(node.tagName)) { node.remove(); return; }
      const tag = node.tagName;
      const marks = [];
      const weight = property(node, 'font-weight');
      if (weight === 'bold' || Number(weight) >= 600 || vlozeni && /^H[1-6]$/.test(tag)) marks.push('strong');
      if (/^(italic|oblique)/.test(property(node, 'font-style'))) marks.push('em');
      const decoration = property(node, 'text-decoration') + ' ' + property(node, 'text-decoration-line');
      if (decoration.includes('underline')) marks.push('u');
      if (decoration.includes('line-through')) marks.push('s');
      const vertical = property(node, 'vertical-align');
      if (vertical === 'super') marks.push('sup');
      if (vertical === 'sub') marks.push('sub');
      const width = property(node, 'width') || node.getAttribute('width') || '';
      const align = property(node, 'text-align') || node.getAttribute('align') || '';
      const listStyle = property(node, 'list-style-type');
      const attributes = {};
      if (tag === 'A' && /^(https?:|mailto:)/i.test((node.getAttribute('href') || '').trim())) attributes.href = node.getAttribute('href');
      if (tag === 'IMG') {
        const src = (node.getAttribute('src') || '').trim();
        if (!/^(https?:|data:image\/(?:png|jpeg|gif|webp|bmp);base64,)/i.test(src)) { node.replaceWith(document.createTextNode(node.getAttribute('alt') || '')); return; }
        attributes.src = src;
        for (const name of ['alt', 'width', 'height']) if (node.hasAttribute(name)) attributes[name] = node.getAttribute(name);
        for (const name of ['width', 'height']) {
          const size = /^(\d+(?:\.\d+)?)(px|pt|cm|mm|in)?$/i.exec(property(node, name));
          if (size) attributes[name] = String(parseFloat(size[1]) * ({ pt: 96 / 72, cm: 96 / 2.54, mm: 96 / 25.4, in: 96 })[size[2]?.toLowerCase()] || parseFloat(size[1]));
        }
      }
      if (tag === 'TD' || tag === 'TH') for (const name of ['colspan', 'rowspan']) {
        const value = Math.min(100, Math.max(1, parseInt(node.getAttribute(name), 10) || 1));
        if (value > 1) attributes[name] = String(value);
      }
      if (tag === 'OL') {
        const type = node.getAttribute('type') || ({ 'lower-alpha': 'a', 'upper-alpha': 'A', 'lower-roman': 'i', 'upper-roman': 'I' })[listStyle];
        if (/^[1aAiI]$/.test(type || '')) attributes.type = type;
        const start = parseInt(node.getAttribute('start'), 10);
        if (Number.isFinite(start) && start !== 1) attributes.start = String(start);
      }
      const styles = [];
      if (['TABLE', 'COL', 'TD', 'TH'].includes(tag) && length(width)) styles.push('width:' + (/^[\d.]+$/.test(width) ? width + 'px' : width));
      if ((blocks.has(tag) || ['LI', 'TD', 'TH'].includes(tag)) && /^(left|center|right|justify)$/.test(align)) styles.push('text-align:' + align);
      if (styles.length) attributes.style = styles.join(';');
      if (tag === 'PRE' && vlozeni) {
        const lines = node.textContent.split(/\r?\n/); node.replaceChildren();
        lines.forEach((line, i) => { if (i) node.append(document.createElement('br')); node.append(document.createTextNode(line)); });
      }
      for (const child of [...node.childNodes]) visit(child);
      for (const attribute of [...node.attributes]) node.removeAttribute(attribute.name);
      for (const [name, value] of Object.entries(attributes)) node.setAttribute(name, value);
      let replacement = aliases[tag];
      if (vlozeni && blocks.has(tag) && tag !== 'P') replacement = node.querySelector('p,div,table,ul,ol') ? null : 'p';
      if (replacement) {
        const clean = document.createElement(replacement);
        for (const attribute of [...node.attributes]) clean.setAttribute(attribute.name, attribute.value);
        clean.append(...node.childNodes); node.replaceWith(clean); node = clean;
      }
      for (const mark of marks) {
        const walker = document.createTreeWalker(node, NodeFilter.SHOW_TEXT), texts = [];
        while (walker.nextNode()) texts.push(walker.currentNode);
        for (const text of texts) {
          if (!text.textContent.trim() || text.parentElement.closest(mark)) continue;
          const wrapper = document.createElement(mark); text.replaceWith(wrapper); wrapper.append(text);
        }
      }
      if (!allowed.has(node.tagName) || node.tagName === 'A' && !node.hasAttribute('href') ||
          vlozeni && ['DIV', 'SECTION', 'ARTICLE', 'BLOCKQUOTE', 'PRE'].includes(node.tagName)) node.replaceWith(...node.childNodes);
    }
    for (const node of [...root.childNodes]) visit(node);
    return template.innerHTML;
  }
  return { ocistit };
})();
