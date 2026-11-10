# ZZZO - zápisník zasedání zastupitelstev obcí
Jednoduchá utilita na generování pozvánek a zápisů ze zasedání zastupitelstev obcí.

Utilita umožňuje načítání/ukládání rozpracovaných zápisů/pozvánek a generování upravitelného DOCX ze šablony. PDF vzniká převodem tohoto DOCX; stejné PDF se zobrazuje v náhledu a lze je tisknout.

ZZZO je svobodná aplikace, která bude vždy zdarma! ZZZO je desktopová aplikace - vaše data tak nejsou uložena v žádném cloudu a máte je tak vždy pod kontrolou.

### Nejnovější verzi aplikace lze stáhnout [zde](https://github.com/martinrotter/zzzo/releases) vždycky v sekci `Assets`. Testovací verzi aplikace potom najdete přímo [zde](https://github.com/martinrotter/zzzo/releases/download/devbuild/zzzo.zip).

### Instalace

1. Stáhněte si některou [verzi](https://github.com/martinrotter/zzzo/releases) aplikace.
2. Rozbalte aplikaci ze staženého zip archivu do libovolné složky.
3. Spusťte aplikaci poklepáním na soubor `ZZZO.exe`.
    1. Pokud aplikace neběží či hlásí chybu, je třeba nainstalovat sdílené běhové knihovny MSVC 2022.
    2. Knihovny stáhněte [zde](https://github.com/martinrotter/generator-zasedani-zo/raw/master/3rd-party/VcRedist/VC_redist.x86.exe) nebo [zde](https://aka.ms/vs/17/release/vc_redist.x86.exe) a nainstalujte.
    3. Opakujte krok 3.
4. Hotovo!

### Seznam funkcí

* rozpracované zápisy lze ukládat a načítat, a kdykoliv tak pokračovat v práci,
* program umí dle zadaného názvu obce najít online logo dané obce,
* kromě zápisů program umí generovat i jednoduchou pozvánku,
* lze pohodlně editovat seznam zastupitelů, body programu a další související věci,
* vygenerované zápisy odpovídají § 95 odst. 1 zákona č. 128/2000 Sb. o obcích,
* vzhled dokumentů určuje upravitelná DOCX šablona v `Data/Templates`, včetně vložených fontů,
* aplikace nevyžaduje instalaci a podporuje 32/64 bitovou variantu OS Windows (8+),
* aplikace má zcela otevřený [zdrojový kód](ZZZO), který kompletně podléhá licenci [GNU GPLv3](LICENSE).

### Jak vytvořit zápis

1. Spusťte aplikaci.
2. V sekci `Základní informace` vyplňte všechny informace o obci a adrese konání zasedání. Vyplňte seznam zastupitelů a u každého nastavte vlastnosti (zda je přítomen, kdo je zapisovatel, atd.). Vlastnosti zastupitele v seznamu lze upravit klepnutím do daného pole.
3. V sekci `Program` vytvořte program. V levé částí tlačítkem `+` přidávejte jednotlivé body programu, v pravé části u každého programu nastavíte jeho detaily.
4. U každého bodu programu můžete vytvořit jednotlivá usnesení. Seznam přítomných zastupitelů v sekci `Usnesení` nelze měnit, je aktualizován automaticky. Pouze u každého zastupitele vyberte jeho volbu v hlasování o daném usnesení.
5. Až budete hotoví, přejděte do sekce `Generátor`. V této sekci tlačítkem `Přegenerovat dokument` nejdříve vygenerujete výsledný dokument, který se zobrazí v zabudovaném náhledu. Pokud jste s výsledkem spokojení, tak můžete výsledek exportovat do některého z nabízených formátů nebo jej rovnou vytisknout.

Samozřejmě, program umožňuje ukládat rozpracovaná zasedání do datových souborů (koncovka `.zzzo`), což se dá použít k deduplikaci práce. Například seznam zastupitelů tak nemusíte vytvářet vždy, stačí načíst předchozí zasedání, upravit a uložit pod novým názvem souboru.

### Program, usnesení a kontrola údajů

- Základní informace mají vlevo údaje o zasedání, adresu a znak obce. Vpravo se seznam zastupitelů přizpůsobuje šířce i výšce okna; tlačítka zůstávají pod tabulkou. V menším okně lze levý formulář posouvat samostatně.
- Program obsahuje hlavní body a jednu úroveň skutečných podbodů. Nabídka **Přidat** přidává body i podbody. Šipkami **↑ / ↓** pod seznamem změníte pořadí mezi sourozenci. V **Akce → Upravit** převedete bod na podbod a zpět. Přesun rodiče zahrnuje jeho podbody.
- Každý bod i podbod má režim **Informativní**, **Bere na vědomí**, nebo **S usneseními**. Informativní bod nevytváří žádnou automatickou formulaci. „Bere na vědomí“ se vypíše jednou bez hlasování. Režim s usneseními umožňuje libovolný počet samostatných návrhů a hlasování.
- Průběh projednání i každé usnesení mají HTML editor. Hlasování je vedle textu; výchozí hlas je **Pro**. Výsledek označuje zelená fajfka nebo červená výstraha vedle nadpisu Hlasování; počty hlasů a potřebná většina jsou v tooltipu. Nepřítomní se do hlasů nepočítají. Ke schválení je potřeba nadpoloviční většina všech evidovaných členů zastupitelstva.
- Vestavěný editor používá lokálně přibalený TinyMCE 8.9.0 s českým překladem a licencí GPL-2.0-or-later. Má jednu základní lištu: zpět/znovu, tučné písmo, kurzívu, podtržení, přeškrtnutí, horní/dolní index, zarovnání, seznamy, odkazy a odstranění formátování. Nabídka tabulek umožňuje vložení, přidání či odebrání řádků a sloupců a sloučení/rozdělení buněk; šířky lze upravovat tažením hranic sloupců.
- Editor používá písmo rozhraní Segoe UI 14 px a pohodlné řádkování nezávisle na písmu dokumentu. Odstavcové mezery a vzhled buněk přebírá ze zvolené DOCX šablony. Šířka textové oblasti je omezena šířkou obsahu šablony, v užším panelu se přizpůsobí prostoru. Přesnou sazbu a stránkování dokumentu ukazuje PDF náhled. Dokončený Unicode vstup z WPF předává přes nativní textové API CEF, aby správně fungovala i česká klávesa 2/ě.
- Při vložení se zachová základní formátování, obsah, seznamy, geometrie tabulek a sloučené buňky. Cizí fonty, barvy, pevné výšky, odstavcové odsazení a aktivní obsah se odstraní. Značky seznamů Wordu se převádějí na skutečné seznamy včetně vnoření a počátečního čísla. Tabulky ukládají šířky v `colgroup`/`col`, případně v buňkách; zbytečné prezentační atributy se neukládají. Starší obrázky a nadpisy zůstávají podporované při načtení.
- Barevný pruh rozlišuje typ bodu, kolečko ukazuje počet jeho vlastních usnesení. Výstraha zahrnuje i chyby usnesení a podbodů.
- Schválení programu, schválení zapisovatele a ověřovatelů a kontrola minulého zápisu zůstávají editovatelné, lze je přidat, duplikovat i odstranit. Pro zápis musí být každý z těchto typů právě jednou.
- Společný stavový řádek kontroly je dole a zůstává vidět na všech záložkách. Výstražná ikona při najetí zobrazí úplný seznam chyb; kliknutím otevřete interaktivní seznam. Kliknutím na problém přejdete k příslušnému bodu, usnesení nebo zastupiteli. Nepřítomný nesmí být řídícím, zapisovatelem ani ověřovatelem; tlačítko „Zrušit role“ jeho přiřazení odstraní. Starosta může být nepřítomen.
- Rozpracovaná data lze uložit i s obsahovými chybami. Neplatně napsané číselné hodnoty je však nutné opravit. Pozvánku neblokují nedokončená hlasování ani chybějící procedurální body.
- Náhled skutečného PDF používá otevřený WPF prohlížeč PdfiumViewer.Net.WPF s přibaleným PDFium. Zobrazuje dokument bez informačního řádku a ovládací lišty. Umožňuje výběr a kopírování textu; Ctrl + plus/minus mění zoom a Ctrl + 0 obnoví základní velikost. Při přiblížení se PDF znovu vykresluje podle zoomu a DPI monitoru. Svislá i vodorovná pozice se pamatuje zvlášť pro pozvánku a zápis. Po změně dat, typu dokumentu nebo šablony je nutné dokument přegenerovat před exportem či tiskem.
- Aplikace si mezi spuštěními pamatuje velikost, pozici a maximalizaci hlavního okna i oba splittery v sekci usnesení. Nastavení je čitelný JSON v `%LocalAppData%\ZZZO\nastaveni.json`; poškozené nastavení se bezpečně ignoruje.

### Datový formát

Nové soubory používají `VerzeFormatu: 2`, stabilní identifikátory položek a kolekci `Podbody` u rodiče. Hlasy odkazují na zastupitele přes `ZastupitelId`. Starý datový formát se záměrně nenačítá a nemigruje. Při duplikaci vzniknou nové identifikátory, texty a hlasování se zkopírují nezávisle.

### Dokumenty a šablony

Aplikace používá `DocumentFormat.OpenXml` pro práci s DOCX a `HtmlAgilityPack` pro načtení fragmentů z editoru TinyMCE. Odstavce, nadpisy, formátované texty, číslované i odrážkové seznamy, tabulky se sloučenými buňkami, odkazy a obrázky převádí na nativní prvky Wordu. Nevytváří výstupní HTML ani nevkládá HTML pomocí `altChunk`. Základní písmo a sazbu určuje šablona; libovolné CSS z webových stránek nemá ve Wordu přesný ekvivalent.

Šířky sloupců se přebírají přednostně z `colgroup`, potom z buněk. Převod podporuje procenta, px, pt, cm, mm a palce; u sloučených buněk respektuje skutečnou mřížku i svislé pokračování. Zachovává poměry a celkovou šířku tabulky a příliš široké tabulky zmenší do textové oblasti stránky. V DOCX používá pevnou mřížku, aby Word nepřepočítal poměry podle obsahu. Výšky řádků se přizpůsobují obsahu.

Výchozí šablona `resources/templates/svésedlice.docx` vychází z dosavadních zápisů: A4, okraje 2,2/1,45 cm, EB Garamond 11 pt, nadpisy, barevná hlasování, souhrn přijatých usnesení na nové straně, podpisy a stránkování. Obsahuje plné fonty Regular, Bold, Italic, BoldItalic, SemiBold a SemiBoldItalic a symbolový font Noto Sans Symbols 2, aby byl dokument upravitelný i offline. Licence SIL OFL a zdroj fontů jsou v `resources/fonts`. Rozdíly v zalamování mezi Wordem a LibreOffice mohou změnit počet stran oproti původnímu HTML/PDF.

Šablonu lze upravit ve Wordu nebo LibreOffice. Zachovejte pojmenované styly a šest obsahových ovládacích prvků s tagy `ZZZO.Logo`, `ZZZO.Title`, `ZZZO.Opening`, `ZZZO.Body`, `ZZZO.Resolutions`, `ZZZO.Signatures`. Generátor je nahradí obsahem; ostatní části šablony ponechá. Vlastní šablonu zkopírujte do `Data/Templates` a vyberte v generátoru. Zdrojový skript `resources/scripts/create-docx-template.py` umožňuje výchozí šablonu znovu sestavit s `python-docx`; pro běh aplikace Python potřeba není.

Popisky hlasování používají společný tabulátor. Šířka rámečku výsledku se vypočítává podle textu, fontů a odsazení ze šablony, aby zůstala kompaktní ve Wordu i v PDF. Pruhované pozadí je nativní vzor výplně Wordu a zachovává se i v PDF vytvořeném Wordem; LibreOffice ho při převodu nahrazuje jednolitou světlou výplní. Čísla stran jsou vodorovně i svisle vystředěná v barevném pruhu zápatí.

**Pro náhled, PDF a tisk je potřeba desktopový Microsoft Word nebo LibreOffice.** Přednost má nainstalovaný Word: přes COM API `Document.ExportAsFixedFormat` pracuje bez zobrazení okna v samostatné instanci a pomocném STA procesu s časovým limitem. Při nedostupnosti, chybě nebo překročení limitu Wordu se automaticky použije LibreOffice. Bez obou převodníků lze stále vygenerovat a exportovat DOCX.

LibreOffice aplikace hledá v `LibreOffice/program/soffice.exe` vedle aplikace, v Program Files a v PATH. Vlastní umístění lze určit proměnnou `ZZZO_LIBREOFFICE` s úplnou cestou k `soffice.exe`; konverze používá oddělený dočasný profil. Pro vynucení LibreOffice lze nastavit `ZZZO_PDF_CONVERTER=libreoffice`. PDF i náhled vycházejí ze stejných bajtů DOCX. Tisk probíhá přes nativní tiskové rozhraní PDFium do tiskového zařízení.

### Sestavení a testy

Potřebujete Windows a .NET 10 SDK. ZIP vytváří přímo .NET, další archivační program není potřeba.

```powershell
# Datové, validační a DOCX regresní testy včetně validace OOXML (self-contained x86).
.\resources\scripts\test-windows.ps1

# PDF a skutečný WPF náhled mimo obrazovku, včetně opakovaného vykreslení bez scrollu.
.\resources\scripts\test-windows.ps1 -Preview

# Skutečný TinyMCE mimo obrazovku, český vstup WPF, vložení Word/Excel a export do DOCX/PDF.
.\resources\scripts\test-windows.ps1 -Editor

# Navíc skutečný převod Wordem i záložním LibreOffice, WPF/CefSharp editory a datové vazby.
.\resources\scripts\test-windows.ps1 -Gui -Pdf

# Testy, self-contained publish a ověřený balíček zzzo.zip.
.\resources\scripts\build-windows.ps1
```

Testovací data, screenshoty a PDF jsou v `ZZZO/ZZZO.Tests/bin/Release/net10.0-windows7.0/win-x86/publish/test-results`. GUI test používá vlastní mezipaměť prohlížeče. GitHub Actions spouští i GUI test a ukládá diagnostické výstupy; release se publikuje jen při push, nikoli při pull requestu.

### Hlášení chyb

Pokud v programu objevíte chybu nebo máte nápad na jeho zlepšení, můžete to nahlásit [zde](https://github.com/martinrotter/generator-zasedani-zo/issues/new).
