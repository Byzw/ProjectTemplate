using System;
using System.Collections.Generic;
using System.IO;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace GoodSleepEIP
{
    public class DocxHelper
    {
        private readonly Dictionary<string, int> _indentationLevels = new Dictionary<string, int>
        {
            { "一、", 1 }, { "二、", 1 }, { "三、", 1 }, { "四、", 1 }, { "五、", 1 }, { "六、", 1 }, { "七、", 1 }, { "八、", 1 }, { "九、", 1 }, { "十、", 1 }, { "十一、", 1 }, { "十二、", 1 }, { "十三、", 1 }, { "十四、", 1 }, { "十五、", 1 }, { "十六、", 1 }, { "十七、", 1 }, { "十八、", 1 }, { "十九、", 1 }, { "二十、", 1 },
            { "(一)", 2 }, { "(二)", 2 }, { "(三)", 2 }, { "(四)", 2 }, { "(五)", 2 }, { "(六)", 2 }, { "(七)", 2 }, { "(八)", 2 }, { "(九)", 2 }, { "(十)", 2 }, { "(十一)", 2 }, { "(十二)", 2 }, { "(十三)", 2 }, { "(十四)", 2 }, { "(十五)", 2 }, { "(十六)", 2 }, { "(十七)", 2 }, { "(十八)", 2 }, { "(十九)", 2 }, { "(二十)", 2 }
        };

        public DocX ReplacePlaceholdersInDocx(string filePath, Dictionary<string, string> replacements)
        {
            // 加載文件並將其讀入內存流
            byte[] fileBytes = File.ReadAllBytes(filePath);
            using (var memoryStream = new MemoryStream(fileBytes))
            {
                DocX document = DocX.Load(memoryStream);

                // 使用 StringReplaceTextOptions 遍歷段落
                foreach (var paragraph in document.Paragraphs)
                {
                    foreach (var replacement in replacements)
                    {
                        var options = new StringReplaceTextOptions
                        {
                            SearchValue = replacement.Key,
                            NewValue = replacement.Value,
                            RemoveEmptyParagraph = true
                        };
                        paragraph.ReplaceText(options);
                    }
                }

                // 遍歷所有表格並替換文本
                foreach (var table in document.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        foreach (var cell in row.Cells)
                        {
                            foreach (var paragraph in cell.Paragraphs)
                            {
                                foreach (var replacement in replacements)
                                {
                                    var options = new StringReplaceTextOptions
                                    {
                                        SearchValue = replacement.Key,
                                        NewValue = replacement.Value,
                                        RemoveEmptyParagraph = true
                                    };
                                    paragraph.ReplaceText(options);
                                }
                            }
                        }
                    }
                }

                return document;
            }
        }

        public void SaveDocxDocument(DocX document, string outputPath)
        {
            document.SaveAs(outputPath);
        }

        public void SetParagraphIndentation(Paragraph paragraph, string text)
        {
            float cmToPoint = 28.35f;

            // 定義不同級別的縮排值（以厘米為單位）
            float level1Indent = 1.5f * cmToPoint; // 1.5厘米轉換為點
            float level2Indent = 2.5f * cmToPoint; // 2.5厘米轉換為點

            foreach (var level in _indentationLevels)
            {
                if (text.StartsWith(level.Key))
                {
                    switch (level.Value)
                    {
                        case 1:
                            paragraph.IndentationHanging = level1Indent; // 首行凸排
                            paragraph.IndentationFirstLine = -32.0f; // 首行縮排
                            break;
                        case 2:
                            paragraph.IndentationHanging = level2Indent; // 首行凸排
                            paragraph.IndentationFirstLine = -33.0f; // 首行縮排
                            break;
                        default:
                            //paragraph.IndentationFirstLine = 0.0f; // 無首行縮排
                            //paragraph.IndentationHanging = 0.0f; // 無首行凸排
                            break;
                    }
                    return;
                }
            }

            // 預設無縮排
            //paragraph.IndentationFirstLine = 0.0f; // 無首行縮排
            //paragraph.IndentationHanging = 0.0f; // 無首行凸排
        }
    }
}
