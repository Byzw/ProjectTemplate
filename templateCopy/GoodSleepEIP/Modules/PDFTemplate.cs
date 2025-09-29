using iText.Html2pdf.Resolver.Font;
using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using System.Runtime;
using ZXing.Common;
using ZXing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace GoodSleepEIP
{
    public class MakePDF
    {
        public byte[] GeneratePDF(string html)
        {
            PdfDocument? pdfDoc = null;
            MemoryStream? memStream = null;
            PdfWriter? pdfWriter = null;

            byte[] buffer;
            try
            {
                ConverterProperties props = new ConverterProperties();
                //var provider = new DefaultFontProvider(true, true, true); // 第三個引數為True，以支援系統字型，否則不支援中文
                var provider = new DefaultFontProvider(false, false, false);
                //provider.AddFont(System.IO.Path.GetFullPath("Templates\\Report\\NotoSansCJKsc-Regular.otf"));
                provider.AddFont(System.IO.Path.GetFullPath("Templates/Report/kaiu.ttf"));
                props.SetCharset("utf-8");
                props.SetFontProvider(provider);

                using (memStream = new MemoryStream())
                {
                    using (pdfWriter = new PdfWriter(memStream))
                    {
                        pdfWriter.SetCloseStream(true);
                        using (pdfDoc = new PdfDocument(pdfWriter))
                        {
                            pdfDoc.SetDefaultPageSize(PageSize.A4); // 設定預設頁面大小，css @page規則可覆蓋這個
                            pdfDoc.SetCloseWriter(true);
                            pdfDoc.SetCloseReader(true);
                            pdfDoc.SetFlushUnusedObjects(true);
                            HtmlConverter.ConvertToPdf(html, pdfDoc, props);  
                            pdfDoc.Close();
                        }
                    }
                    buffer = memStream.ToArray();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (pdfDoc != null && !pdfDoc.IsClosed()) { pdfDoc.Close(); }
                if (pdfWriter != null) { pdfWriter.Dispose(); }
                if (memStream != null) { memStream.Dispose(); }
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
            return buffer;
        }

        public byte[] MergePDF(IEnumerable<byte[]> pdfs)
        {
            using (var writerMemoryStream = new MemoryStream())
            {
                using (var writer = new PdfWriter(writerMemoryStream)) using (var mergedDocument = new PdfDocument(writer))
                {
                    var merger = new PdfMerger(mergedDocument);
                    foreach (var pdfBytes in pdfs)
                    {
                        using (var copyFromMemoryStream = new MemoryStream(pdfBytes)) using (var reader = new PdfReader(copyFromMemoryStream)) using (var copyFromDocument = new PdfDocument(reader))
                            merger.Merge(copyFromDocument, 1, copyFromDocument.GetNumberOfPages());
                    }
                    merger = null;
                }
                return writerMemoryStream.ToArray();
            }
        }
    }
    public class BarCode
    {
        public string html_code128(string data)
        {
            try
            {
                var BarCodeWriter = new ZXing.ImageSharp.BarcodeWriter<SixLabors.ImageSharp.PixelFormats.La32>
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Height = 40,
                        Width = 160,
                        Margin = 0
                    }
                };
                var pixelData = BarCodeWriter.Write(data);
                return @String.Format("<img src=\"{0}\"/>", pixelData.ToBase64String(PngFormat.Instance));
            }
            catch { return ""; }
        }
    }
}
