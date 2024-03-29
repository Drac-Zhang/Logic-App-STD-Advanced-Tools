﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LogicAppAdvancedTool
{
    public class ConsoleTable
    {
        private int ColumnCount;
        private List<string[]> Rows;
        private List<int> ColumnWidth;

        public ConsoleTable(params string[] headers)
        {
            ColumnCount = headers.Length;

            Rows = new List<string[]>();
            Rows.Add(headers);

            ColumnWidth = new List<int>();
            foreach (string header in headers)
            {
                ColumnWidth.Add(header.Length + 2);
            }
        }

        public void AddRow(params string[] contents)
        {
            if (contents.Length != ColumnCount)
            {
                throw new Exception("Column count mismatch");
            }

            Rows.Add(contents);

            for (int i = 0; i < contents.Length; i++)
            {
                if (String.IsNullOrEmpty(contents[i]))
                {
                    continue;
                }

                if (ColumnWidth[i] < contents[i].Length + 2)
                {
                    ColumnWidth[i] = contents[i].Length + 2;
                }
            }
        }

        public void Print()
        {
            int rowLength = 0;

            foreach (int l in ColumnWidth)
            {
                rowLength += l;
            }

            rowLength += ColumnWidth.Count + 1;

            Console.WriteLine(new string('-', rowLength));

            foreach (string[] row in Rows)
            {
                StringBuilder rowContent = new StringBuilder();

                for (int i = 0; i < row.Length; i++)
                {
                    rowContent.Append("|");
                    rowContent.Append(string.Format($" {row[i]}".PadRight(ColumnWidth[i])));
                }

                rowContent.Append("|");

                Console.WriteLine(rowContent.ToString());
                Console.WriteLine(new string('-', rowLength));
            }
        }
    }
}
