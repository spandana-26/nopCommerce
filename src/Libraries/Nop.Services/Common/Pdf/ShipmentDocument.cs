using System.ComponentModel;
using PdfRpt.Core.Contracts;

namespace Nop.Services.Common.Pdf;

/// <summary>
/// Represents the shipment document
/// </summary>
public partial class ShipmentDocument : PdfDocument<ProductItem>
{
    #region Utilities

    private PdfGrid CreateDefaultHeader()
    {
        var headerTable = PdfDocumentHelper.BuildPdfGrid(numColumns: 1, Language);

        var font = PdfDocumentHelper.GetFont(FontFamily);
        headerTable.AddCell(PdfDocumentHelper.BuildPdfPCell<ShipmentDocument>(source => OrderNumberText, OrderNumberText, font, Language));
        headerTable.AddCell(PdfDocumentHelper.BuildPdfPCell<ShipmentDocument>(source => ShipmentNumberText, ShipmentNumberText, font, Language));

        return headerTable;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Generate shipment
    /// </summary>
    /// <param name="pdfStreamOutput">the PDF file's stream</param>
    /// Close the document by closing the underlying stream. Its default value is true.
    /// If you want to access the PDF stream after it has been created, set it to false.
    /// </param>
    public override void Generate(Stream pdfStreamOutput)
    {
        var font = PdfDocumentHelper.GetFont(FontFamily);
        var languageId = Language?.Id ?? 0;

        Document
            .MainTablePreferences(table =>
            {
                table.ColumnsWidthsType(TableColumnWidthType.Relative);
            })
            .PagesHeader(header =>
            {
                header.InlineHeader(inlineHeader =>
                {
                    inlineHeader.AddPageHeader(data => CreateDefaultHeader());
                });
            })
            .MainTableDataSource(dataSource =>
            {
                dataSource.StronglyTypedList(Products);
            })
            .MainTableColumns(columns =>
            {
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Name, Language, font, width: 10));
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Sku, Language, font, width: 3));
                columns.AddColumn(column => column.ConfigureProductColumn<ProductItem>(p => p.Quantity, Language, font, width: 3));

            })
            .MainTableEvents(events => events.MainTableCreated(events =>
            {
                events.PdfDoc.Add(BuildAddressTable<ShipmentDocument>(p => p.Address, font, Address));
            }))
            .Generate(builder => builder.AsPdfStream(pdfStreamOutput, closeStream: false));
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the shipping address
    /// </summary>
    [DisplayName("Pdf.ShippingInformation")]
    public AddressItem Address { get; set; }

    /// <summary>
    /// Gets or sets the order number
    /// </summary>
    [DisplayName("Pdf.Order")]
    public string OrderNumberText { get; set; }

    /// <summary>
    /// Gets or sets the shipment number
    /// </summary>
    [DisplayName("Pdf.Shipment")]
    public string ShipmentNumberText { get; set; }

    #endregion
}