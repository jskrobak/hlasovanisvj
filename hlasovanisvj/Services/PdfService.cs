using hlasovanisvj.Domain;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace hlasovanisvj.Services;

public class PdfService
{
    // Uprav si dle potřeby (velikosti písma/QR a odsazení)
    private const float IdFontSize = 44;
    private const float NameFontSize = 16;
    private const float QrSizeMm = 55;            // velikost QR čtverce na lístku
    private const float PageMarginMm = 10;         // okraj stránky
    private const float CenterLineWidthPt = 1.5f;  // tloušťka přerušované čáry
    private const float CenterLineDashPt = 6f;     // délka úseku
    private const float CenterLineGapPt = 6f;      // mezera

    public byte[] GeneratePdf(IReadOnlyList<Member> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        // Cache QR obrázků (ať se to negeneruje pořád dokola)
        var qrCache = members
            .Select(m => m.Id.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(id => id, CreateQrPng, StringComparer.Ordinal);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(PageMarginMm, QuestPDF.Infrastructure.Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(8, QuestPDF.Infrastructure.Unit.Millimetre);

                    // dvojice = 2 lístky v řádku
                    foreach (var pair in members.Chunk(2))
                    {
                        var left = pair.ElementAtOrDefault(0);
                        var right = pair.ElementAtOrDefault(1);

                        col.Item().Row(row =>
                        {
                            row.Spacing(0);

                            // levý lístek
                            row.RelativeItem().Element(e =>
                            {
                                if (left != null)
                                    BuildTicket(e, left, qrCache[left.Id.ToString()]);
                            });

                            // přerušovaná čára uprostřed (konstantní šířka)
                            //row.ConstantItem(CenterLineWidthPt + 2).Element(DrawDashedVerticalLine);

                            // pravý lístek
                            row.RelativeItem().Element(e =>
                            {
                                if (right != null)
                                    BuildTicket(e, right, qrCache[right.Id.ToString()]);
                            });
                        });
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void BuildTicket(IContainer container, Member member, byte[] qrPng)
    {
        // Bez rámečků, jen obsah
        container
            .PaddingHorizontal(8, QuestPDF.Infrastructure.Unit.Millimetre )
            .PaddingVertical(6, QuestPDF.Infrastructure.Unit.Millimetre)
            .AlignMiddle()
            .Column(col =>
            {
                col.Spacing(4, QuestPDF.Infrastructure.Unit.Millimetre);

                col.Item()
                    .AlignCenter()
                    .Text(member.Id)
                    .FontSize(IdFontSize)
                    .SemiBold();

                col.Item()
                    .AlignCenter()
                    .Text(member.Name ?? string.Empty)
                    .FontSize(NameFontSize);

                col.Item()
                    .AlignCenter()
                    .Width(QrSizeMm, QuestPDF.Infrastructure.Unit.Millimetre)
                    .Height(QrSizeMm, QuestPDF.Infrastructure.Unit.Millimetre)
                    .Image(qrPng);
            });
    }

    

    private static byte[] CreateQrPng(string text)
    {
        // QR jako PNG byte[] (QRCoder)
        using var qrGen = new QRCodeGenerator();
        using var qrData = qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);

        // pixelsPerModule: ovlivňuje rozlišení QR
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }
}