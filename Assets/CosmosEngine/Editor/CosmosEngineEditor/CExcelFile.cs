﻿using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

class CExcelFile
{
    //private Workbook Workbook_;
    //private Worksheet Worksheet_;
    public Dictionary<string, int> ColName2Index;
    //private DataTable DataTable_;
    private string Path;
    private IWorkbook Workbook;
    private ISheet Worksheet;
    public bool IsLoadSuccess = true;
    public CExcelFile(string excelPath)
    {
        Path = excelPath;

        using (var file = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
        {
            try
            {
                Workbook = WorkbookFactory.Create(file);
            }
            catch (Exception e)
            {
                CDebug.LogError("无法打开Excel: {0}, 可能原因：正在打开？或是Office2007格式（尝试另存为）？ {1}", excelPath, e.Message);
                IsLoadSuccess = false;
            }
            
        }
        if (IsLoadSuccess)
        {
            CDebug.Assert(Workbook);

            //var dt = new DataTable();

            Worksheet = Workbook.GetSheetAt(0);
            ColName2Index = new Dictionary<string, int>();
            var headerRow = Worksheet.GetRow(0);
            int columnCount = headerRow.LastCellNum;

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var cell = headerRow.GetCell(columnIndex);
                if (cell == null)
                {
                    //CDebug.LogError("Null Cel on Column: {0}, File: {1}", columnIndex, excelPath);
                    // 可能是空字符串的格子，忽略！
                    continue;
                }
                var headerName = cell.ToString().Split('|', '[', ':', ']')[0]; // 去掉参数定义
                ColName2Index[headerName] = columnIndex;
            }
        }
        
    }

    /// <summary>
    /// 清除行内容
    /// </summary>
    /// <param name="row"></param>
    public void ClearRow(int row)
    {
        var theRow = Worksheet.GetRow(row);
        Worksheet.RemoveRow(theRow);
    }

    public float GetFloat(string columnName, int row)
    {
        return GetString(columnName, row).ToFloat();
    }
    public int GetInt(string columnName, int row)
    {
        return GetString(columnName, row).ToInt32();
    }
    public string GetString(string columnName, int row)
    {
        var theRow = Worksheet.GetRow(row);
        if (theRow == null)
            theRow = Worksheet.CreateRow(row);

        var colIndex = ColName2Index[columnName];
        var cell = theRow.GetCell(colIndex);
        if (cell == null)
            cell = theRow.CreateCell(colIndex);
        return cell.ToString();
    }

    public int GetRowsCount()
    {
        return Worksheet.LastRowNum + 1;
    }

    private ICellStyle GreyCellStyleCache;
    public void SetRowGrey(int row)
    {
        var theRow = Worksheet.GetRow(row);
        foreach (var cell in theRow.Cells)
        {
            if (GreyCellStyleCache == null)
            {
                var newStyle = Workbook.CreateCellStyle();
                newStyle.CloneStyleFrom(cell.CellStyle);
                //newStyle.FillBackgroundColor = colorIndex;
                newStyle.FillPattern = FillPattern.Diamonds;
                GreyCellStyleCache = newStyle;
            }

            cell.CellStyle = GreyCellStyleCache;
        }
    }

    public void SetRow(string columnName, int row, string value)
    {
        if (!ColName2Index.ContainsKey(columnName))
        {
            CDebug.LogError("No Column: {0} of File: {1}", columnName, Path);
            return;
        }
        var theRow = Worksheet.GetRow(row);
        if (theRow == null)
            theRow = Worksheet.CreateRow(row);
        var cell = theRow.GetCell(ColName2Index[columnName]);
        if (cell == null)
            cell = theRow.CreateCell(ColName2Index[columnName]);

        if (value.Length > (1 << 14))  // if too long
        {
            value = value.Substring(0, 1 << 14);
        }
        cell.SetCellValue(value);
        
    }

    public void Save()
    {
        /*for (var loopRow = Worksheet.FirstRowNum; loopRow <= Worksheet.LastRowNum; loopRow++)
        {
            var row = Worksheet.GetRow(loopRow);
            bool emptyRow = true;
            foreach (var cell in row.Cells)
            {
                if (!string.IsNullOrEmpty(cell.ToString()))
                    emptyRow = false;
            }
            if (emptyRow)
                Worksheet.RemoveRow(row);
        }*/
        //try
        {
            using (var memStream = new MemoryStream())
            {
                Workbook.Write(memStream);
                memStream.Flush();
                memStream.Position = 0;

                using (var fileStream = new FileStream(Path, FileMode.Create, FileAccess.Write))
                {
                    var data = memStream.ToArray();
                    fileStream.Write(data, 0, data.Length);
                    fileStream.Flush();
                }
            }

        }
        //catch (Exception e)
        //{
        //    CDebug.LogError(e.Message);
        //    CDebug.LogError("是否打开了Excel表？");
        //}

    }
}
