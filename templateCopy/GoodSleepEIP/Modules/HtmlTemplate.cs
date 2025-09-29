using HtmlAgilityPack;
using Newtonsoft.Json;
using NUnit.Framework;
using GoodSleepEIP.Models;

namespace GoodSleepEIP
{
    public class PurchaseEmailBody
    {
        public required string email_address { get; set; }
        public string? supplier_full_name { get; set; }
        public string? purchase_no { get; set; }
        public string? users_username { get; set; }
        public string? admin_unit_name { get; set; }
        public string? employee_name { get; set; }
    }

    public class HtmlBody
    {
        // "//span[@name='RespPersonName']", RespPersonName
        public void UpdateNodesHtml(HtmlDocument doc, string xPath, string value)
        {
            try
            {
                var nodes = doc.DocumentNode.SelectNodes(xPath) ?? throw new Exception("No nodes found for the given xPath.");
                foreach (var node in nodes)
                {
                    node.InnerHtml = value;
                }
            }
            catch { }
        }

        public void UpdateNodesAttribute(HtmlDocument doc, string xPath, string attrName, string value)
        {
            try
            {
                var nodes = doc.DocumentNode.SelectNodes(xPath) ?? throw new Exception("No nodes found for the given xPath.");
                foreach (var node in nodes)
                {
                    node.SetAttributeValue(attrName, value);
                }
            }
            catch { }
        }

        // "//table[@id='table1']", new string[] { "產品1", "10", "100", "1000" });
        // "//table[@id='table1']|//table[@id='table2']", new string[] { "產品1", "10", "100", "1000" });
        public void AddTableRow(HtmlDocument doc, string tableXPath, string[] cellValues)
        {
            try
            {
                var tableNode = doc.DocumentNode.SelectSingleNode(tableXPath);
                if (tableNode != null)
                {
                    var newRow = doc.CreateElement("tr");
                    foreach (var cellValue in cellValues)
                    {
                        var newCell = doc.CreateElement("td");
                        newCell.InnerHtml = cellValue;
                        newRow.AppendChild(newCell);
                    }
                    tableNode.AppendChild(newRow);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}
