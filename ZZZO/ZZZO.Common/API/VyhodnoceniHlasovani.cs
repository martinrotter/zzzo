namespace ZZZO.Common.API;

public sealed record VyhodnoceniHlasovani(int Pro, int Proti, int ZdrzelSe, int Nepritomni, int PotrebnychHlasu, bool JePlatne)
{
  public bool JeSchvaleno => JePlatne && Pro >= PotrebnychHlasu;
  public string Souhrn => $"Pro: {Pro} · Proti: {Proti} · Zdržel se: {ZdrzelSe} · Nepřítomni: {Nepritomni}";
  public string Vysledek => !JePlatne ? "Hlasování obsahuje chybu." : JeSchvaleno ? "Návrh byl přijat." : "Návrh nebyl přijat.";
  public static VyhodnoceniHlasovani Vyhodnotit(Zasedani zasedani, Usneseni usneseni)
  {
    var osoby = zasedani.Zastupitele;
    var hlasy = usneseni.VolbyZastupitelu;
    bool platne = osoby.Count > 0 && osoby.Select(z => z.Id).Distinct().Count() == osoby.Count &&
      hlasy.Count == osoby.Count && hlasy.Select(h => h.ZastupitelId).Distinct().Count() == hlasy.Count &&
      hlasy.All(h => osoby.Any(z => z.Id == h.ZastupitelId) && Enum.IsDefined(h.Volba));
    var pritomni = osoby.Where(z => z.JePritomen).Select(z => z.Id).ToHashSet();
    int Pocet(HlasovaniZastupitele.VolbaHlasovani volba) => hlasy.Count(h => pritomni.Contains(h.ZastupitelId) && h.Volba == volba);
    return new(Pocet(HlasovaniZastupitele.VolbaHlasovani.Pro), Pocet(HlasovaniZastupitele.VolbaHlasovani.Proti),
      Pocet(HlasovaniZastupitele.VolbaHlasovani.ZdrzujeSe), osoby.Count(z => !z.JePritomen), osoby.Count / 2 + 1, platne);
  }
}
