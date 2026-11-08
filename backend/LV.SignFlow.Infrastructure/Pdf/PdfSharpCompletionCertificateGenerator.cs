using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Infrastructure.Pdf.Fonts;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LV.SignFlow.Infrastructure.Pdf
{

    public sealed class PdfSharpCompletionCertificateGenerator
         : ICompletionCertificateGenerator
    {
        private const double Margin = 45;
        private const double BottomMargin = 45;
        private const double LineHeight = 16;

        public Task<byte[]> GenerateAsync(
            CompletionCertificateData data,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            cancellationToken.ThrowIfCancellationRequested();

            if (data.EnvelopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Envelope id is required.",
                    nameof(data));
            }

            if (data.CompletedAt == default)
            {
                throw new InvalidOperationException(
                    "Envelope completion date is required.");
            }

            using var document =
                new PdfDocument();

            document.Info.Title =
                $"Certificate of Completion - {data.Subject}";

            document.Info.Subject =
                "LV.SignFlow Certificate of Completion";

            document.Info.Creator =
                "LV.SignFlow";

            PdfPage? page = null;
            XGraphics? graphics = null;

            double y = 0;

            var titleFont =
                new XFont(
                    LvSignFontFamilies.Sans,
                    20,
                    XFontStyleEx.Bold);

            var headingFont =
                new XFont(
                    LvSignFontFamilies.Sans,
                    12,
                    XFontStyleEx.Bold);

            var normalFont =
                new XFont(
                    LvSignFontFamilies.Sans,
                    9,
                    XFontStyleEx.Regular);

            var smallFont =
                new XFont(
                    LvSignFontFamilies.Sans,
                    8,
                    XFontStyleEx.Regular);

            void NewPage()
            {
                graphics?.Dispose();

                page =
                    document.AddPage();

                y = Margin;

                graphics =
                    XGraphics.FromPdfPage(
                        page);
            }

            void EnsureSpace(
                double requiredHeight)
            {
                if (
                    page is null ||
                    graphics is null)
                {
                    NewPage();
                    return;
                }

                if (
                    y + requiredHeight >
                    page.Height.Point -
                    BottomMargin)
                {
                    NewPage();
                }
            }

            void DrawText(
                string? text,
                XFont font,
                double height = LineHeight)
            {
                EnsureSpace(height);

                graphics!.DrawString(
                    text ?? string.Empty,
                    font,
                    XBrushes.Black,
                    new XRect(
                        Margin,
                        y,
                        page!.Width.Point -
                        (Margin * 2),
                        height),
                    XStringFormats.TopLeft);

                y += height;
            }

            void DrawKeyValue(
                string label,
                string? value)
            {
                EnsureSpace(LineHeight);

                const double labelWidth = 130;

                graphics!.DrawString(
                    label,
                    headingFont,
                    XBrushes.Black,
                    new XRect(
                        Margin,
                        y,
                        labelWidth,
                        LineHeight),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    value ?? string.Empty,
                    normalFont,
                    XBrushes.Black,
                    new XRect(
                        Margin + labelWidth,
                        y,
                        page!.Width.Point -
                        (Margin * 2) -
                        labelWidth,
                        LineHeight),
                    XStringFormats.TopLeft);

                y += LineHeight;
            }

            void DrawDivider()
            {
                EnsureSpace(12);

                graphics!.DrawLine(
                    XPens.LightGray,
                    Margin,
                    y + 4,
                    page!.Width.Point - Margin,
                    y + 4);

                y += 12;
            }

            void DrawWrappedText(
                string text,
                XFont font,
                double indent = 0)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                var availableWidth =
                    595 - (Margin * 2) - indent;

                var words =
                    text.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

                var currentLine =
                    string.Empty;

                foreach (var word in words)
                {
                    var candidate =
                        string.IsNullOrWhiteSpace(
                            currentLine)
                            ? word
                            : $"{currentLine} {word}";

                    var size =
                        graphics!.MeasureString(
                            candidate,
                            font);

                    if (
                        size.Width >
                        availableWidth &&
                        !string.IsNullOrWhiteSpace(
                            currentLine))
                    {
                        EnsureSpace(LineHeight);

                        graphics.DrawString(
                            currentLine,
                            font,
                            XBrushes.Black,
                            new XRect(
                                Margin + indent,
                                y,
                                page!.Width.Point -
                                (Margin * 2) -
                                indent,
                                LineHeight),
                            XStringFormats.TopLeft);

                        y += LineHeight;

                        currentLine =
                            word;
                    }
                    else
                    {
                        currentLine =
                            candidate;
                    }
                }

                if (!string.IsNullOrWhiteSpace(
                        currentLine))
                {
                    EnsureSpace(LineHeight);

                    graphics!.DrawString(
                        currentLine,
                        font,
                        XBrushes.Black,
                        new XRect(
                            Margin + indent,
                            y,
                            page!.Width.Point -
                            (Margin * 2) -
                            indent,
                            LineHeight),
                        XStringFormats.TopLeft);

                    y += LineHeight;
                }
            }

            NewPage();

            /*
             * HEADER
             */
            DrawText(
                "LV.SignFlow",
                headingFont,
                20);

            DrawText(
                "Certificate of Completion",
                titleFont,
                30);

            DrawWrappedText(
                "This certificate records the completion details and final signed document fingerprints for this agreement.",
                normalFont);

            y += 8;

            DrawDivider();

            /*
             * ENVELOPE
             */
            DrawText(
                "Envelope",
                headingFont,
                22);

            DrawKeyValue(
                "Envelope ID",
                data.EnvelopeId.ToString());

            DrawKeyValue(
                "Subject",
                data.Subject);

            DrawKeyValue(
                "Organization",
                data.OrganizationName);

            DrawKeyValue(
                "Sender",
                data.SenderName);

            DrawKeyValue(
                "Sender email",
                data.SenderEmail);

            DrawKeyValue(
                "Created",
                FormatDate(data.CreatedAt));

            DrawKeyValue(
                "Sent",
                data.SentAt.HasValue
                    ? FormatDate(
                        data.SentAt.Value)
                    : "-");

            DrawKeyValue(
                "Completed",
                FormatDate(
                    data.CompletedAt));

            y += 8;

            DrawDivider();

            /*
             * RECIPIENTS
             */
            DrawText(
                "Recipients",
                headingFont,
                22);

            if (data.Recipients.Count == 0)
            {
                DrawText(
                    "No recipients.",
                    normalFont);
            }
            else
            {
                foreach (
                    var recipient in
                    data.Recipients
                        .OrderBy(x =>
                            x.RoutingOrder))
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    EnsureSpace(90);

                    DrawKeyValue(
                        "Name",
                        recipient.Name);

                    DrawKeyValue(
                        "Email",
                        recipient.Email);

                    DrawKeyValue(
                        "Role",
                        recipient.RecipientType);

                    DrawKeyValue(
                        "Routing order",
                        recipient.RoutingOrder
                            .ToString(
                                CultureInfo.InvariantCulture));

                    DrawKeyValue(
                        "Status",
                        recipient.Status);

                    DrawKeyValue(
                        "Completed",
                        recipient.CompletedAt.HasValue
                            ? FormatDate(
                                recipient.CompletedAt.Value)
                            : "-");

                    DrawDivider();
                }
            }

            /*
             * DOCUMENTS
             */
            DrawText(
                "Signed Documents",
                headingFont,
                22);

            foreach (
                var signedDocument in
                data.Documents)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                EnsureSpace(90);

                DrawKeyValue(
                    "File",
                    signedDocument.FileName);

                DrawKeyValue(
                    "Document ID",
                    signedDocument.DocumentId.ToString());

                DrawKeyValue(
                    "Finalized",
                    FormatDate(
                        signedDocument.FinalizedAt));

                DrawText(
                    "SHA-256",
                    headingFont);

                DrawWrappedText(
                    signedDocument.FinalHash,
                    smallFont,
                    12);

                DrawDivider();
            }

            /*
             * AUDIT TIMELINE
             */
            DrawText(
                "Audit Timeline",
                headingFont,
                22);

            foreach (
                var auditEvent in
                data.Events
                    .OrderBy(x =>
                        x.OccurredAt))
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                EnsureSpace(48);

                DrawText(
                    $"{FormatDate(auditEvent.OccurredAt)}  |  {auditEvent.EventType}",
                    headingFont);

                DrawWrappedText(
                    auditEvent.Description,
                    normalFont,
                    12);

                y += 4;
            }

            /*
             * FOOTER
             */
            DrawDivider();

            DrawWrappedText(
                "Document hashes shown in this certificate are SHA-256 fingerprints of the final signed PDF artifacts generated by LV.SignFlow.",
                smallFont);

            graphics?.Dispose();

            using var output =
                new MemoryStream();

            document.Save(
                output,
                closeStream: false);

            return Task.FromResult(
                output.ToArray());
        }

        private static string FormatDate(
            DateTimeOffset value)
        {
            return value
                .ToUniversalTime()
                .ToString(
                    "yyyy-MM-dd HH:mm:ss 'UTC'",
                    CultureInfo.InvariantCulture);
        }
    }
}
