using System.Linq.Expressions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Nop.Services.Common.Pdf;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;
using PdfRpt.FluentInterface;
using Language = Nop.Core.Domain.Localization.Language;

/// <summary>
/// Extensions
/// </summary>
public static class PdfDocumentExtensions
{
    /// <summary>
    /// Add a cell for the given selector and text
    /// </summary>
    /// <param name="table">PDF table</param>
    /// <param name="labelSelector">Property selector to get resource key annotation</param>
    /// <param name="text">Text</param>
    /// <param name="font">Font</param>
    /// <param name="language">Language</param>
    public static void AddTextCell<TLabel>(this PdfPTable table, Expression<Func<TLabel, string>> labelSelector, string text, Font font, Language language)
    {
        ArgumentNullException.ThrowIfNull(labelSelector);
        ArgumentNullException.ThrowIfNullOrEmpty(text);

        var label = PdfDocumentHelper.LabelField(labelSelector, font, language?.Id ?? 0);

        var content = new Phrase() { label, new Chunk(":", font), new Chunk(" ", font), new Chunk(text, font) };
        var cell = new PdfPCell(content)
        {
            HorizontalAlignment = Element.ALIGN_LEFT,
            RunDirection = language.GetPdfRunDirection(),
            Border = 0,
            Padding = 3
        };

        cell.SetLeading(0f, PdfDocumentHelper.RELATIVE_LEADING);

        table.AddCell(cell);
    }

    /// <summary>
    /// Add an empty cell
    /// </summary>
    public static void AddEmptyCell(this PdfPTable table)
    {
        table.AddCell(new PdfPCell(new Phrase())
        {
            Border = 0,
            Padding = 0
        });
    }

    /// <summary>
    /// Specify default behavior for maintable column
    /// </summary>
    /// <param name="column">Column builder</param>
    /// <param name="propertyExpression">Property selector for cells in the column</param>
    /// <param name="language">Language</param>
    /// <param name="font">Font</param>
    /// <param name="width">The column's width according to the PdfRptPageSetup.MainTableColumnsWidthsType value</param>
    /// <param name="printProductAttributes">Indicates that product attribute descriptions should be printed if they exist</param>
    public static void ConfigureProductColumn<TItem>(this ColumnAttributesBuilder column,
        Expression<Func<TItem, object>> propertyExpression,
        Language language,
        Font font,
        int width = 1,
        bool printProductAttributes = false)
    {
        column.PropertyName(propertyExpression);
        column.CellsHorizontalAlignment(HorizontalAlignment.Left);
        column.IsVisible(true);
        column.Width(width);
        column.HeaderCell(PdfDocumentHelper.LabelField(propertyExpression, font, language.Id).Content, horizontalAlignment: HorizontalAlignment.Left);

        column.ColumnItemsTemplate(itemsTemplate =>
        {
            itemsTemplate.InlineField(inlineField =>
            {
                inlineField.RenderCell(cellData =>
                {
                    var table = new PdfGrid(numColumns: 1)
                    {
                        WidthPercentage = 100,
                        RunDirection = language.GetPdfRunDirection(),
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        SpacingAfter = 5,
                        SpacingBefore = 5
                    };

                    var data = cellData.Attributes.RowData.TableRowData;
                    var text = data.GetSafeStringValueOf(propertyExpression);

                    table.AddCell(PdfDocumentHelper.BuildPdfPCell(text, language, font, verticalAlignment: Element.ALIGN_TOP));

                    if (printProductAttributes)
                    {
                        var productAttributes = (List<string>)data.GetValueOf((ProductItem x) => x.ProductAttributes);
                        var font8Italic = PdfDocumentHelper.GetFont(font.Familyname, 8, DocumentFontStyle.Italic);

                        foreach (var pa in productAttributes)
                        {
                            table.AddCell(new PdfPCell(new Phrase(pa, font8Italic))
                            {
                                RunDirection = language.GetPdfRunDirection(),
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                Border = 0
                            });
                        }
                    }

                    return new PdfPCell(table)
                    {
                        RunDirection = language.GetPdfRunDirection(),
                        BorderWidthBottom = 2,
                        BorderColorBottom = BaseColor.LightGray,
                        MinimumHeight = 25,
                        VerticalAlignment = Element.ALIGN_CENTER
                    };
                });
            });
        });
    }

    /// <summary>
    /// Get document run direction
    /// </summary>
    /// <param name="language"></param>
    /// <returns>Run direction</returns>
    public static int GetPdfRunDirection(this Language language)
    {
        return language?.Rtl == true ? PdfWriter.RUN_DIRECTION_RTL : PdfWriter.RUN_DIRECTION_LTR;
    }

    /// <summary>
    /// Resolve font name
    /// </summary>
    /// <param name="language">Language</param>
    /// <param name="fontName">The name of the font</param>
    /// <returns><paramref name="fontName"/> or default value if null</returns>
    public static string ResolveFontName(this Language language, string fontName = null)
    {
        if (!string.IsNullOrEmpty(fontName))
            return fontName;

        return language?.Rtl == true ? PdfDocumentHelper.RIGHT_TO_LEFT_FONT_NAME : PdfDocumentHelper.LEFT_TO_RIGHT_FONT_NAME;
    }
}
