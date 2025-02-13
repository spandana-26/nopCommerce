using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;
using Language = Nop.Core.Domain.Localization.Language;

namespace Nop.Services.Common.Pdf;

/// <summary>
/// Represents cell tempate for the product name
/// </summary>
public partial class CatalogCellTemplate : IColumnItemsTemplate
{
    #region Ctor

    public CatalogCellTemplate(string fontName, Language language)
    {
        FontName = fontName;
        Language = language;
        RunDirection = language.Rtl ? PdfWriter.RUN_DIRECTION_RTL : PdfWriter.RUN_DIRECTION_LTR;
    }

    #endregion

    private Font GetFont(int size = 10, DocumentFontStyle style = DocumentFontStyle.Normal)
    {
        return FontFactory.GetFont(FontName, BaseFont.IDENTITY_H, true, size, (int)style, BaseColor.Black);
    }


    private PdfPCell BuildProductProperties(IList<CellData> rowData, Font font)
    {
        var table = PdfDocumentHelper.BuildPdfGrid(1, Language);

        var price = rowData.GetSafeStringValueOf<CatalogItem>(p => p.Price);
        if (!string.IsNullOrEmpty(price))
            table.AddTextCell<CatalogItem>(p => p.Price, price, font, Language);

        var sku = rowData.GetSafeStringValueOf<CatalogItem>(p => p.Sku);
        if (!string.IsNullOrEmpty(sku))
            table.AddTextCell<CatalogItem>(p => p.Sku, sku, font, Language);

        var weight = rowData.GetSafeStringValueOf<CatalogItem>(p => p.Weight);
        if (!string.IsNullOrEmpty(weight))
            table.AddTextCell<CatalogItem>(p => p.Weight, weight, font, Language);

        var stock = rowData.GetSafeStringValueOf<CatalogItem>(p => p.Stock);
        if (!string.IsNullOrEmpty(stock))
            table.AddTextCell<CatalogItem>(p => p.Stock, stock, font, Language);

        return PdfDocumentHelper.BuildPdfPCell(table, Language);
    }

    private PdfPCell BuildProductImages(IList<CellData> rowData)
    {
        var table = PdfDocumentHelper.BuildPdfGrid(2, Language);

        table.HorizontalAlignment = Element.ALIGN_CENTER;

        var picPaths = (HashSet<string>)rowData.GetValueOf<CatalogItem>(x => x.PicturePaths);
        if (picPaths?.Any() == true)
        {
            foreach (var path in picPaths)
            {
                var imageCell = new PdfPCell(PdfImageHelper.GetITextSharpImageFromImageFile(path, false), fit: false)
                {
                    Border = 0,
                    Padding = 10,
                    VerticalAlignment = Element.ALIGN_CENTER,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };

                if (picPaths.Count % 2 != 0 && picPaths.Last() == path)
                    imageCell.Colspan = 2;

                table.AddCell(imageCell);
            }
        }
        return PdfDocumentHelper.BuildPdfPCell(table, Language, horizontalAlign: Element.ALIGN_CENTER);
    }

    #region Methods

    /// <summary>
    /// This method is called at the end of the cell's rendering.
    /// </summary>
    /// <param name="cell">The current cell</param>
    /// <param name="position">The coordinates of the cell</param>
    /// <param name="canvases">An array of PdfContentByte to add text or graphics</param>
    /// <param name="attributes">Current cell's custom attributes</param>
    public void CellRendered(PdfPCell cell, Rectangle position, PdfContentByte[] canvases, CellAttributes attributes)
    {
    }

    /// <summary>
    /// Custom cell's content template as a PdfPCell
    /// </summary>
    /// <returns>Content as a PdfPCell</returns>
    public PdfPCell RenderingCell(CellAttributes attributes)
    {
        var font = GetFont();

        var font16Bold = GetFont(16, DocumentFontStyle.Bold);

        var table = new PdfGrid(numColumns: 1)
        {
            WidthPercentage = 100,
            RunDirection = RunDirection,
            HorizontalAlignment = Element.ALIGN_LEFT
        };

        var name = attributes.RowData.TableRowData.GetSafeStringValueOf<CatalogItem>(p => p.Name);
        table.AddCell(new PdfPCell(new Phrase(name, font16Bold))
        {
            RunDirection = RunDirection,
            HorizontalAlignment = Element.ALIGN_LEFT,
            VerticalAlignment = Element.ALIGN_TOP,
            PaddingBottom = 14,
            Border = 0
        });


        var description = attributes.RowData.TableRowData.GetSafeStringValueOf<CatalogItem>(p => p.Description);

        var cell = new PdfPCell(new Paragraph(description, font))
        {
            RunDirection = RunDirection,
            HorizontalAlignment = Element.ALIGN_LEFT,
            VerticalAlignment = Element.ALIGN_TOP,
            Border = 0,
            PaddingBottom = 10
        };
        cell.SetLeading(0f, 1.3f);
        table.AddCell(cell);

        table.AddCell(BuildProductProperties(attributes.RowData.TableRowData, font));
        table.AddCell(BuildProductImages(attributes.RowData.TableRowData));

        return new PdfPCell(table)
        {
            RunDirection = RunDirection,
            Border = 0,
            MinimumHeight = 25,
            VerticalAlignment = Element.ALIGN_TOP
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Table's Cells Definitions. If you don't set this value, it will be filled by using current template's settings internally.
    /// </summary>
    public CellBasicProperties BasicProperties { get; set; }

    /// <summary>
    /// Defines the current cell's properties, based on the other cells values. 
    /// Here IList contains actual row's cells values.
    /// It can be null.
    /// </summary>
    public Func<IList<CellData>, CellBasicProperties> ConditionalFormatFormula { set; get; }

    public Language Language { get; init; }
    public string FontName { get; init; }
    public int RunDirection { get; init; }

    #endregion
}
