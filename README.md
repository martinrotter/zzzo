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
2. V sekci `Základní informace` vyplňte všechny informace o obci a adrese konání zasedání. Vyplňte seznam zastupitelů a u každého nastavte vlastností (zda je přítomen, kdo je zapisovatel, atd.). Vlastnosti zastupitele v seznamu lze upravit klepnutím do daného pole.
3. V sekci `Program` vytvořte program. V levé částí tlačítkem `+` přidávejte jednotlivé body programu, v pravé části u každého programu nastavíte jeho detaily.
4. U každého bodu programu můžete vytvořit jednotlivá usnesení. Seznam přítomných zastupitelů v sekci `Usnesení` nelze měnit, je aktualizován automaticky. Pouze u každého zastupitele vyberte jeho volbu v hlasování o daném usnesení.
5. Až budete hotoví, přejděte do sekce `Generátor`. V této sekci tlačítkem `Přegenerovat dokument` nejdříve vygenerujete výsledný dokument, který se zobrazí v zabudovaném náhledu. Pokud jste s výsledkem spokojení, tak můžete výsledek exportovat do některého z nabízených formátů nebo jej rovnou vytisknout.

Samozřejmě, program umožňuje ukládat rozpracovaná zasedání do datových souborů (koncovka `.zzzo`), což se dá použít k deduplikaci práce. Například seznam zastupitelů tak nemusíte vytvářet vždy, stačí načíst předchozí zasedání, upravit a uložit pod novým názvem souboru.

### Hlášení chyb
Pokud v programu objevíte chybu nebo máte nápad na jeho zlepšení, můžete to nahlásit [zde](https://github.com/martinrotter/generator-zasedani-zo/issues/new).
