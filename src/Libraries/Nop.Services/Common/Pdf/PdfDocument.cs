using System.Linq.Expressions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Nop.Core.Domain.Localization;
using PdfRpt.Core.Contracts;
using PdfRpt.FluentInterface;

namespace Nop.Services.Common.Pdf;

/// <summary>
/// Represents base document class
/// </summary>
public abstract class PdfDocument<TItem>
{
    #region Fields

    private string _fontName;

    #endregion

    #region Utilities

    protected virtual PdfGrid BuildAddressTable<TLabel>(Expression<Func<TLabel, AddressItem>> labelSelector, Font font, AddressItem address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var addressTable = PdfDocumentHelper.BuildPdfGrid(numColumns: 1, Language);

        var fontBold = PdfDocumentHelper.GetFont(font.Familyname, font.Size, DocumentFontStyle.Bold);
        var label = PdfDocumentHelper.LabelField(labelSelector, fontBold, LanguageId);

        addressTable.AddCell(
            new PdfPCell(new Phrase(label))
            {
                HorizontalAlignment = Element.ALIGN_LEFT,
                RunDirection = Language.GetPdfRunDirection(),
                Border = 0,
                BorderWidthBottom = 2,
                BorderColorBottom = BaseColor.LightGray,
                PaddingBottom = 5
            });

        if (!string.IsNullOrEmpty(address?.Company))
            addressTable.AddTextCell<AddressItem>(address => address.Company, address.Company, font, Language);

        if (!string.IsNullOrEmpty(address?.Name))
            addressTable.AddTextCell<AddressItem>(address => address.Name, address?.Name, font, Language);

        if (!string.IsNullOrEmpty(address?.Phone))
            addressTable.AddTextCell<AddressItem>(address => address.Phone, address?.Phone, font, Language);

        if (!string.IsNullOrEmpty(address?.AddressLine))
            addressTable.AddTextCell<AddressItem>(address => address.AddressLine, address?.AddressLine, font, Language);

        if (!string.IsNullOrEmpty(address?.VATNumber))
            addressTable.AddTextCell<AddressItem>(address => address.VATNumber, address?.VATNumber, font, Language);

        if (!string.IsNullOrEmpty(address?.PaymentMethod))
            addressTable.AddTextCell<AddressItem>(address => address.PaymentMethod, address?.PaymentMethod, font, Language);

        if (!string.IsNullOrEmpty(address?.ShippingMethod))
            addressTable.AddTextCell<AddressItem>(address => address.ShippingMethod, address?.ShippingMethod, font, Language);

        if (address?.CustomValues.Any() == true)
        {
            foreach (var (key, value) in address.CustomValues)
            {
                addressTable.AddCell(new PdfPCell(
                    new Phrase {
                        new Chunk(key), new Chunk(":"), new Chunk(value.ToString())
                    }
                 ));
            }
        }

        return addressTable;
    }

    protected virtual PdfReport DefaultDocument()
    {
        return new PdfReport()
            .DocumentPreferences(doc =>
            {
                doc.RunDirection(IsRightToLeft ? PdfRunDirection.RightToLeft : PdfRunDirection.LeftToRight);
                doc.Orientation(PageOrientation.Portrait);
                doc.PageSize(PageSize);
            })
            .DefaultFonts(fonts =>
            {
                fonts.Size(PdfDocumentHelper.BASE_FONT_SIZE);
            })
            .MainTableEvents(events =>
            {
                events.CellCreated(args =>
                {
                    if (args.CellType == CellType.HeaderCell && !string.IsNullOrWhiteSpace(args.Cell.RowData.Value?.ToString()))
                    {
                        args.Cell.BasicProperties.BackgroundColor = BaseColor.LightGray;
                        args.Cell.BasicProperties.CellPadding = 5;
                    }
                });
            });
    }

    #endregion

    #region Methods

    /// <summary>
    /// Generate document
    /// </summary>
    /// <param name="pdfStreamOutput">Stream for PDF output</param>
    public abstract void Generate(Stream pdfStreamOutput);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the language context
    /// </summary>
    public required Language Language { get; init; }

    /// <summary>
    /// Gets the language Id
    /// </summary>
    public int LanguageId => Language?.Id ?? 0;

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public required PdfPageSize PageSize { get; init; }

    /// <summary>
    /// Gets or sets a value indicating that the text direction is from right to left
    /// </summary>
    public bool IsRightToLeft => Language.Rtl;

    /// <summary>
    /// Gets or sets the font name. Loaded from the ~/App_Data/Pdf directory during application start. The default font is Tahoma.
    /// </summary>
    public string FontFamily
    {
        get => Language.ResolveFontName(_fontName);
        set => _fontName = value;
    }

    /// <summary>
    /// Gets or sets a collection of shipping items
    /// </summary>
    public List<TItem> Products { get; init; }

    /// <summary>
    /// PDF document builder 
    /// </summary>
    protected PdfReport Document => DefaultDocument();

    #endregion
}