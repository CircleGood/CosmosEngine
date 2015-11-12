﻿//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
// tab文件读写，参照c++库
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;

/// <summary>
/// 性能更好，不能写入的Tab读取器
/// </summary>
[System.Obsolete("不支持行头定义类型！暂时不用了！")]
public class CTabReader : ICTabReadble, IDisposable
{
    Stream m_tableStream;
    StreamReader m_tableReader;

    Dictionary<string, int> m_attrDict = null;  /*表格属性列 */
    int m_RowCount = 0;
    int m_CursorPos = 0;
    string[] m_CachedColumns; // 当前行缓存(已拆分)

    private string m_FileName = "";  // LoadFromFile调用时才产生

    /** 构造器 */
    private CTabReader() { }

    /*  从文件对象中创建 */
    public static CTabReader LoadFromFile(string path)
    {
        CTabReader tableFile = null;
        try
        {
            byte[] fileBuffer = File.ReadAllBytes(path);
            MemoryStream stream = new MemoryStream(fileBuffer);
            stream.Read(fileBuffer, 0, Convert.ToInt32(stream.Length));
            tableFile = LoadFromContent(path, fileBuffer);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        return tableFile;
    }
    /* 从字符串中创建对象 */
    public static CTabReader LoadFromString(string filename, string str)
    {
        return LoadFromContent(filename, Encoding.UTF8.GetBytes(str));
    }

    /* 从字符串中创建对象 */
    public static CTabReader LoadFromContent(string filename, byte[] data)
    {
        CTabReader tableFile = new CTabReader();

        tableFile.m_FileName = filename;  // 文件名保存，用于输出
        //byte[] tableBytes = Convert.FromBase64String(txt);  // string -> bytes -> stream
        tableFile.m_tableStream = new MemoryStream(data);
        //tableFile.m_tableReader = new StreamReader(tableFile.m_tableStream);

        tableFile.ParseColumnNames(tableFile.m_tableStream);

        tableFile.ParseRowCount(data);

        tableFile.InitStreamReader(); // reset cursor position

        return tableFile;
    }

    /* 初始化实例对象的Reader，回到起点，用于下一次读取 */
    public void InitStreamReader()
    {
        this.m_tableStream.Seek(0, 0);
        m_CursorPos = 0;

        this.m_tableReader = new StreamReader(this.m_tableStream, Encoding.UTF8);
    }

    // 开始实例方法

    /// 初始化列名表
    private bool ParseColumnNames(Stream stream)
    {
        var streamReader = new StreamReader(stream, Encoding.UTF8);
        m_attrDict = new Dictionary<string, int>();

        string attrLine = streamReader.ReadLine();
        string[] attrArray = attrLine.Split('\t');

        for (int i = 0; i < attrArray.Length; i++)
        {
            if (string.IsNullOrEmpty(attrArray[i]))
            {
                Debug.LogError(string.Format("读取表格{0}的第{1}列，列头是空字符串！请检查", m_FileName, i));
                continue;
            }

            /* 列从1开始，  i+1所以 */
            try
            {
                this.m_attrDict.Add(attrArray[i], i + 1);  /* 放入字典 -> ( ColumnName,  ColumnNum ) (名字，列数）*/
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("添加文件{0}列名[{1}] 时出错: {2}", m_FileName, attrArray[i], e.Message));
                continue;
            }
        }
        return true;
    }

    /* 根据列名获取列的数字 */
    private int findColumnByName(string columnName)
    {
        if (m_attrDict.ContainsKey(columnName))
        {
            return this.m_attrDict[columnName];
        }

        Debug.LogError(string.Format("找不到列名[{0}]!在表{1}", columnName, m_FileName));
        return 0;
    }

    /** 获取第一行第column列的名字， 即表头属性名 */
    public string GetColumnName(int column)
    {
        foreach (KeyValuePair<string, int> attr in this.m_attrDict)
        {
            if (attr.Value == column)
            {
                return attr.Key;
            }
        }

        /* 没找到 */
        return null;
    }

    public bool HasColumn(string column)
    {
        if (findColumnByName(column) == 0)
        {
            return false;
        }

        return true;
    }

    /** 列数 */
    public int GetColumnsCount()
    {
        return m_attrDict.Count;
    }

    /* 设置值, 返回成功与否， 不抛出异常 */
    public string GetString(int row, string columnName)
    {
        string outVal = "";

        try
        {
            int col = findColumnByName(columnName);
            outVal = GetString(row, col);
        }
        catch (IndexOutOfRangeException)
        {
            //outVal = defaultVal;  // 取默认值
        }

        return outVal;
    }

    /* 获取表格内容字符串 */
    public string GetString(int row, int column)
    {
        if (row < (m_CursorPos - 1))
            this.InitStreamReader(); // 一般不会往前读，避免了seek

        if (row > m_RowCount)
            throw new IndexOutOfRangeException();

        if (row == (m_CursorPos - 1))
        {
            return m_CachedColumns[column - 1].Trim(); // 去空格
        }

        do
        {
            string line = m_tableReader.ReadLine();
            if (line == null)
                break;
            if (line == string.Empty)
                continue; // 注意没有递增m_CursorPos

            m_CursorPos++; // 递增行数游标

            if (row == (m_CursorPos - 1))
            {
                m_CachedColumns = line.Split('\t');

                return m_CachedColumns[column - 1].Trim();  // 去空格
            }
        } while (true);

        // 仍然没找到？内部状态乱了
        throw new IndexOutOfRangeException();
    }

    public int GetInteger(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return (int)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public int GetInteger(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return (int)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public uint GetUInteger(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return (uint)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public uint GetUInteger(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return (uint)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }
    public double GetDouble(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return double.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public double GetDouble(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return double.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public float GetFloat(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public float GetFloat(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public bool GetBool(int row, int column)
    {
        int field = GetInteger(row, column);
        return field != 0;
    }

    public bool GetBool(int row, string columnName)
    {
        int field = GetInteger(row, columnName);
        return field != 0;
    }

    public int GetHeight()
    {
        return GetRowsCount();
    }

    public int GetColumnCount()
    {
        return m_CachedColumns.Length;
    }

    private bool ParseRowCount(byte[] bytes)
    {
        m_RowCount = 0;
        for (uint i = 0; i < bytes.Length; ++i)
        {
            // 匹配\r\n和\n组合
            if (bytes[i] == '\r')
            {
                if (bytes[i + 1] == '\n')
                {
                    ++i;
                }
                m_RowCount++;
            }
            else if (bytes[i] == '\n')
            {
                m_RowCount++;
            }
        }

        if (bytes.Length > 0)
        {
            // 末尾没空行的
            if (bytes[bytes.Length - 1] != '\r' && bytes[bytes.Length - 1] != '\n')
                m_RowCount++;
        }

        if (m_RowCount > 0)
            m_RowCount--; // 减去第一行column header
        return true;
    }

    /// 获取行数（不含第一行的ColumnHeader）
    public int GetRowsCount()
    {
        return m_RowCount;
    }

    public void Close()
    {
        m_tableReader.Close();
        m_tableStream = null;
        m_tableReader = null;
    }

    public void Dispose()
    {
        Close();
    }
}