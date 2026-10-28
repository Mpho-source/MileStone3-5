using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NextGen__7
{
    internal static class EduConnectPdfExporter
    {
        private sealed class PdfPage
        {
            internal string Content;
            internal bool HasImage;
        }

        private static string Esc(string value)
        {
            var escaped = new StringBuilder();
            foreach (byte valueByte in Encoding.GetEncoding(1252).GetBytes(value ?? ""))
            {
                if (valueByte == 40 || valueByte == 41 || valueByte == 92)
                    escaped.Append('\\');
                if (valueByte < 32 || valueByte > 126)
                    escaped.Append('\\').Append(Convert.ToString(valueByte, 8).PadLeft(3, '0'));
                else
                    escaped.Append((char)valueByte);
            }
            return escaped.ToString();
        }

        private static string Fit(string value, double width, double fontSize)
        {
            value = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            int count = Math.Max(4, (int)(width / (fontSize * 0.53)));
            if (value.Length <= count) return value;
            return value.Substring(0, Math.Max(1, count - 3)) + "...";
        }

        private static void Label(StringBuilder s, string text, double x, double y,
            double size, double r, double g, double b)
        {
            s.AppendFormat(CultureInfo.InvariantCulture,
                "BT /F1 {0:0.##} Tf {1:0.###} {2:0.###} {3:0.###} rg {4:0.##} {5:0.##} Td ({6}) Tj ET\n",
                size, r, g, b, x, y, Esc(text));
        }

        private static void Rect(StringBuilder s, double x, double y, double width,
            double height, double r, double g, double b)
        {
            s.AppendFormat(CultureInfo.InvariantCulture,
                "{0:0.###} {1:0.###} {2:0.###} rg {3:0.##} {4:0.##} {5:0.##} {6:0.##} re f\n",
                r, g, b, x, y, width, height);
        }

        private static byte[] Ascii(string value)
        {
            return Encoding.ASCII.GetBytes(value);
        }

        private static byte[] StreamObject(byte[] bytes, string dictionary)
        {
            using (var output = new MemoryStream())
            {
                byte[] prefix = Ascii("<< " + dictionary + " /Length " +
                    bytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                output.Write(prefix, 0, prefix.Length);
                output.Write(bytes, 0, bytes.Length);
                byte[] suffix = Ascii("\nendstream");
                output.Write(suffix, 0, suffix.Length);
                return output.ToArray();
            }
        }

        private static string ReadValue(DataRow row, int column)
        {
            object value = row[column];
            if (value == DBNull.Value || value == null) return "-";
            if (value is DateTime)
            {
                DateTime date = (DateTime)value;
                return date.ToString("dd MMM yyyy", CultureInfo.CurrentCulture);
            }
            if (value is double || value is decimal || value is float)
                return Convert.ToDecimal(value).ToString("0.##", CultureInfo.CurrentCulture);
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? "-";
        }

        private static double[] GetColumnWidths(DataTable table, double totalWidth)
        {
            int count = table.Columns.Count;
            var widths = new double[count];
            if (count == 0) return widths;

            double minimum = count > 10 ? 42 : count > 8 ? 52 : 65;
            minimum = Math.Min(minimum, totalWidth / count * 0.78);
            var weights = new double[count];
            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                string heading = table.Columns[i].ColumnName;
                int desired = heading.Length;
                int sample = Math.Min(75, table.Rows.Count);
                for (int j = 0; j < sample; j++)
                    desired = Math.Max(desired, ReadValue(table.Rows[j], i).Length);
                weights[i] = Math.Max(10, Math.Min(34, desired));
                sum += weights[i];
            }
            double free = totalWidth - minimum * count;
            for (int i = 0; i < count; i++)
                widths[i] = minimum + free * weights[i] / sum;
            return widths;
        }

        private static void TextColors(string heading, string value,
            out double r, out double g, out double b)
        {
            r = 0.16; g = 0.23; b = 0.33;
            if (heading.IndexOf("status", StringComparison.OrdinalIgnoreCase) < 0 &&
                heading.IndexOf("attendance", StringComparison.OrdinalIgnoreCase) < 0)
                return;
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "scheduled":
                case "attended":
                    r = 0.07; g = 0.40; b = 0.27; break;
                case "submitted":
                    r = 0.57; g = 0.34; b = 0.08; break;
                case "verified":
                    r = 0.16; g = 0.27; b = 0.57; break;
                case "completed":
                    r = 0.11; g = 0.33; b = 0.57; break;
                case "cancelled":
                case "absent":
                    r = 0.64; g = 0.16; b = 0.20; break;
            }
        }

        private static List<PdfPage> MakePages(string title, string description,
            DataTable table, bool hasImage, string dataNote)
        {
            var pages = new List<PdfPage>();
            int row = 0;
            int pageNumber = 1;
            do
            {
                bool chartPage = hasImage && pageNumber == 1;
                var canvas = new StringBuilder();
                Rect(canvas, 0, 527, 842, 68, 0.12, 0.22, 0.39);
                Rect(canvas, 0, 524, 842, 3, 0.08, 0.56, 0.53);
                Label(canvas, "EDUCONNECT  |  UNIVERSITY TUTORING PORTAL", 36, 570,
                    10, 0.78, 0.88, 1.0);
                Label(canvas, Fit(title, 770, 18), 36, 542,
                    18, 1, 1, 1);
                Label(canvas, Fit(description, 762, 9), 36, 504,
                    9, 0.29, 0.39, 0.53);
                if (!string.IsNullOrWhiteSpace(dataNote))
                    Label(canvas, Fit(dataNote, 755, 8), 36, 488,
                        8, 0.55, 0.31, 0.11);

                double top = chartPage ? 260 : (string.IsNullOrWhiteSpace(dataNote) ? 477 : 465);
                if (chartPage)
                {
                    Rect(canvas, 35, 283, 772, 195, 0.96, 0.98, 1);
                    canvas.Append("q 575 0 0 190 133 286 cm /Im1 Do Q\n");
                }

                double left = 35;
                double tableWidth = 772;
                double[] columnWidths = GetColumnWidths(table, tableWidth);
                double[] columnLeft = new double[table.Columns.Count];
                double offset = left;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    columnLeft[i] = offset;
                    offset += columnWidths[i];
                }
                Rect(canvas, left, top - 32, tableWidth, 32, 0.12, 0.27, 0.46);
                for (int i = 0; i < table.Columns.Count; i++)
                    Label(canvas, Fit(table.Columns[i].ColumnName, columnWidths[i] - 10, 7.5),
                        columnLeft[i] + 5, top - 20, 7.5, 1, 1, 1);

                double y = top - 32;
                while (row < table.Rows.Count && y - 31 >= 43)
                {
                    y -= 31;
                    if (row % 2 == 0)
                        Rect(canvas, left, y, tableWidth, 31, 0.94, 0.97, 1);
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        string value = ReadValue(table.Rows[row], i);
                        double r, g, b;
                        TextColors(table.Columns[i].ColumnName, value, out r, out g, out b);
                        Label(canvas, Fit(value, columnWidths[i] - 10, 8),
                            columnLeft[i] + 5, y + 11, 8, r, g, b);
                    }
                    row++;
                }
                Rect(canvas, 35, 34, 772, 1, 0.83, 0.88, 0.94);
                Label(canvas, "Generated " + DateTime.Now.ToString("dd MMM yyyy HH:mm",
                        CultureInfo.CurrentCulture) + "  |  " + table.Rows.Count +
                        " record(s)", 35, 22, 8, 0.37, 0.44, 0.54);
                Label(canvas, "Page " + pageNumber, 755, 22, 8, 0.37, 0.44, 0.54);
                pages.Add(new PdfPage { Content = canvas.ToString(), HasImage = chartPage });
                pageNumber++;
            } while (row < table.Rows.Count);
            return pages;
        }

        internal static void Save(string file, string title, string description,
            DataTable table, byte[] chartJpeg, int chartWidth, int chartHeight,
            string dataNote)
        {
            if (table == null || table.Rows.Count == 0)
                throw new InvalidOperationException("Generate a report with results before saving a PDF.");
            bool hasImage = chartJpeg != null && chartJpeg.Length > 0 &&
                chartWidth > 0 && chartHeight > 0;
            List<PdfPage> pages = MakePages(title, description, table, hasImage, dataNote);
            var objects = new List<byte[]> { null };
            Func<int> reserve = () => { objects.Add(null); return objects.Count - 1; };
            int catalogId = reserve();
            int pagesId = reserve();
            int fontId = reserve();
            int imageId = hasImage ? reserve() : 0;
            objects[fontId] = Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            if (hasImage)
                objects[imageId] = StreamObject(chartJpeg,
                    "/Type /XObject /Subtype /Image /Width " + chartWidth +
                    " /Height " + chartHeight +
                    " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode");
            var pageIds = new List<int>();
            foreach (PdfPage page in pages)
            {
                int contentId = reserve();
                objects[contentId] = StreamObject(Ascii(page.Content), "");
                int pageId = reserve();
                string resources = "<< /Font << /F1 " + fontId + " 0 R >>" +
                    (page.HasImage ? " /XObject << /Im1 " + imageId + " 0 R >>" : "") + " >>";
                objects[pageId] = Ascii("<< /Type /Page /Parent " + pagesId +
                    " 0 R /MediaBox [0 0 842 595] /Resources " + resources +
                    " /Contents " + contentId + " 0 R >>");
                pageIds.Add(pageId);
            }
            objects[pagesId] = Ascii("<< /Type /Pages /Count " + pageIds.Count +
                " /Kids [" + string.Join(" ", pageIds.Select(id => id + " 0 R")) + "] >>");
            objects[catalogId] = Ascii("<< /Type /Catalog /Pages " + pagesId + " 0 R >>");
            byte[] outputBytes;
            using (var buffer = new MemoryStream())
            {
                Action<string> write = s =>
                {
                    byte[] bytes = Ascii(s);
                    buffer.Write(bytes, 0, bytes.Length);
                };
                write("%PDF-1.4\n");
                var offsets = new long[objects.Count];
                for (int i = 1; i < objects.Count; i++)
                {
                    offsets[i] = buffer.Position;
                    write(i.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    buffer.Write(objects[i], 0, objects[i].Length);
                    write("\nendobj\n");
                }
                long xref = buffer.Position;
                write("xref\n0 " + objects.Count + "\n0000000000 65535 f \n");
                for (int i = 1; i < objects.Count; i++)
                    write(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) +
                        " 00000 n \n");
                write("trailer\n<< /Size " + objects.Count +
                    " /Root " + catalogId + " 0 R >>\nstartxref\n" +
                    xref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
                outputBytes = buffer.ToArray();
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(file));
            string temporary = Path.Combine(directory,
                ".educonnect-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllBytes(temporary, outputBytes);
                if (File.Exists(file))
                    File.Replace(temporary, file, null);
                else
                    File.Move(temporary, file);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
