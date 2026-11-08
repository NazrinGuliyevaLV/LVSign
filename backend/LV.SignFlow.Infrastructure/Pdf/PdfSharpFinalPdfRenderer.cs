using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Enums.SigningEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using LV.SignFlow.Infrastructure.Pdf.Fonts;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LV.SignFlow.Infrastructure.Pdf
{
    public sealed class PdfSharpFinalPdfRenderer
    : IFinalPdfRenderer
    {
        private readonly IFileStorage _fileStorage;

        public PdfSharpFinalPdfRenderer(
            IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public async Task<byte[]> RenderAsync(
            Stream originalPdf,
            IReadOnlyCollection<FinalPdfRenderField> fields,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(originalPdf);
            ArgumentNullException.ThrowIfNull(fields);

            if (!originalPdf.CanRead)
            {
                throw new InvalidOperationException(
                    "Original PDF stream is not readable.");
            }

            if (originalPdf.CanSeek)
            {
                originalPdf.Position = 0;
            }

            using var document =
                PdfReader.Open(
                    originalPdf,
                    PdfDocumentOpenMode.Modify);

            foreach (var pageGroup in fields
                         .Where(x => x.PageNumber > 0)
                         .GroupBy(x => x.PageNumber))
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var pageIndex =
                    pageGroup.Key - 1;

                if (
                    pageIndex < 0 ||
                    pageIndex >= document.PageCount)
                {
                    throw new InvalidOperationException(
                        $"Field references invalid PDF page {pageGroup.Key}.");
                }

                var page =
                    document.Pages[pageIndex];

                using var graphics =
                    XGraphics.FromPdfPage(page);

                foreach (var field in pageGroup)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    await DrawFieldAsync(
                        graphics,
                        page.Width.Point,
                        page.Height.Point,
                        field,
                        cancellationToken);
                }
            }

            using var output =
                new MemoryStream();

            document.Save(
                output,
                closeStream: false);

            return output.ToArray();
        }

        private async Task DrawFieldAsync(
            XGraphics graphics,
            double pageWidth,
            double pageHeight,
            FinalPdfRenderField field,
            CancellationToken cancellationToken)
        {
            var rect =
                ToPdfRect(
                    field,
                    pageWidth,
                    pageHeight);

            switch (field.FieldType)
            {
                case FieldType.Signature:
                case FieldType.Initial:
                    await DrawSignatureAsync(
                        graphics,
                        rect,
                        field,
                        cancellationToken);
                    break;

                case FieldType.Stamp:
                    await DrawImageMarkAsync(
                        graphics,
                        rect,
                        field,
                        cancellationToken);
                    break;

                case FieldType.Checkbox:
                    DrawCheckbox(
                        graphics,
                        rect,
                        field.ValueBoolean == true);
                    break;

                default:
                    DrawTextField(
                        graphics,
                        rect,
                        field);
                    break;
            }
        }

        private async Task DrawSignatureAsync(
            XGraphics graphics,
            XRect rect,
            FinalPdfRenderField field,
            CancellationToken cancellationToken)
        {
            if (
                field.SignatureTypeSnapshot ==
                SignatureType.Typed)
            {
                DrawTypedSignature(
                    graphics,
                    rect,
                    field);

                return;
            }

            await DrawImageMarkAsync(
                graphics,
                rect,
                field,
                cancellationToken);
        }

        private static void DrawTypedSignature(
            XGraphics graphics,
            XRect rect,
            FinalPdfRenderField field)
        {
            if (string.IsNullOrWhiteSpace(
                    field.TypedTextSnapshot))
            {
                throw new InvalidOperationException(
                    $"Typed signature text is missing for field '{field.FieldId}'.");
            }

            var family =
                LvSignFontMapper.GetSignatureFamily(
                    field.FontKeySnapshot);

            var fontSize =
                CalculateSignatureFontSize(
                    field.TypedTextSnapshot,
                    rect);

            var font =
                new XFont(
                    family,
                    fontSize,
                    XFontStyleEx.Regular);

            graphics.DrawString(
                field.TypedTextSnapshot,
                font,
                XBrushes.Black,
                rect,
                XStringFormats.Center);
        }

        private async Task DrawImageMarkAsync(
            XGraphics graphics,
            XRect rect,
            FinalPdfRenderField field,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    field.AppliedBlobPath))
            {
                throw new InvalidOperationException(
                    $"Applied mark asset is missing for field '{field.FieldId}'.");
            }

            var contentType =
                field.AppliedContentType?
                    .Trim()
                    .ToLowerInvariant();

            if (
                contentType != "image/png" &&
                contentType != "image/jpeg" &&
                contentType != "image/jpg" &&
                contentType != "image/bmp")
            {
                throw new InvalidOperationException(
                    $"Unsupported applied mark image format '{field.AppliedContentType}'.");
            }

            await using var source =
                await _fileStorage.OpenReadAsync(
                    field.AppliedBlobPath,
                    cancellationToken);

            using var memory =
                new MemoryStream();

            await source.CopyToAsync(
                memory,
                cancellationToken);

            var bytes =
                memory.ToArray();

            if (bytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Applied mark asset is empty for field '{field.FieldId}'.");
            }

            using var imageStream =
                new MemoryStream(
                    bytes,
                    0,
                    bytes.Length,
                    writable: false,
                    publiclyVisible: true);

            using var image =
                XImage.FromStream(
                    imageStream);

            var destination =
                CalculateImageFit(
                    rect,
                    image.PixelWidth,
                    image.PixelHeight);

            graphics.DrawImage(
                image,
                destination);
        }

        private static void DrawTextField(
            XGraphics graphics,
            XRect rect,
            FinalPdfRenderField field)
        {
            var text =
                GetDisplayText(field);

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var fontSize =
                Math.Clamp(
                    rect.Height * 0.55,
                    7,
                    12);

            var font =
                new XFont(
                    LvSignFontFamilies.Sans,
                    fontSize,
                    XFontStyleEx.Regular);

            graphics.DrawString(
                text,
                font,
                XBrushes.Black,
                rect,
                XStringFormats.CenterLeft);
        }

        private static void DrawCheckbox(
            XGraphics graphics,
            XRect rect,
            bool isChecked)
        {
            if (!isChecked)
            {
                return;
            }

            var size =
                Math.Min(
                    rect.Width,
                    rect.Height) * 0.70;

            var x =
                rect.X +
                ((rect.Width - size) / 2);

            var y =
                rect.Y +
                ((rect.Height - size) / 2);

            var pen =
                new XPen(
                    XColors.Black,
                    Math.Max(1, size * 0.08));

            graphics.DrawLine(
                pen,
                x,
                y,
                x + size,
                y + size);

            graphics.DrawLine(
                pen,
                x + size,
                y,
                x,
                y + size);
        }

        private static string? GetDisplayText(
            FinalPdfRenderField field)
        {
            return field.FieldType switch
            {
                FieldType.Number =>
                    field.ValueNumber?.ToString(
                        CultureInfo.InvariantCulture),

                FieldType.DateSigned =>
                    field.ValueDate?.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),

                FieldType.Checkbox =>
                    null,

                _ =>
                    field.ValueText
            };
        }

        private static XRect ToPdfRect(
            FinalPdfRenderField field,
            double pageWidth,
            double pageHeight)
        {
            /*
             * Field coordinates DB-də 0..1 normalized-dir.
             */
            var x =
                (double)field.X *
                pageWidth;

            var y =
                (double)field.Y *
                pageHeight;

            var width =
                (double)field.Width *
                pageWidth;

            var height =
                (double)field.Height *
                pageHeight;

            if (
                x < 0 ||
                y < 0 ||
                width <= 0 ||
                height <= 0 ||
                x + width > pageWidth + 1 ||
                y + height > pageHeight + 1)
            {
                throw new InvalidOperationException(
                    $"Invalid normalized coordinates for field '{field.FieldId}'.");
            }

            return new XRect(
                x,
                y,
                width,
                height);
        }

        private static double CalculateSignatureFontSize(
            string text,
            XRect rect)
        {
            var byHeight =
                rect.Height * 0.72;

            /*
             * Handwriting font üçün sadə width approximation.
             * Sonrakı mərhələdə lazım olsa MeasureString ilə
             * iterative fitting edə bilərik.
             */
            var estimatedCharacterWidth =
                0.48;

            var byWidth =
                rect.Width /
                Math.Max(
                    1,
                    text.Length *
                    estimatedCharacterWidth);

            return Math.Clamp(
                Math.Min(
                    byHeight,
                    byWidth),
                8,
                42);
        }

        private static XRect CalculateImageFit(
            XRect container,
            double imageWidth,
            double imageHeight)
        {
            if (
                imageWidth <= 0 ||
                imageHeight <= 0)
            {
                throw new InvalidOperationException(
                    "Applied mark image dimensions are invalid.");
            }

            var scale =
                Math.Min(
                    container.Width / imageWidth,
                    container.Height / imageHeight);

            var width =
                imageWidth * scale;

            var height =
                imageHeight * scale;

            return new XRect(
                container.X +
                ((container.Width - width) / 2),

                container.Y +
                ((container.Height - height) / 2),

                width,
                height);
        }
    }
    }
