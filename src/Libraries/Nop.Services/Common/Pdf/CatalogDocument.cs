using PdfRpt.Core.Contracts;

namespace Nop.Services.Common.Pdf;

/// <summary>
/// Represents the catalog document
/// </summary>
public partial class CatalogDocument : PdfDocument<CatalogItem>
{
    #region Methods

    /// <summary>
    /// Generate the catalog
    /// </summary>
    /// <param name="pdfStreamOutput">Stream for PDF output</param>
    public override void Generate(Stream pdfStreamOutput)
    {
        Document
            .MainTablePreferences(table =>
            {
                table.ColumnsWidthsType(TableColumnWidthType.Relative);
                table.NumberOfDataRowsPerPage(1);
            })
            .MainTableDataSource(dataSource =>
            {
                dataSource.StronglyTypedList(Products);
            })
            .MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.ConfigureProductColumn<CatalogItem>(p => p.Name, Language, PdfDocumentHelper.GetFont(FontFamily));
                    column.HeaderCell(" ", mergeHeaderCell: true);
                    column.ColumnItemsTemplate(itemsTemplate =>
                    {
                        itemsTemplate.CustomTemplate(new CatalogCellTemplate(FontFamily, Language));
                    });
                });

            })
            .Generate(builder => builder.AsPdfStream(pdfStreamOutput, closeStream: false));
    }

    #endregion
}