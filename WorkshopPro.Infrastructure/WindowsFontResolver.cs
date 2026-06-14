using PdfSharp.Fonts;
using System.IO;
using System.Runtime.CompilerServices;

namespace WorkshopPro.Infrastructure
{
    public class WindowsFontResolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            string fontsFolder = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Fonts);

            string fileName;
            if (faceName == "Arial-Bold")
                fileName = "arialbd.ttf";
            else if (faceName == "Arial-Italic")
                fileName = "ariali.ttf";
            else if (faceName == "Arial-BoldItalic")
                fileName = "arialbi.ttf";
            else
                fileName = "arial.ttf";

            string fullPath = Path.Combine(fontsFolder, fileName);
            return File.ReadAllBytes(fullPath);
        }
        

        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        {
            string face;
            if (bold && italic)
                face = "Arial-BoldItalic";
            else if (bold)
                face = "Arial-Bold";
            else if (italic)
                face = "Arial-Italic";
            else
                face = "Arial";

            return new FontResolverInfo(face);
        }
    }
}
