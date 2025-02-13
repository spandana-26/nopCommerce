using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using PdfRpt.Core.Contracts;

namespace Nop.Services.Common.Pdf;

/// <summary>
/// Extensions
/// </summary>
public static partial class PdfDocumentHelper
{
    #region Constants

    public const int BASE_FONT_SIZE = 10;
    public const float RELATIVE_LEADING = 1.3f;

    /// <summary>
    /// ~/App_Data/Pdf/Vazirmatn.ttf
    /// </summary>
    public const string RIGHT_TO_LEFT_FONT_NAME = "Vazirmatn";

    /// <summary>
    /// ~/App_Data/Pdf/OpenSans.ttf
    /// </summary>
    public const string LEFT_TO_RIGHT_FONT_NAME = "OpenSans";

    #endregion

    #region Methods

    /// <summary>
    /// Build a cell with a hyperlink 
    /// </summary>
    /// <param name="labelSelector">Property selector to get resource key annotation</param>
    /// <param name="font">Font</param>
    /// <param name="language">Language</param>
    /// <param name="url">URL</param>
    /// <returns>A cell for PDF table</returns>
    public static PdfPCell BuildHyperLinkCell<TLabel>(Expression<Func<TLabel, string>> labelSelector, Font font, Language language, string url)
    {
        ArgumentNullException.ThrowIfNull(labelSelector);
        ArgumentNullException.ThrowIfNullOrEmpty(url);

        var content = new Phrase();
        var label = LabelField(labelSelector, font, language?.Id ?? 0);

        if (label.IsEmpty())
            label.Append(url);

        content.Add(new Anchor(label) { Reference = url });

        var cell = new PdfPCell(content)
        {
            HorizontalAlignment = Element.ALIGN_LEFT,
            RunDirection = language.GetPdfRunDirection(),
            Border = 0,
            Padding = 3
        };

        cell.SetLeading(0f, RELATIVE_LEADING);

        return cell;
    }

    /// <summary>
    /// Build a cell with the given property
    /// </summary>
    /// <param name="labelSelector">Property selector to get resource key annotation</param>
    /// <param name="value">Value to format</param>
    /// <param name="font">Font</param>
    /// <param name="language">Language</param>
    /// <param name="horizontalAlign">Horizontal alignment</param>
    /// <returns>A cell for PDF table</returns>
    public static PdfPCell BuildPdfPCell<TLabel>(Expression<Func<TLabel, string>> labelSelector, string value, Font font, Language language, int horizontalAlign = Element.ALIGN_LEFT)
    {
        var label = LabelField(labelSelector, font, language?.Id ?? 0, value);

        var cell = new PdfPCell(new Phrase() { label })
        {
            RunDirection = language.GetPdfRunDirection(),
            HorizontalAlignment = horizontalAlign,
            VerticalAlignment = Element.ALIGN_CENTER,
            Border = 0,
            Padding = 3
        };

        cell.SetLeading(0f, RELATIVE_LEADING);

        return cell;
    }

    /// <summary>
    /// Build a cell with the given table
    /// </summary>
    /// <param name="table">PDF table</param>
    /// <param name="language">Language</param>
    /// <param name="collSpan">The number of columns occupied by a cell</param>
    /// <param name="horizontalAlign">Horizontal alignment</param>
    /// <returns>A cell for PDF table</returns>
    public static PdfPCell BuildPdfPCell(PdfGrid table, Language language, int collSpan = 1, int horizontalAlign = Element.ALIGN_CENTER)
    {
        var cell = new PdfPCell(table)
        {
            RunDirection = language.GetPdfRunDirection(),
            Colspan = collSpan,
            HorizontalAlignment = horizontalAlign,
            Border = 0
        };

        cell.SetLeading(0f, RELATIVE_LEADING);

        return cell;
    }

    /// <summary>
    /// Build a cell with the given text
    /// </summary>
    /// <param name="text">Text</param>
    /// <param name="language">Language</param>
    /// <param name="font">Font</param>
    /// <param name="collSpan">The number of columns occupied by a cell</param>
    /// <param name="horizontalAlign">Horizontal alignment</param>
    /// <param name="verticalAlignment">Vertical alignment</param>
    /// <returns>A cell for PDF table</returns>
    public static PdfPCell BuildPdfPCell(string text, Language language, Font font, int collSpan = 1, int horizontalAlign = Element.ALIGN_LEFT, int verticalAlignment = Element.ALIGN_CENTER)
    {
        var cell = new PdfPCell(new Phrase(text, font))
        {
            HorizontalAlignment = horizontalAlign,
            VerticalAlignment = verticalAlignment,
            Colspan = collSpan,
            RunDirection = language.GetPdfRunDirection(),
            Border = 0,
            Padding = 3
        };

        cell.SetLeading(0f, RELATIVE_LEADING);

        return cell;
    }

    /// <summary>
    /// Build default PDF table
    /// </summary>
    /// <param name="numColumns">The number of columns</param>
    /// <param name="language">Language</param>
    /// <returns>A PDF table</returns>
    public static PdfGrid BuildPdfGrid(int numColumns, Language language)
    {
        return new PdfGrid(numColumns: numColumns)
        {
            WidthPercentage = 100,
            RunDirection = language.GetPdfRunDirection(),
            HorizontalAlignment = Element.ALIGN_LEFT,
            SpacingAfter = 10
        };
    }

    /// <summary>
    /// Get a font
    /// </summary>
    /// <param name="fontName">The name of the font</param>
    /// <param name="size">The size of this font</param>
    /// <param name="style">The style of this font</param>
    /// <returns>A font object</returns>
    public static Font GetFont(string fontName, float size = BASE_FONT_SIZE, DocumentFontStyle style = DocumentFontStyle.Normal)
    {
        return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, true, size, (int)style, BaseColor.Black);
    }

    /// <summary>
    /// Get a label for the given property
    /// </summary>
    /// <param name="propertyExpression">Property selector to get resource key annotation</param>
    /// <param name="font">Font</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="args">Array of objects to format the resource string</param>
    /// <returns>A chunk with localized annotation if present, otherwise an empty chunk</returns>
    public static Chunk LabelField<TLabel, TOut>(Expression<Func<TLabel, TOut>> propertyExpression, Font font, int languageId, params string[] args)
    {
        var expression = (MemberExpression)propertyExpression.Body;
        var propertyInfo = (PropertyInfo)expression.Member;
        var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

        var label = propertyInfo
            .GetCustomAttributes<DisplayNameAttribute>(true)
            .FirstOrDefault() is DisplayNameAttribute attr ?
            localizationService.GetResourceAsync(attr.DisplayName, languageId).Result : string.Empty;

        if (!string.IsNullOrEmpty(label) && args.Any())
            label = string.Format(label, args);

        return new Chunk(label, font);
    }

    #endregion
}
