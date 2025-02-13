using System.ComponentModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;

namespace Nop.Services.Common.Pdf;

/// <summary>
/// Represents the invoice document
/// </summary>
public partial class InvoiceDocument : PdfDocument<ProductItem>
{
    #region Utilities

    private PdfGrid CreateAdressesInfo()
    {
        var font = PdfDocumentHelper.GetFont(FontFamily);
        var billingInfo = PdfDocumentHelper.BuildPdfPCell(
            BuildAddressTable<InvoiceDocument>(source => BillingAddress, font, BillingAddress), Language);
        var shippingInfo = PdfDocumentHelper.BuildPdfPCell(
            BuildAddressTable<InvoiceDocument>(source => ShippingAddress, font, ShippingAddress), Language);

        var addressesTable = PdfDocumentHelper.BuildPdfGrid(numColumns: 2, Language);

        billingInfo.Padding = 5;
        shippingInfo.Padding = 5;

        addressesTable.AddCell(billingInfo);
        addressesTable.AddCell(shippingInfo);

        return addressesTable;
    }

    private PdfGrid CreateInvoiceHeader()
    {
        var headerTable = PdfDocumentHelper.BuildPdfGrid(numColumns: 2, Language);
        var font = PdfDocumentHelper.GetFont(FontFamily);

        var info = PdfDocumentHelper.BuildPdfGrid(numColumns: 1, Language);
        info.SpacingAfter = 15;

        info.AddCell(PdfDocumentHelper.BuildPdfPCell<InvoiceDocument>(source => OrderNumberText, OrderNumberText, font, Language));
        info.AddCell(PdfDocumentHelper.BuildHyperLinkCell<InvoiceDocument>(source => StoreUrl, font, Language, StoreUrl));
        info.AddTextCell<InvoiceDocument>(source => OrderDateUser, OrderDateUser, font, Language);

        headerTable.AddCell(PdfDocumentHelper.BuildPdfPCell(info, Language, horizontalAlign: Element.ALIGN_LEFT));

        if (LogoData is not null)
        {
            var logo = PdfImageHelper.GetITextSharpImageFromByteArray(LogoData);
            headerTable.AddCell(new PdfPCell(logo, fit: true)
            {
                Border = 0,
                FixedHeight = 65,
                RunDirection = Language.GetPdfRunDirection(),
                VerticalAlignment = Element.ALIGN_CENTER,
                HorizontalAlignment = Element.ALIGN_CENTER
            });
        }
        else
        {
            headerTable.AddEmptyCell();
        }

        headerTable.AddCell(PdfDocumentHelper.BuildPdfPCell(CreateAdressesInfo(), Language, collSpan: 2));

        return headerTable;
    }

    private PdfGrid CreateFooter(FooterData footerData)
    {
        var font = PdfDocumentHelper.GetFont(FontFamily);
        var footerTable = PdfDocumentHelper.BuildPdfGrid(numColumns: 2, Language);

        if (!FooterTextColumn1.Any() && !FooterTextColumn2.Any())
            return footerTable;

        var footer1Table = PdfDocumentHelper.BuildPdfGrid(numColumns: 1, Language);
        foreach (var line in FooterTextColumn1)
            footer1Table.AddCell(PdfDocumentHelper.BuildPdfPCell(line, Language, font));

        var footer2Table = PdfDocumentHelper.BuildPdfGrid(numColumns: 1, Language);
        foreach (var line in FooterTextColumn2)
            footer2Table.AddCell(PdfDocumentHelper.BuildPdfPCell(line, Language, font));

        footerTable.AddCell(PdfDocumentHelper.BuildPdfPCell(footer1Table, Language));
        footerTable.AddCell(PdfDocumentHelper.BuildPdfPCell(footer2Table, Language));

        footerTable.AddCell(PdfDocumentHelper.BuildPdfPCell($"- {footerData.CurrentPageNumber} -", Language, font, collSpan: 2, horizontalAlign: Element.ALIGN_CENTER));

        return footerTable;
    }

    private PdfGrid CreateSummary()
    {
        var summaryData = PdfDocumentHelper.BuildPdfGrid(numColumns: 1, Language);
        var font = PdfDocumentHelper.GetFont(FontFamily);

        if (!string.IsNullOrEmpty(Totals.SubTotal))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.SubTotal, Totals.SubTotal, font, Language);

        if (!string.IsNullOrEmpty(Totals.Discount))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.Discount, Totals.Discount, font, Language);
        if (!string.IsNullOrEmpty(Totals.Shipping))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.Shipping, Totals.Shipping, font, Language);
        if (!string.IsNullOrEmpty(Totals.PaymentMethodAdditionalFee))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.PaymentMethodAdditionalFee, Totals.PaymentMethodAdditionalFee, font, Language);
        if (!string.IsNullOrEmpty(Totals.Tax))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.Tax, Totals.Tax, font, Language);

        foreach (var rate in Totals.TaxRates)
            summaryData.AddCell(new PdfPCell(new Phrase(rate)));

        if (!string.IsNullOrEmpty(Totals.RewardPoints))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.Tax, Totals.RewardPoints, font, Language);
        if (!string.IsNullOrEmpty(Totals.OrderTotal))
            summaryData.AddTextCell<InvoiceTotals>(totals => totals.OrderTotal, Totals.OrderTotal, font, Language);

        return summaryData;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Generate the invoice
    /// </summary>
    /// <param name="pdfStreamOutput">Stream for PDF output</param>
    public override void Generate(Stream pdfStreamOutput)
    {
        Document
            .MainTablePreferences(table =>
            {
                table.ColumnsWidthsType(TableColumnWidthType.Relative);
            })
            .MainTableDataSource(dataSource =>
            {
                dataSource.StronglyTypedList(Products);
            })
            .PagesFooter(footer =>
            {
                footer.InlineFooter(inlineFooter =>
                {
                    inlineFooter.FooterProperties(new FooterBasicProperties
                    {
                        PdfFont = footer.PdfFont,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        RunDirection = PdfRunDirection.LeftToRight
                    });
                    inlineFooter.AddPageFooter(data => CreateFooter(data));
                });
            })
            .MainTableColumns(columns =>
            {
                var font = PdfDocumentHelper.GetFont(FontFamily);
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Name, Language, font, width: 10, printProductAttributes: true));
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Sku, Language, font, width: 3));
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Price, Language, font, width: 3));
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Quantity, Language, font, width: 2));
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Total, Language, font, width: 3));
            })
            .MainTableEvents(events =>
            {
                events.MainTableCreated(events =>
                {
                    //add to body, since adding hyperlinks to document header is not allowed
                    events.PdfDoc.Add(CreateInvoiceHeader());
                });
                events.MainTableAdded(events =>
                {
                    var summaryTable = PdfDocumentHelper.BuildPdfGrid(numColumns: 3, Language);
                    summaryTable.AddCell(new PdfPCell() { Colspan = 2, Border = 0 });
                    summaryTable.AddCell(PdfDocumentHelper.BuildPdfPCell(CreateSummary(), Language));
                    events.PdfDoc.Add(summaryTable);
                });
            })
            .Generate(builder => builder.AsPdfStream(pdfStreamOutput, closeStream: false));
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the logo binary
    /// </summary>
    public byte[] LogoData { get; set; }

    /// <summary>
    /// Gets or sets the date and time of order creation
    /// </summary>
    [DisplayName("Pdf.OrderDate")]
    public string OrderDateUser { get; set; }

    /// <summary>
    /// Gets or sets the order number
    /// </summary>
    [DisplayName("Pdf.Order")]
    public string OrderNumberText { get; set; }

    /// <summary>
    /// Gets or sets store location
    /// </summary>
    public string StoreUrl { get; set; }

    /// <summary>
    /// Gets or sets the billing address
    /// </summary>
    [DisplayName("Pdf.BillingInformation")]
    public AddressItem BillingAddress { get; set; }

    /// <summary>
    /// Gets or sets the shipping address
    /// </summary>
    [DisplayName("Pdf.ShippingInformation")]
    public AddressItem ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display product SKU in the invoice document
    /// </summary>
    public bool ShowSkuInProductList { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display vendor name in the invoice document
    /// </summary>
    public bool ShowVendorInProductList { get; set; }

    /// <summary>
    /// Gets or sets the checkout attribute description
    /// </summary>
    public string CheckoutAttributes { get; set; }

    /// <summary>
    /// Gets or sets order totals
    /// </summary>
    public InvoiceTotals Totals { get; set; } = new();

    /// <summary>
    /// Gets or sets order notes
    /// </summary>
    [DisplayName("Pdf.OrderNotes")]
    public List<(string, string)> OrderNotes { get; set; }

    /// <summary>
    /// Gets or sets the text that will appear at the bottom of invoice (column 1)
    /// </summary>
    public List<string> FooterTextColumn1 { get; set; } = new();

    /// <summary>
    /// Gets or sets the text that will appear at the bottom of invoice (column 2)
    /// </summary>
    public List<string> FooterTextColumn2 { get; set; } = new();

    #endregion
}