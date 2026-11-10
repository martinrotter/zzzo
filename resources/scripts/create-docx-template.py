"""Rebuild the editable template. Requires python-docx; no network access.

The PDF references and the former svesedlice.css define the layout.
Fonts are distributed under the adjacent SIL Open Font License.
"""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from uuid import UUID
from lxml import etree as E
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT, WD_ROW_HEIGHT_RULE
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / 'resources/templates/svésedlice.docx'
W = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
R = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
PKG = 'http://schemas.openxmlformats.org/package/2006/relationships'


def prop(parent, tag, **attrs):
    element = OxmlElement('w:' + tag)
    for key, value in attrs.items():
        element.set(qn('w:' + key), str(value))
    parent.append(element)
    return element


doc = Document()
section = doc.sections[0]
section.page_width, section.page_height = Cm(21), Cm(29.7)
section.top_margin = section.bottom_margin = Cm(2.2)
section.left_margin = section.right_margin = Cm(1.45)
section.footer_distance = Cm(0.15)
normal = doc.styles['Normal']
normal.font.name = 'EB Garamond'
normal.font.size = Pt(11)
normal.font.color.rgb = RGBColor(0, 0, 0)
normal.paragraph_format.line_spacing = Pt(12.1)
normal.paragraph_format.space_after = Pt(8.5)
prop(normal.element.get_or_add_rPr(), 'lang', val='cs-CZ')

for name, size, before, after in [('Title', 22, 27.5, 14.7), ('Heading 1', 16.5, 20.625, 13.7), ('Heading 2', 12.87, 16.1, 12.87)]:
    style = doc.styles[name]
    style.font.name = 'EB Garamond'
    style.font.size = Pt(size)
    style.font.bold = True
    style.font.color.rgb = RGBColor(0, 0, 0)
    style.paragraph_format.space_before = Pt(before)
    style.paragraph_format.space_after = Pt(after)
    style.paragraph_format.keep_with_next = True
    style.paragraph_format.line_spacing = Pt(size * 1.1)
    # Remove theme font/color defaults inherited from Word's built-in styles.
    for child in style.element.get_or_add_rPr():
        for attr in list(child.attrib):
            if 'Theme' in attr or 'theme' in attr:
                del child.attrib[attr]
doc.styles['Title'].paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
for element in list(doc.styles['Title'].element.get_or_add_pPr()):
    if element.tag == qn('w:pBdr'):
        element.getparent().remove(element)
for element in list(doc.styles['Title'].element.get_or_add_rPr()):
    if element.tag in [qn('w:spacing'), qn('w:szCs')]:
        element.getparent().remove(element)

for name, base in [('Logo', 'Normal'), ('Agenda', 'Normal'), ('Subagenda', 'Agenda'),
                   ('Resolution', 'Normal'), ('Vote', 'Normal'), ('VoteHeading', 'Vote'), ('VoteLine', 'Vote'),
                   ('DecisionAccepted', 'Vote'), ('DecisionRejected', 'Vote'),
                   ('SummaryTitle', 'Heading 1'), ('Signature', 'Normal'),
                   ('AfterAgenda', 'Normal'), ('TableEnd', 'Vote')]:
    style = doc.styles.add_style(name, 1)
    style.base_style = doc.styles[base]

doc.styles['Logo'].paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
doc.styles['Logo'].paragraph_format.keep_with_next = True
doc.styles['Logo'].paragraph_format.line_spacing = 1
doc.styles['AfterAgenda'].paragraph_format.space_before = Pt(8.5)
doc.styles['TableEnd'].font.size = Pt(1)
doc.styles['TableEnd'].paragraph_format.line_spacing = Pt(1)
doc.styles['TableEnd'].paragraph_format.space_before = Pt(0)
doc.styles['TableEnd'].paragraph_format.space_after = Pt(0)
for name, indent in [('Agenda', 0.58), ('Subagenda', 1.16)]:
    doc.styles[name].paragraph_format.left_indent = Cm(indent)
    doc.styles[name].paragraph_format.space_after = Pt(0)
doc.styles['Resolution'].font.name = 'EB Garamond SemiBold'
doc.styles['Vote'].paragraph_format.space_after = Pt(0)
doc.styles['Vote'].paragraph_format.line_spacing = Pt(12.1)
vote_line = doc.styles['VoteLine'].paragraph_format
vote_line.left_indent = Pt(18)
vote_line.first_line_indent = Pt(-18)
vote_line.tab_stops.add_tab_stop(Pt(18))
doc.styles['VoteHeading'].paragraph_format.space_after = Pt(10)
for name, color, fill in [('DecisionAccepted', '32CD32', 'E6F4E6'), ('DecisionRejected', 'FF0000', 'F4E6E6')]:
    style = doc.styles[name]
    style.paragraph_format.space_before = Pt(0)
    style.paragraph_format.space_after = Pt(0)
    style.paragraph_format.line_spacing = Pt(12.1)
    style.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
doc.styles['SummaryTitle'].paragraph_format.page_break_before = True
doc.styles['Signature'].paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
doc.styles['Signature'].paragraph_format.space_before = Pt(45)
doc.styles['Signature'].paragraph_format.space_after = Pt(0)

# Table styles hold the presentation, the generator only selects a style.
for name, color in [('VoteAccepted', '32CD32'), ('VoteRejected', 'FF0000'), ('Signatures', None), ('ContentTable', '000000'), ('DecisionBoxAccepted', '32CD32'), ('DecisionBoxRejected', 'FF0000')]:
    style = doc.styles.add_style(name, 3)
    tp = prop(style.element, 'tblPr')
    prop(tp, 'jc', val='left' if name.startswith('DecisionBox') else 'center')
    margins = prop(tp, 'tblCellMar')
    for side in ['top', 'left', 'bottom', 'right']:
        padding = 80 if name == 'ContentTable' else 150 if side in ['left', 'right'] else 0
        if name.startswith('Vote') and side in ['top', 'bottom']:
            padding = 220
        if name.startswith('DecisionBox'):
            padding = 120 if side in ['left', 'right'] else 100
        prop(margins, side, w=padding, type='dxa')
    borders = prop(tp, 'tblBorders')
    for edge in ['top', 'left', 'bottom', 'right', 'insideH', 'insideV']:
        visible = color and (name == 'ContentTable' or name.startswith('DecisionBox') and edge not in ['insideH', 'insideV'] or edge in ['top', 'bottom'])
        prop(borders, edge, val='single' if visible else 'nil', sz=12 if name.startswith(('Vote', 'DecisionBox')) else 6, color=color or 'FFFFFF')
    if name.startswith('DecisionBox'):
        # The former almost identical foreground/background disappeared at normal zoom.
        prop(tp, 'shd', val='thinReverseDiagStripe', color='B8D8B8' if name.endswith('Accepted') else 'D8B8B8', fill='FFFFFF')

# Native Word fields; both the editable file and PDF have the same footer.
doc.styles['Footer'].font.name = 'EB Garamond'
doc.styles['Footer'].font.size = Pt(8)
doc.styles['Footer'].paragraph_format.space_before = Pt(0)
doc.styles['Footer'].paragraph_format.space_after = Pt(0)
doc.styles['Footer'].paragraph_format.line_spacing = Pt(9)
# A vertically centered cell keeps the fields in the middle of the band in both
# Word and LibreOffice, independently of the font's baseline and line metrics.
band = section.footer.add_table(rows=1, cols=1, width=Cm(21))
band.alignment = WD_TABLE_ALIGNMENT.CENTER
band.autofit = False
band.columns[0].width = Cm(21)
band._tbl.tblPr.find(qn('w:tblW')).set(qn('w:w'), str(Cm(21).twips))
band.rows[0].height = Pt(22.5)
band.rows[0].height_rule = WD_ROW_HEIGHT_RULE.EXACTLY
cell = band.cell(0, 0)
cell.width = Cm(21)
prop(cell._tc.get_or_add_tcPr(), 'shd', val='clear', fill='E6F4E6')
margins = prop(cell._tc.get_or_add_tcPr(), 'tcMar')
for side in ['top', 'left', 'bottom', 'right']:
    prop(margins, side, w=0, type='dxa')
cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
footer = cell.paragraphs[0]
footer.style = doc.styles['Footer']
footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
for value in ['PAGE', ' - ', 'NUMPAGES']:
    run = footer.add_run(' - ' if value == ' - ' else '')
    run.font.name = 'EB Garamond'
    run.font.size = Pt(8)
    if value != ' - ':
        field = OxmlElement('w:fldSimple')
        field.set(qn('w:instr'), value)
        prop(field, 'r').append(OxmlElement('w:t'))
        field[0][0].text = '1'
        footer._p.append(field)
for paragraph in list(section.footer.paragraphs):
    section.footer._element.remove(paragraph._p)
footer_tail = section.footer.add_paragraph()
footer_tail.paragraph_format.line_spacing = Pt(1)
footer_tail.paragraph_format.space_before = Pt(0)
footer_tail.paragraph_format.space_after = Pt(0)
footer_tail.add_run().font.size = Pt(1)

for tag, label, style in [('Logo', 'Znak obce', 'Logo'), ('Title', 'Název dokumentu', 'Title'),
                           ('Opening', 'Zahájení nebo údaje pozvánky', 'Normal'),
                           ('Body', 'Program a průběh zasedání', 'Normal'),
                           ('Resolutions', 'Přijatá usnesení', 'SummaryTitle'),
                           ('Signatures', 'Podpisy', 'Normal')]:
    sdt = OxmlElement('w:sdt')
    pr = prop(sdt, 'sdtPr')
    prop(pr, 'alias', val=label)
    prop(pr, 'tag', val='ZZZO.' + tag)
    content = prop(sdt, 'sdtContent')
    p = doc.add_paragraph(label, style)
    content.append(p._p)
    doc._element.body.insert(len(doc._element.body) - 1, sdt)
prop(doc.settings.element, 'embedTrueTypeFonts')
prop(doc.settings.element, 'embedSystemFonts')
prop(doc.settings.element, 'updateFields', val='true')
# Keep the schema order when adding lower-level properties not exposed by python-docx.
paragraph_order = ['pStyle', 'keepNext', 'keepLines', 'pageBreakBefore', 'framePr',
                   'widowControl', 'numPr', 'suppressLineNumbers', 'pBdr', 'shd', 'tabs',
                   'suppressAutoHyphens', 'kinsoku', 'wordWrap', 'overflowPunct',
                   'topLinePunct', 'autoSpaceDE', 'autoSpaceDN', 'bidi', 'adjustRightInd',
                   'snapToGrid', 'spacing', 'ind', 'contextualSpacing', 'mirrorIndents',
                   'suppressOverlap', 'jc', 'textDirection', 'textAlignment', 'textboxTightWrap',
                   'outlineLvl', 'divId', 'cnfStyle', 'rPr', 'sectPr', 'pPrChange']
for root in [doc.element, doc.styles.element, section.footer._element]:
    for pp in root.iter(qn('w:pPr')):
        pp[:] = sorted(pp, key=lambda e: paragraph_order.index(E.QName(e).localname) if E.QName(e).localname in paragraph_order else 100)
for name in ['VoteAccepted', 'VoteRejected', 'Signatures', 'ContentTable', 'DecisionBoxAccepted', 'DecisionBoxRejected']:
    tp = doc.styles[name].element.find(qn('w:tblPr'))
    order = ['jc', 'tblBorders', 'shd', 'tblCellMar']
    tp[:] = sorted(tp, key=lambda e: order.index(E.QName(e).localname))
settings = doc.settings.element
for tag in ['embedTrueTypeFonts', 'embedSystemFonts']:
    element = settings.find(qn('w:' + tag))
    settings.remove(element)
    settings.find(qn('w:proofState')).addprevious(element)
update = settings.find(qn('w:updateFields'))
settings.remove(update)
settings.find(qn('w:compat')).addprevious(update)
doc.core_properties.title = 'Šablona zápisů a pozvánek obce Svésedlice'
doc.core_properties.author = 'ZZZO'
TARGET.parent.mkdir(parents=True, exist_ok=True)
doc.save(TARGET)

# ECMA-376 font embedding: reverse GUID bytes and XOR the first 32 bytes.
with ZipFile(TARGET) as package:
    parts = {n: package.read(n) for n in package.namelist()}
font_table = E.fromstring(parts['word/fontTable.xml'])
for font in list(font_table):
    font_table.remove(font)
rels = E.Element('{%s}Relationships' % PKG, nsmap={None: PKG})
families = [('EB Garamond', [('Regular', 'embedRegular'), ('Bold', 'embedBold'), ('Italic', 'embedItalic'), ('BoldItalic', 'embedBoldItalic')]),
            ('EB Garamond SemiBold', [('SemiBold', 'embedRegular'), ('SemiBoldItalic', 'embedItalic')]),
            ('Noto Sans Symbols 2', [('Symbols', 'embedRegular')])]
for family, faces in families:
    font = E.SubElement(font_table, '{%s}font' % W, {'{%s}name' % W: family})
    for face, tag in faces:
        index = len(rels) + 1
        key = UUID('00000000-0000-0000-0000-%012d' % index)
        source = 'NotoSansSymbols2-Regular.ttf' if face == 'Symbols' else 'EBGaramond-' + face + '.ttf'
        data = bytearray((ROOT / 'resources/fonts' / source).read_bytes())
        mask = key.bytes[::-1]
        for i in range(32):
            data[i] ^= mask[i % 16]
        name = 'font%d.odttf' % index
        parts['word/fonts/' + name] = data
        rid = 'rIdFont%d' % index
        E.SubElement(font, '{%s}%s' % (W, tag), {'{%s}id' % R: rid, '{%s}fontKey' % W: '{' + str(key).upper() + '}'})
        E.SubElement(rels, '{%s}Relationship' % PKG, Id=rid, Type=R + '/font', Target='fonts/' + name)
parts['word/fontTable.xml'] = E.tostring(font_table, xml_declaration=True, encoding='UTF-8', standalone=True)
parts['word/_rels/fontTable.xml.rels'] = E.tostring(rels, xml_declaration=True, encoding='UTF-8', standalone=True)
types = E.fromstring(parts['[Content_Types].xml'])
E.SubElement(types, '{%s}Default' % types.nsmap[None], Extension='odttf', ContentType='application/vnd.openxmlformats-officedocument.obfuscatedFont')
parts['[Content_Types].xml'] = E.tostring(types, xml_declaration=True, encoding='UTF-8', standalone=True)
with ZipFile(TARGET, 'w', ZIP_DEFLATED) as package:
    for name, data in parts.items():
        package.writestr(name, data)
print(TARGET)
