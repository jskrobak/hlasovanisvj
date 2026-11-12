using hlasovanisvj.Domain;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace hlasovanisvj.Services;

public class PdfService(IConfiguration config)
{
    // Uprav si dle potřeby (velikosti písma/QR a odsazení)
    private const float IdFontSize = 44;
    private const float NameFontSize = 16;
    private const float QrSizeMm = 55; // velikost QR čtverce na lístku
    private const float PageMarginMm = 20; // okraj stránky


    public byte[] GeneratePdf(IReadOnlyList<Member> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        var qrCache = members
            .Select(m => m.GlobalId)
            .Distinct()
            .ToDictionary(id => id, CreateQrPng);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4); // A4 na výšku
                page.Margin(PageMarginMm, QuestPDF.Infrastructure.Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial", "Liberation Sans", "DejaVu Sans"));

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    // každá stránka = 4 lístky
                    foreach (var pageMembers in members.Chunk(4))
                    {
                        col.Item().Grid(grid =>
                        {
                            grid.Columns(2);
                            grid.Spacing(0);

                            foreach (var member in pageMembers)
                            {
                                grid.Item().Element(e => { BuildTicket(e, member, qrCache[member.GlobalId]); });
                            }
                        });

                        // vynutí novou stránku po 4 kartách
                        col.Item().PageBreak();
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void BuildTicket(IContainer container, Member member, byte[] qrPng)
    {
        container
            .PaddingHorizontal(10, QuestPDF.Infrastructure.Unit.Millimetre)
            .PaddingVertical(12, QuestPDF.Infrastructure.Unit.Millimetre)
            .AlignMiddle()
            .AlignCenter()
            .Column(col =>
            {
                col.Spacing(6, QuestPDF.Infrastructure.Unit.Millimetre);

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

    private byte[] CreateQrPng(Guid memberGlobalId)
    {
        var text = $"{config["BaseUrl"]}/Vote?guid={memberGlobalId}";

        // QR jako PNG byte[] (QRCoder)
        using var qrGen = new QRCodeGenerator();
        using var qrData = qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);

        // pixelsPerModule: ovlivňuje rozlišení QR
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }
}