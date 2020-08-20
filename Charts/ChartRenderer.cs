using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Bot.WebHookServer;


namespace Bot.Charts
{
    class ChartRenderer
    {
        public string TABLE_CSS = "<style>\n.the_table {\nborder:1px inset #00FFFF;\nborder-collapse:separate;\nborder-spacing:0px;\npadding:6px;\n}\n\n.the_table th {\nborder:1px inset #00FFFF;\npadding:6px;\nbackground:#F0F0F0;\n}\n.the_table td {\nborder:1px inset #00FFFF;\npadding:6px;\nbackground:#000000;\ncolor:#FFFFFF;\n}\n</style>";


        [WebhookAttribs("/charts/%", HTTPMethod = "GET")]
        public WebhookRegistry.HTTPResponseData renderChart(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData hrd = new WebhookRegistry.HTTPResponseData();
            if (ChartMemory.HasChartID(arguments[0]))
            {
                hrd.Status = 200;
                hrd.ReturnContentType = "text/html";
                Chart C = ChartMemory.GetChartByID(arguments[0]);
                string page = "<html><head><title>Chart - " + C.ChartName + "</title></head><body bgcolor='black'>";
                // Don't prepend the Table CSS since we need to set colors custom
                page += "<center><a style='color:white'><h2>" + C.ChartDescription + "</h2></a>";
                page += "<table style=\"border:1px inset #00FFFF;border-collapse:separate;border-spacing:0px;padding:6px;\">";
                ChartColumn col = C.ColumnData;
                page += "<thead><th style=\"background:#F0F0F0;border:1px inset #00FFFF;padding:6px\">"+C.ChartName+"</th>";
                foreach (ChartColumnField field in col.Fields)
                {
                    page += "<th style=\"background:" + field.ColorCode + "\";border:1px inset #00FFFF;padding:6px\">" + field.Label + "</th>";
                }
                page += "</thead><tbody>";
                int Pos = 0;
                foreach(ChartRow row in C.RowData)
                {
                    Pos = 0;
                    page += "<tr><td style=\"border:1px inset #00FFFF;padding:6px;background:#F0F0F0;color:black\">" + row.Label + "</td>";
                    foreach(ChartColumnField field in col.Fields)
                    {

                        // Begin processing row. Keep index of what position we are at
                        int MaskForPos = col.Pos2Bit(Pos);
                        string ColorCode = "#000000";
                        if ((row.Mask & MaskForPos) == MaskForPos)
                        {
                            // set the color code to the col code
                            ColorCode = col.Fields[Pos].ColorCode;
                        }
                        page += "<td style=\"border:1px inset #00FFFF;padding:6px;background:" + ColorCode + ";color:white\"> </td>";



                        Pos++;
                    }
                    page += "</tr>";
                }

                page += "</tbody></table></body>";
                page += "<script type='text/javascript'>";
                page += "setInterval(function(){window.location.reload(true);}, 30000);";
                page += "</script>";
                hrd.ReplyString = page;
            }else
            {
                hrd.ReplyString = "Not found";
                hrd.Status = 404;
                hrd.ReturnContentType = "text/plain";
            }


            return hrd;
        }


        [WebhookAttribs("/charts", HTTPMethod = "GET")]
        public WebhookRegistry.HTTPResponseData listCharts(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData hrd = new WebhookRegistry.HTTPResponseData();
            string webpage = "<html><head><title>BotCore5 chart list</title></head><body bgcolor='#000000'>";
            webpage += TABLE_CSS;
            webpage += "\n\n<table class=\"the_table\"><thead><tr><th>Chart Name</th><th>Chart Path</th><th>Description of chart</th></tr></thead><tbody>";

            foreach(Chart c in ChartMemory.Instance.Charts)
            {
                webpage += "\n<tr><td>" + c.ChartName;
                webpage += "</td><td><a href='/charts/" + c.ChartID+"'>"+c.ChartName+"</a></td><td>"+c.ChartDescription+"</td></tr>";
            }
            webpage += "</tbody>";
            webpage += "</table></body></html>";

            hrd.ReplyString = webpage;
            hrd.ReturnContentType = "text/html";
            hrd.Status = 200;


            return hrd;
        }
    }
}
