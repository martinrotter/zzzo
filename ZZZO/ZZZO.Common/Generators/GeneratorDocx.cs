using System;
using ZZZO.Common.API;

namespace ZZZO.Common.Generators
{
  public class GeneratorDocx : Generator
  {
    #region Vlastnosti

    public override string FileSuffix
    {
      get => "docx";
    }

    #endregion

    #region Metody

    protected override byte[] GenerateDoWork(Zasedani zas, IProgress<int> progress)
    {
      // TODO: generovat docx přes OpenXML SDK nebo Xceed.DocX
      //TODO: nebo možná udělat generátor přímo do PDF, například QuestPDF vypadá skvěle
      // TODO: jako první asi generovat pěkné HTML, to se dá tisknout do PDF přes CefSharp nebo externě
      // v jakémkoliv prohlížeči
      return null;
    }

    #endregion
  }
}