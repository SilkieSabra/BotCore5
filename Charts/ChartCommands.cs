using System;
using System.Collections.Generic;
using System.Text;
using Bot.CommandSystem;
using Bot;
using OpenMetaverse;
using System.Linq;

namespace Bot.Charts
{
    class ChartCommands : BaseCommands
    {
        [CommandGroup("mkchart", 5, "mkchart [name] - Makes a new empty chart", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void makeNewChart(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                MHE(source, client, "A chart by that name already exists");
                return;
            }
            ChartMemory.Instance.Charts.Add(new Chart(additionalArgs[0]));
            ChartMemory.SaveToFile();
            MHE(source, client, "New chart created successfully");
        }


        [CommandGroup("renamechart", 5, "renamechart [name] [newName] - Renames a chart", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void renameChart(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);
                int index = ChartMemory.Instance.Charts.IndexOf(v);
                v.ChartName = additionalArgs[1];

                ChartMemory.Instance.Charts[index] = v;
                ChartMemory.SaveToFile();
                MHE(source, client, "Chart renamed");
                return;
            }
        }


        [CommandGroup("delchart", 5, "delchart [name] - Deletes named chart", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void deleteChart(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);

                ChartMemory.Instance.Charts.Remove(v);
                ChartMemory.SaveToFile();
                MHE(source, client, "Chart deleted");
                return;
            }
        }

        [CommandGroup("addchartrow", 5, "addchartrow [name] [bitmask] [index] [label]", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void addChartRow(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);
                int index = ChartMemory.Instance.Charts.IndexOf(v);
                ChartRow row = new ChartRow();
                row.Label = "";
                row.Mask = Convert.ToInt32(additionalArgs[1]);
                int masterIndex;
                bool hasIndex = int.TryParse(additionalArgs[2], out masterIndex);
                void getRowLabel()
                {
                    row.Label = "";
                    if (hasIndex)
                    {
                        // the label is at 3 and above
                        for(int i = 3; i < additionalArgs.Length; i++)
                        {
                            row.Label += additionalArgs[i] + " ";
                        }

                        if (row.Label.EndsWith(" ")) row.Label=row.Label.TrimEnd(' ');
                    }else
                    {

                        // the label is at 2 and above
                        for (int i = 2; i < additionalArgs.Length; i++)
                        {
                            row.Label += additionalArgs[i] + " ";
                        }

                        if (row.Label.EndsWith(" ")) row.Label=row.Label.TrimEnd(' ');
                    }

                }

                getRowLabel();
                if (v.RowData.Where(x => x.Label == row.Label).Count() > 0)
                {
                    MHE(source, client, "A chart row with that label already exists.");
                    return;
                }else
                {
                    // index optional
                    if(hasIndex)
                    {
                        v.RowData.Insert(masterIndex, row);
                        ChartMemory.Instance.Charts[index] = v;
                        ChartMemory.SaveToFile();
                    }else
                    {
                        // row label might start at 2 instead, but we only add anyway
                        getRowLabel();

                        v.RowData.Add(row);
                        ChartMemory.Instance.Charts[index] = v;
                        ChartMemory.SaveToFile();
                    }
                }


                MHE(source, client, "Chart row added");
                return;
            }
        }




        [CommandGroup("reloadcharts", 5, "reloadcharts - Reloads the charts.json file from disk", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void reloadCharts(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            ChartMemory.Reload();
            MHE(source, client, "Reload completed");
        }



        [CommandGroup("setchartdesc", 5, "setchartdesc [chartName] [args...] - Sets the chart description on the chart page header", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void setchartdesc(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            string finalStr = "";
            int pos = 0;
            foreach(string s in additionalArgs)
            {
                if (pos == 0) continue;
                else
                    finalStr += s+" ";
                pos++;
            }

            if (finalStr.EndsWith(" ")) finalStr = finalStr.TrimEnd(' ');
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);
                pos = ChartMemory.Instance.Charts.IndexOf(v);
                v.ChartDescription = finalStr;
                ChartMemory.Instance.Charts[pos] = v;
                ChartMemory.SaveToFile();
                MHE(source, client, "Chart description has been set");
            }
            else
            {
                MHE(source, client, "No such chart");
            }
        }


        [CommandGroup("delchartrow", 5, "delchartrow [name] [label]", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void removeChartRow(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);
                int index = ChartMemory.Instance.Charts.IndexOf(v);
                ChartRow row = new ChartRow();
                row.Label = "";
                void getRowLabel()
                {
                    row.Label = "";
                    for (int i = 1; i < additionalArgs.Length; i++)
                    {
                        row.Label += additionalArgs[i] + " ";
                    }

                    if (row.Label.EndsWith(" ")) row.Label=row.Label.TrimEnd(' ');


                }

                getRowLabel();
                if (v.RowData.Where(x => x.Label == row.Label).Count() > 0)
                {
                    row=v.RowData.Where(x => x.Label == row.Label).First();
                    v.RowData.Remove(row);
                    ChartMemory.Instance.Charts[index] = v;
                    ChartMemory.SaveToFile();
                    
                }
                else
                {
                    MHE(source, client, "No such row in that chart");
                    return;
                }


                MHE(source, client, "Chart row deleted");
                return;
            }
        }


        [CommandGroup("addchartcol", 5, "addchartcol [name] [label] [color code] [colIndex]", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void setChartCols(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);
                int index = ChartMemory.Instance.Charts.IndexOf(v);
                ChartColumn col = v.ColumnData;
                if (col == null) col = new ChartColumn();
                if (col.Fields == null) col.Fields = new List<ChartColumnField>();


                if (col.Fields.Where(x => x.Label == additionalArgs[1]).Count() > 0)
                {
                    // already has this column
                    MHE(source, client, "That column already exists");
                    return;
                }else
                {
                    ChartColumnField field = new ChartColumnField();
                    field.ColorCode = additionalArgs[2];
                    field.Label = additionalArgs[1];


                    if (additionalArgs.Length == 4)
                    {
                        // insert at index
                        col.Fields.Insert(Convert.ToInt32(additionalArgs[3]), field);
                    }else 
                        col.Fields.Add(field);

                }

                v.ColumnData = col;
                ChartMemory.Instance.Charts[index] = v;
                ChartMemory.SaveToFile();

                MHE(source, client, "Column data has been updated");
            }
            else
            {
                MHE(source, client, "No such chart");
            }
        }

        [CommandGroup("delchartcol", 5, "delchartcol [name] [label]", Bot.Destinations.DEST_AGENT | Bot.Destinations.DEST_LOCAL | Bot.Destinations.DEST_GROUP)]
        public void remChartCols(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            if (ChartMemory.HasChart(additionalArgs[0]))
            {
                Chart v = ChartMemory.GetNamedChart(additionalArgs[0]);
                int index = ChartMemory.Instance.Charts.IndexOf(v);
                if (v.ColumnData.Fields.Where(x => x.Label == additionalArgs[1]).Count() > 0)
                {
                    ChartColumnField theChartField = v.ColumnData.Fields.Where(x => x.Label == additionalArgs[1]).First();
                    v.ColumnData.Fields.Remove(theChartField);

                    ChartMemory.Instance.Charts[index] = v;
                    ChartMemory.SaveToFile();

                    MHE(source, client, "Column removed");
                }
                else
                {
                    MHE(source, client, "Column does not exist, nothing to remove");
                }
            }
            else
            {
                MHE(source, client, "No such chart");
            }
        }
    }
}
