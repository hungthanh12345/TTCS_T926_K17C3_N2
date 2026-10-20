using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using InternshipManagementApi.DTOs.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OpenXmlColor = DocumentFormat.OpenXml.Spreadsheet.Color;
using OpenXmlFonts = DocumentFormat.OpenXml.Spreadsheet.Fonts;
using QuestColors = QuestPDF.Helpers.Colors;

namespace InternshipManagementApi.Services.Reports;

public sealed record Sprint5ReportExportFile(
    byte[] Content,
    string ContentType,
    string FileName);

public interface ISprint5ReportExportService
{
    Sprint5ReportExportFile CreateExcel(Sprint5AggregateReportDto report);
    Sprint5ReportExportFile CreatePdf(Sprint5AggregateReportDto report);
}

public sealed class PdfExportLicenseNotConfiguredException : InvalidOperationException
{
    public PdfExportLicenseNotConfiguredException()
        : base("PDF export is disabled until an approved QuestPDF license type is configured.")
    {
    }
}

public sealed class Sprint5ReportExportService : ISprint5ReportExportService
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";
    private readonly bool _pdfExportEnabled;

    public Sprint5ReportExportService(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredLicense = configuration["Reports:QuestPdfLicense"];
        var communityLicenseAllowed = environment.IsDevelopment() || environment.IsEnvironment("Test");
        if (Enum.TryParse<LicenseType>(configuredLicense, ignoreCase: true, out var license) &&
            Enum.IsDefined(license) &&
            (license != LicenseType.Evaluation || environment.IsEnvironment("Test")) &&
            (license != LicenseType.Community || communityLicenseAllowed))
        {
            QuestPDF.Settings.License = license;
            _pdfExportEnabled = true;
        }
    }

    public Sprint5ReportExportFile CreateExcel(Sprint5AggregateReportDto report)
    {
        Validate(report);
        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(
                   stream,
                   SpreadsheetDocumentType.Workbook,
                   autoSave: true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            AddWorkbookStyles(workbookPart);

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "TongHop"
            });

            WriteReportRows(sheetData, report);
            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }

        return new Sprint5ReportExportFile(
            stream.ToArray(),
            ExcelContentType,
            BuildFileName(report.GeneratedAtUtc, "xlsx"));
    }

    public Sprint5ReportExportFile CreatePdf(Sprint5AggregateReportDto report)
    {
        if (!_pdfExportEnabled)
            throw new PdfExportLicenseNotConfiguredException();

        Validate(report);

        var content = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(style => style.FontSize(9).FontFamily("Lato"));

                page.Header().Column(header =>
                {
                    header.Item().Text(report.ReportKind.GetTitle())
                        .FontSize(18)
                        .Bold()
                        .FontColor(QuestColors.Blue.Darken2);
                    header.Item().PaddingTop(4).Text(
                        $"Thời điểm xuất (UTC): {report.GeneratedAtUtc.UtcDateTime:dd/MM/yyyy HH:mm}");
                    header.Item().Text(FormatFilters(report));
                });

                page.Content().Column(contentColumn =>
                {
                    contentColumn.Spacing(12);
                    if (report.ReportKind.Includes(Sprint5ReportKind.MentorWorkload))
                        AddMentorSection(contentColumn, report);
                    if (report.ReportKind.Includes(Sprint5ReportKind.StudentDemographics))
                        AddDemographicSection(contentColumn, report);
                    if (report.ReportKind.Includes(Sprint5ReportKind.ProgramCompletion))
                        AddCompletionSection(contentColumn, report);
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return new Sprint5ReportExportFile(
            content,
            PdfContentType,
            BuildFileName(report.GeneratedAtUtc, "pdf"));
    }

    private static void Validate(Sprint5AggregateReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.GeneratedAtUtc == default)
        {
            throw new ArgumentException("A report generation timestamp is required.", nameof(report));
        }

        if (!Enum.IsDefined(report.ReportKind))
        {
            throw new ArgumentException("The report type is invalid.", nameof(report));
        }

        if (report.FromDate.HasValue &&
            report.ToDate.HasValue &&
            report.FromDate.Value > report.ToDate.Value)
        {
            throw new ArgumentException("The report start date must not be after its end date.", nameof(report));
        }

        if (!report.CompletionDataAvailable &&
            report.ProgramCompletions.Count != 0)
        {
            throw new ArgumentException(
                "Completion rows cannot be exported while completion data is unavailable.",
                nameof(report));
        }

        foreach (var item in report.MentorWorkloads)
        {
            if (item.AssignedStudentCount < 0)
            {
                throw new ArgumentException("Mentor workload counts cannot be negative.", nameof(report));
            }
        }

        foreach (var item in report.Demographics)
        {
            if (item.StudentCount < 0)
            {
                throw new ArgumentException("Demographic counts cannot be negative.", nameof(report));
            }
        }

        foreach (var item in report.ProgramCompletions)
        {
            _ = ProgramCompletionRate.CalculatePercent(item.EligibleCount, item.CompletedCount);
        }
    }

    private static void AddWorkbookStyles(WorkbookPart workbookPart)
    {
        var stylePart = workbookPart.AddNewPart<WorkbookStylesPart>();
        var stylesheet = new Stylesheet
        {
            Fonts = new OpenXmlFonts(
                new Font(),
                new Font(new Bold(), new OpenXmlColor { Rgb = "FFFFFFFF" }),
                new Font(new Bold(), new FontSize { Val = 14 })),
            Fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                new Fill(new PatternFill(
                    new ForegroundColor { Rgb = "FF1F4E78" })
                {
                    PatternType = PatternValues.Solid
                })),
            Borders = new Borders(new Border()),
            CellStyleFormats = new CellStyleFormats(new CellFormat()),
            CellFormats = new CellFormats(
                new CellFormat { FontId = 0, FillId = 0, BorderId = 0 },
                new CellFormat
                {
                    FontId = 1,
                    FillId = 2,
                    BorderId = 0,
                    ApplyFont = true,
                    ApplyFill = true
                },
                new CellFormat
                {
                    FontId = 2,
                    FillId = 0,
                    BorderId = 0,
                    ApplyFont = true
                })
        };

        stylePart.Stylesheet = stylesheet;
        stylesheet.Save();
    }

    private static void WriteReportRows(SheetData sheetData, Sprint5AggregateReportDto report)
    {
        uint rowNumber = 1;
        AppendTextRow(sheetData, rowNumber++, report.ReportKind.GetTitle(), styleIndex: 2);
        AppendTextRow(sheetData, rowNumber++, new[] { "Thời điểm xuất (UTC)", report.GeneratedAtUtc.UtcDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) });
        AppendTextRow(sheetData, rowNumber++, new[] { "Chương trình", string.IsNullOrWhiteSpace(report.ProgramFilter) ? "Tất cả chương trình" : report.ProgramFilter });
        AppendTextRow(sheetData, rowNumber++, new[] { "Trường", string.IsNullOrWhiteSpace(report.UniversityFilter) ? "Tất cả trường" : report.UniversityFilter });
        AppendTextRow(sheetData, rowNumber++, new[] { "Ngành", string.IsNullOrWhiteSpace(report.MajorFilter) ? "Tất cả ngành" : report.MajorFilter });
        AppendTextRow(sheetData, rowNumber++, new[] { "Khoảng thời gian", FormatDateRange(report) });
        rowNumber++;

        if (report.ReportKind.Includes(Sprint5ReportKind.MentorWorkload))
        {
            AppendTextRow(sheetData, rowNumber++, "Thực tập sinh theo Mentor", styleIndex: 2);
            AppendTextRow(sheetData, rowNumber++, new[] { "Mentor", "Phòng ban", "Số thực tập sinh" }, 1);
            if (report.MentorWorkloads.Count == 0)
            {
                AppendTextRow(sheetData, rowNumber++, "Không có dữ liệu");
            }
            else
            {
                foreach (var item in report.MentorWorkloads)
                {
                    AppendTextRow(
                        sheetData,
                        rowNumber++,
                        new[] { item.MentorName, item.Department, item.AssignedStudentCount.ToString(CultureInfo.InvariantCulture) });
                }
            }
            rowNumber++;
        }

        if (report.ReportKind.Includes(Sprint5ReportKind.StudentDemographics))
        {
            var universityStats = AggregateDemographics(report.Demographics, row => row.University);
            AppendTextRow(sheetData, rowNumber++, "Số lượng theo trường", styleIndex: 2);
            AppendTextRow(sheetData, rowNumber++, new[] { "Trường", "Số sinh viên" }, 1);
            if (universityStats.Count == 0)
            {
                AppendTextRow(sheetData, rowNumber++, "Không có dữ liệu");
            }
            else
            {
                foreach (var item in universityStats)
                {
                    AppendTextRow(sheetData, rowNumber++, new[] { item.Name, item.StudentCount.ToString(CultureInfo.InvariantCulture) });
                }
            }

            rowNumber++;
            var majorStats = AggregateDemographics(report.Demographics, row => row.Major);
            AppendTextRow(sheetData, rowNumber++, "Số lượng theo ngành", styleIndex: 2);
            AppendTextRow(sheetData, rowNumber++, new[] { "Ngành", "Số sinh viên" }, 1);
            if (majorStats.Count == 0)
            {
                AppendTextRow(sheetData, rowNumber++, "Không có dữ liệu");
            }
            else
            {
                foreach (var item in majorStats)
                {
                    AppendTextRow(sheetData, rowNumber++, new[] { item.Name, item.StudentCount.ToString(CultureInfo.InvariantCulture) });
                }
            }

            rowNumber++;
        }

        if (report.ReportKind.Includes(Sprint5ReportKind.ProgramCompletion))
        {
            AppendTextRow(sheetData, rowNumber++, "Tỷ lệ hoàn thành chương trình", styleIndex: 2);
            if (!report.CompletionDataAvailable)
            {
                AppendTextRow(
                    sheetData,
                    rowNumber,
                    $"Chưa có dữ liệu hoàn thành đã được xác minh. {report.CompletionUnavailableReason}".Trim());
                return;
            }

            AppendTextRow(sheetData, rowNumber++, new[] { "Chương trình", "Tổng hợp lệ", "Đã xác nhận", "Tỷ lệ (%)" }, 1);
            if (report.ProgramCompletions.Count == 0)
            {
                AppendTextRow(sheetData, rowNumber, "Không có dữ liệu");
                return;
            }

            foreach (var item in report.ProgramCompletions)
            {
                var rate = ProgramCompletionRate.CalculatePercent(item.EligibleCount, item.CompletedCount);
                AppendTextRow(
                    sheetData,
                    rowNumber++,
                    new[] { item.ProgramName, item.EligibleCount.ToString(CultureInfo.InvariantCulture), item.CompletedCount.ToString(CultureInfo.InvariantCulture), rate?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Không có dữ liệu" });
            }
        }
    }

    private static void AppendTextRow(SheetData sheetData, uint rowNumber, string value, uint styleIndex = 0)
    {
        AppendTextRow(sheetData, rowNumber, new[] { value }, styleIndex);
    }

    private static void AppendTextRow(SheetData sheetData, uint rowNumber, string[] values, uint styleIndex = 0)
    {
        var row = new Row { RowIndex = rowNumber };
        for (var index = 0; index < values.Length; index++)
        {
            var columnName = ColumnName(index + 1);
            row.Append(new Cell
            {
                CellReference = $"{columnName}{rowNumber}",
                DataType = CellValues.InlineString,
                StyleIndex = styleIndex,
                InlineString = new InlineString(
                    new Text(values[index] ?? string.Empty)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    })
            });
        }

        sheetData.Append(row);
    }

    private static string ColumnName(int columnNumber)
    {
        var name = string.Empty;
        while (columnNumber > 0)
        {
            var remainder = (columnNumber - 1) % 26;
            name = (char)('A' + remainder) + name;
            columnNumber = (columnNumber - 1) / 26;
        }

        return name;
    }

    private static void AddMentorSection(ColumnDescriptor content, Sprint5AggregateReportDto report)
    {
        content.Item().Text("Thực tập sinh theo Mentor").FontSize(13).Bold();
        if (report.MentorWorkloads.Count == 0)
        {
            content.Item().Text("Không có dữ liệu.");
            return;
        }

        content.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });
            AddHeaderCell(table, "Mentor");
            AddHeaderCell(table, "Phòng ban");
            AddHeaderCell(table, "Số thực tập sinh");

            foreach (var item in report.MentorWorkloads)
            {
                AddBodyCell(table, item.MentorName);
                AddBodyCell(table, item.Department);
                AddBodyCell(table, item.AssignedStudentCount.ToString(CultureInfo.InvariantCulture));
            }
        });
    }

    private sealed record DemographicCount(string Name, int StudentCount);

    private static IReadOnlyList<DemographicCount> AggregateDemographics(
        IReadOnlyList<StudentDemographicPairDto> demographics,
        Func<StudentDemographicPairDto, string> selector)
    {
        var vietnameseComparer = StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), ignoreCase: false);
        return demographics
            .GroupBy(selector, StringComparer.Ordinal)
            .Select(group => new DemographicCount(group.Key, group.Sum(item => item.StudentCount)))
            .OrderByDescending(item => item.StudentCount)
            .ThenBy(item => item.Name, vietnameseComparer)
            .ToArray();
    }

    private static void AddDemographicSection(ColumnDescriptor content, Sprint5AggregateReportDto report)
    {
        content.Item().Text("Thống kê theo trường/ngành").FontSize(13).Bold();
        AddDimensionTable(
            content,
            "Số lượng theo trường",
            "Trường",
            AggregateDemographics(report.Demographics, item => item.University));
        AddDimensionTable(
            content,
            "Số lượng theo ngành",
            "Ngành",
            AggregateDemographics(report.Demographics, item => item.Major));
    }

    private static void AddDimensionTable(
        ColumnDescriptor content,
        string title,
        string dimensionHeader,
        IReadOnlyList<DemographicCount> rows)
    {
        content.Item().Text(title).FontSize(11).Bold();
        if (rows.Count == 0)
        {
            content.Item().Text("Không có dữ liệu.");
            return;
        }

        content.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });
            AddHeaderCell(table, dimensionHeader);
            AddHeaderCell(table, "Số sinh viên");

            foreach (var row in rows)
            {
                AddBodyCell(table, row.Name);
                AddBodyCell(table, row.StudentCount.ToString(CultureInfo.InvariantCulture));
            }
        });
    }
    private static void AddCompletionSection(ColumnDescriptor content, Sprint5AggregateReportDto report)
    {
        content.Item().Text("Tỷ lệ hoàn thành chương trình").FontSize(13).Bold();
        if (!report.CompletionDataAvailable)
        {
            content.Item().Text(
                $"Chưa có dữ liệu hoàn thành đã được xác minh. {report.CompletionUnavailableReason}".Trim());
            return;
        }

        if (report.ProgramCompletions.Count == 0)
        {
            content.Item().Text("Không có dữ liệu.");
            return;
        }

        content.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });
            AddHeaderCell(table, "Chương trình");
            AddHeaderCell(table, "Tổng hợp lệ");
            AddHeaderCell(table, "Đã xác nhận");
            AddHeaderCell(table, "Tỷ lệ");

            foreach (var item in report.ProgramCompletions)
            {
                var rate = ProgramCompletionRate.CalculatePercent(item.EligibleCount, item.CompletedCount);
                AddBodyCell(table, item.ProgramName);
                AddBodyCell(table, item.EligibleCount.ToString(CultureInfo.InvariantCulture));
                AddBodyCell(table, item.CompletedCount.ToString(CultureInfo.InvariantCulture));
                AddBodyCell(table, rate.HasValue ? $"{rate.Value:0.##}%" : "Không có dữ liệu");
            }
        });
    }

    private static void AddHeaderCell(TableDescriptor table, string value)
    {
        table.Cell()
            .Element(container => container
                .Background(QuestColors.Blue.Darken2)
                .Padding(5))
            .Text(value)
            .Bold()
            .FontColor(QuestColors.White);
    }

    private static void AddBodyCell(TableDescriptor table, string value)
    {
        table.Cell()
            .Element(container => container
                .BorderBottom(0.5f)
                .BorderColor(QuestColors.Grey.Lighten2)
                .Padding(5))
            .Text(value);
    }

    private static string FormatFilters(Sprint5AggregateReportDto report)
    {
        var program = string.IsNullOrWhiteSpace(report.ProgramFilter)
            ? "Tất cả chương trình"
            : report.ProgramFilter;
        var university = string.IsNullOrWhiteSpace(report.UniversityFilter)
            ? "Tất cả trường"
            : report.UniversityFilter;
        var major = string.IsNullOrWhiteSpace(report.MajorFilter)
            ? "Tất cả ngành"
            : report.MajorFilter;
        return $"Bộ lọc: {program}; trường: {university}; ngành: {major}; thời gian: {FormatDateRange(report)}";
    }
    private static string FormatDateRange(Sprint5AggregateReportDto report)
    {
        var from = report.FromDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Không giới hạn";
        var to = report.ToDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Không giới hạn";
        return $"{from} - {to}";
    }

    private static string BuildFileName(DateTimeOffset timestamp, string extension)
    {
        return $"bao-cao-thuc-tap-{timestamp.UtcDateTime:yyyyMMdd_HHmmss}.{extension}";
    }
}
