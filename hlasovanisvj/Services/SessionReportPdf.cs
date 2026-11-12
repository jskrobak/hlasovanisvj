using hlasovanisvj.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace hlasovanisvj.Services;

/// <summary>
/// PDF report schůze pro archivaci: souhrn, usnesení s výsledky, prezenční listina
/// a jmenovité hlasování ke každému usnesení.
/// </summary>
public class SessionReportPdf
{
    private static readonly Color Muted = Colors.Grey.Darken1;
    private static readonly Color Border = Colors.Grey.Lighten2;

    public byte[] Generate(SessionReport report) => Create(report).GeneratePdf();

    public IDocument Create(SessionReport report)
    {
        var session = report.Session;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial", "Liberation Sans", "DejaVu Sans"));

                page.Header().Column(col =>
                {
                    col.Item().Text(report.OrganizationName).FontColor(Muted);
                    col.Item().Text(session.Name).FontSize(16).SemiBold();
                    col.Item().Text(text =>
                    {
                        text.Span($"Datum schůze: {session.Date:d. M. yyyy}");
                        text.Span(session.IsClosed
                            ? $"   ·   uzavřena {Prague(session.ClosedAt!.Value):d. M. yyyy H:mm}"
                            : "   ·   schůze není uzavřena (průběžný stav)").FontColor(Muted);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Border);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(c => Summary(c, report));
                    col.Item().Element(c => Resolutions(c, report));
                    col.Item().PageBreak();
                    col.Item().Element(c => Attendance(c, report));

                    foreach (var resolution in report.Resolutions.Where(r => r.HasResult))
                    {
                        col.Item().PageBreak();
                        col.Item().Element(c => NamedVotes(c, report, resolution));
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Vygenerováno {Prague(DateTime.UtcNow):d. M. yyyy H:mm}").FontColor(Muted).FontSize(8);
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontColor(Muted).FontSize(8));
                        t.Span("Strana ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            });
        });
    }

    private static void Summary(IContainer container, SessionReport report)
    {
        container.Row(row =>
        {
            row.Spacing(8);
            Stat(row.RelativeItem(), "Vlastníci", report.Owners.Count().ToString());
            Stat(row.RelativeItem(), "Přítomno (vč. zastoupených)", report.PresentOwnersCount.ToString());
            Stat(row.RelativeItem(), "Přítomné podíly", $"{report.PresentShare:0.000} %");
            Stat(row.RelativeItem(), "Usnášeníschopná", report.IsQuorate ? "ano" : "ne");
        });

        static void Stat(IContainer c, string label, string value) =>
            c.Border(0.5f).BorderColor(Border).Padding(6).Column(col =>
            {
                col.Item().Text(label).FontColor(Muted).FontSize(8);
                col.Item().Text(value).FontSize(12).SemiBold();
            });
    }

    private static void Resolutions(IContainer container, SessionReport report)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).Text("Usnesení").FontSize(13).SemiBold();

            if (!report.Resolutions.Any())
            {
                col.Item().Text("Ke schůzi nejsou žádná usnesení.").FontColor(Muted);
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(18);
                    c.RelativeColumn(4);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.2f);
                });

                table.Header(h =>
                {
                    foreach (var title in new[] { "#", "Usnesení", "Pro", "Proti", "Zdržel se", "Přítomno", "Výsledek" })
                        h.Cell().Element(HeaderCell).Text(title).SemiBold();
                });

                var i = 1;
                foreach (var r in report.Resolutions)
                {
                    var opened = r.Id == report.Session.OpenedResolutionId;
                    var hasResult = r.HasResult && !opened;

                    table.Cell().Element(Cell).Text(i++.ToString());
                    table.Cell().Element(Cell).Column(c =>
                    {
                        c.Item().Text(r.Title).SemiBold();
                        c.Item().Text(r.Question).FontColor(Muted);
                    });
                    table.Cell().Element(Cell).AlignRight().Text(hasResult ? $"{r.PercentOfPresent(r.InFavorShare):0.00} %" : "");
                    table.Cell().Element(Cell).AlignRight().Text(hasResult ? $"{r.PercentOfPresent(r.AgainstShare):0.00} %" : "");
                    table.Cell().Element(Cell).AlignRight().Text(hasResult ? $"{r.PercentOfPresent(r.AbstainedShare):0.00} %" : "");
                    table.Cell().Element(Cell).AlignRight().Text(hasResult ? $"{r.PresentShare:0.000} %" : "");
                    table.Cell().Element(Cell).Text(SessionReport.ResultText(r, opened)).SemiBold();
                }
            });

            col.Item().PaddingTop(4).Text(
                "Procenta jsou z podílů přítomných při uzavření hlasování. „Přijato“ = nadpoloviční většina hlasů přítomných " +
                "(výchozí pravidlo § 1206 NOZ; stanovy mohou pro některá rozhodnutí vyžadovat jinou většinu).")
                .FontSize(7).FontColor(Muted);
        });
    }

    private static void Attendance(IContainer container, SessionReport report)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).Text("Prezenční listina").FontSize(13).SemiBold();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(22);
                    c.RelativeColumn(3);
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    foreach (var title in new[] { "Č.", "Vlastník / host", "Jednotky", "Podíl", "Účast / zastupuje", "Podpis" })
                        h.Cell().Element(HeaderCell).Text(title).SemiBold();
                });

                foreach (var m in report.Members)
                {
                    var attendance = SessionReport.AttendanceText(m);
                    if (m.Principals.Any())
                        attendance += $"; zastupuje: {string.Join(", ", m.Principals.Select(p => p.Name))}";

                    table.Cell().Element(Cell).Text(m.Id.ToString());
                    table.Cell().Element(Cell).Text(m.IsGuest ? $"{m.Name} (host)" : m.Name);
                    table.Cell().Element(Cell).Text(string.Join(", ", m.Units));
                    table.Cell().Element(Cell).AlignRight().Text(m.IsGuest ? "–" : $"{m.ShareValue:0.000} %");
                    table.Cell().Element(Cell).Text(attendance).FontColor(m.IsPresent ? Colors.Black : Muted);
                    table.Cell().Element(Cell).MinHeight(18).Text("");
                }

                table.Cell().ColumnSpan(3).Element(Cell).Text("Celkem přítomno").SemiBold();
                table.Cell().Element(Cell).AlignRight().Text($"{report.PresentShare:0.000} %").SemiBold();
                table.Cell().ColumnSpan(2).Element(Cell).Text($"z {report.TotalShare:0.000} %").FontColor(Muted);
            });
        });
    }

    private static void NamedVotes(IContainer container, SessionReport report, Domain.Resolution resolution)
    {
        container.Column(col =>
        {
            col.Item().Text($"Jmenovité hlasování: {resolution.Title}").FontSize(13).SemiBold();
            col.Item().Text(resolution.Question).FontColor(Muted);
            col.Item().PaddingBottom(6).Text(
                $"Pro {resolution.PercentOfPresent(resolution.InFavorShare):0.00} %, " +
                $"proti {resolution.PercentOfPresent(resolution.AgainstShare):0.00} %, " +
                $"zdržel se {resolution.PercentOfPresent(resolution.AbstainedShare):0.00} % " +
                $"(přítomno {resolution.PresentShare:0.000} % podílů) – {SessionReport.ResultText(resolution, false)}");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(22);
                    c.RelativeColumn(3);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(2.4f);
                });

                table.Header(h =>
                {
                    foreach (var title in new[] { "Č.", "Vlastník", "Podíl", "Hlas", "Hlasoval(a) prostřednictvím" })
                        h.Cell().Element(HeaderCell).Text(title).SemiBold();
                });

                // Jen vlastníci (hosté mají nulový podíl a hlasují za zmocnitele).
                foreach (var m in report.Owners.Where(m => m.IsPresent))
                {
                    table.Cell().Element(Cell).Text(m.Id.ToString());
                    table.Cell().Element(Cell).Text(m.Name);
                    table.Cell().Element(Cell).AlignRight().Text($"{m.ShareValue:0.000} %");
                    table.Cell().Element(Cell).Text(SessionReport.ChoiceText(report.GetVote(resolution, m)));
                    table.Cell().Element(Cell).Text(m.Proxy?.Name ?? "");
                }
            });
        });
    }

    private static readonly TimeZoneInfo PragueZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    /// <summary>Čas v ČR nezávisle na časové zóně serveru (v Dockeru bývá UTC).</summary>
    private static DateTime Prague(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PragueZone);

    private static IContainer HeaderCell(IContainer c) =>
        c.BorderBottom(0.75f).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).PaddingHorizontal(2);

    private static IContainer Cell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Border).PaddingVertical(3).PaddingHorizontal(2);
}
