using System.Collections.ObjectModel;
using Newtonsoft.Json;
namespace ZZZO.Common.API;

public class Usneseni : ObservableObject
{
  public Guid Id { get; set; } = Guid.NewGuid();
  private string _textHtml = "";
  public string TextHtml { get => _textHtml; set { if (SetProperty(ref _textHtml, value ?? "")) OnPropertyChanged(nameof(Vytah)); } }
  public ObservableCollection<HlasovaniZastupitele> VolbyZastupitelu { get; } = new();
  [JsonIgnore] public string Vytah => string.IsNullOrWhiteSpace(HtmlObsah.ProstyText(TextHtml)) ? "(prázdné usnesení)" : HtmlObsah.ProstyText(TextHtml);
}
