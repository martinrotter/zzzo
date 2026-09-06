# ZZZO - zápisník zasedání zastupitelstev obcí
Jednoduchá utilita na generování pozvánek a zápisů ze zasedání zastupitelstev obcí.

Utilita umožňuje načítání/ukládání rozpracovaných zápisů/pozvánek a následné generování výstupu ve formátu HTML nebo PDF. Je možno zápisy i přímo tisknout.

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
* aplikace podporuje standardní CSS styly pro úpravu formátu výstupního dokumentu, je tedy docela jednoduché implementovat zcela vlastní styl, který poté stačí uložit do podsložky `Styles`,
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

- Program obsahuje hlavní body a jednu úroveň skutečných podbodů. Nabídka **Přidat** přidává body i podbody. V **Akce → Upravit** změníte pořadí mezi sourozenci nebo převedete bod na podbod a zpět. Přesun rodiče zahrnuje jeho podbody.
- Každý bod i podbod má režim **Informativní**, **Bere na vědomí**, nebo **S usneseními**. Informativní bod nevytváří žádnou automatickou formulaci. „Bere na vědomí“ se vypíše jednou bez hlasování. Režim s usneseními umožňuje libovolný počet samostatných návrhů a hlasování.
- Průběh projednání i každé usnesení mají HTML editor. Hlasování je vedle textu; výchozí hlas je **Pro**. Výsledek označuje zelená fajfka nebo červená výstraha vedle nadpisu Hlasování; počty hlasů a potřebná většina jsou v tooltipu. Nepřítomní se do hlasů nepočítají. Ke schválení je potřeba nadpoloviční většina všech evidovaných členů zastupitelstva.
- Vestavěný HTML editor používá lokálně přibalený TinyMCE 8.9.0 s českým překladem a licencí GPL-2.0-or-later. Tabulky ukládá jako jednoduché sémantické HTML bez prezentačních atributů a pomocných sloupcových skupin.
- Barevný pruh rozlišuje typ bodu, kolečko ukazuje počet jeho vlastních usnesení. Výstraha zahrnuje i chyby usnesení a podbodů.
- Schválení programu, schválení zapisovatele a ověřovatelů a kontrola minulého zápisu zůstávají editovatelné, lze je přidat, duplikovat i odstranit. Pro zápis musí být každý z těchto typů právě jednou.
- Společný stavový řádek kontroly je dole a zůstává vidět na všech záložkách. Výstražná ikona při najetí zobrazí úplný seznam chyb; kliknutím otevřete interaktivní seznam. Kliknutím na problém přejdete k příslušnému bodu, usnesení nebo zastupiteli. Nepřítomný nesmí být řídícím, zapisovatelem ani ověřovatelem; tlačítko „Zrušit role“ jeho přiřazení odstraní. Starosta může být nepřítomen.
- Rozpracovaná data lze uložit i s obsahovými chybami. Neplatně napsané číselné hodnoty je však nutné opravit. Pozvánku neblokují nedokončená hlasování ani chybějící procedurální body.
- Náhled zachovává scroll zvlášť pro pozvánku a zápis. Po změně dat, typu dokumentu nebo stylu je nutné náhled přegenerovat před exportem či tiskem. Původní výstupní CSS a PDF patička zůstaly zachovány.
- Aplikace si mezi spuštěními pamatuje velikost, pozici a maximalizaci hlavního okna i oba splittery v sekci usnesení. Nastavení je čitelný JSON v `%LocalAppData%\ZZZO\nastaveni.json`; poškozené nastavení se bezpečně ignoruje.

### Datový formát

Nové soubory používají `VerzeFormatu: 2`, stabilní identifikátory položek a kolekci `Podbody` u rodiče. Hlasy odkazují na zastupitele přes `ZastupitelId`. Starý datový formát se záměrně nenačítá a nemigruje. Při duplikaci vzniknou nové identifikátory, texty a hlasování se zkopírují nezávisle.

### Sestavení a testy

Potřebujete Windows a .NET 10 SDK. ZIP vytváří přímo .NET, další archivační program není potřeba.

```powershell
# Datové, validační a HTML regresní testy (self-contained x86).
.\resources\scripts\test-windows.ps1

# Navíc skutečné WPF/CefSharp editory, scroll, PDF a kontrola datových vazeb.
.\resources\scripts\test-windows.ps1 -Gui

# Testy, self-contained publish a ověřený balíček zzzo.zip.
.\resources\scripts\build-windows.ps1
```

Testovací data, screenshoty a PDF jsou v `ZZZO/ZZZO.Tests/bin/Release/net10.0-windows7.0/win-x86/publish/test-results`. GUI test používá vlastní mezipaměť prohlížeče. GitHub Actions spouští i GUI test a ukládá diagnostické výstupy; release se publikuje jen při push, nikoli při pull requestu.

### Hlášení chyb

Pokud v programu objevíte chybu nebo máte nápad na jeho zlepšení, můžete to nahlásit [zde](https://github.com/martinrotter/generator-zasedani-zo/issues/new).
