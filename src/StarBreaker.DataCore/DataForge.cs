﻿using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public static class DataForge
{
    public static DataForge<string> FromDcbPathJson(string path)
    {
        return new DataForge<string>(new DataCoreBinaryJson(new DataCoreDatabase(File.OpenRead(path))));
    }

    public static DataForge<string> FromDcbStreamJson(Stream dcbStream)
    {
        return new DataForge<string>(new DataCoreBinaryJson(new DataCoreDatabase(dcbStream)));
    }

    public static DataForge<string> FromDcbPathXml(string path)
    {
        return new DataForge<string>(new DataCoreBinaryXml(new DataCoreDatabase(File.OpenRead(path))));
    }

    public static DataForge<string> FromDcbStreamXml(Stream dcbStream)
    {
        return new DataForge<string>(new DataCoreBinaryXml(new DataCoreDatabase(dcbStream)));
    }

    public static DataForge<JsonObject> FromDcbPathJsonNode(string path)
    {
        return new DataForge<JsonObject>(new DataCoreBinaryJsonObject(new DataCoreDatabase(File.OpenRead(path))));
    }
}

public class DataForge<T>
{
    public IDataCoreBinary<T> DataCore { get; }

    public DataForge(IDataCoreBinary<T> dataCore)
    {
        DataCore = dataCore;
    }

    public Dictionary<string, DataCoreRecord> GetRecordsByFileName(string? fileNameFilter = null)
    {
        var structsPerFileName = new Dictionary<string, DataCoreRecord>();
        foreach (var recordId in DataCore.Database.MainRecords)
        {
            var record = DataCore.Database.GetRecord(recordId);
            var fileName = record.GetFileName(DataCore.Database);

            if (fileNameFilter != null && !FileSystemName.MatchesSimpleExpression(fileNameFilter, fileName))
                continue;

            structsPerFileName[fileName] = record;
        }

        return structsPerFileName;
    }

    public T GetFromRecord(DataCoreRecord record)
    {
        return DataCore.GetFromMainRecord(record);
    }

    public T GetFromRecord(CigGuid recordGuid)
    {
        return DataCore.GetFromMainRecord(DataCore.Database.GetRecord(recordGuid));
    }

    public Dictionary<string, string[]> ExportEnums()
    {
        var result = new Dictionary<string, string[]>(DataCore.Database.EnumDefinitions.Length);

        foreach (var enumDef in DataCore.Database.EnumDefinitions)
        {
            var enumValues = new string[enumDef.ValueCount];
            for (var i = 0; i < enumDef.ValueCount; i++)
            {
                enumValues[i] = DataCore.Database.GetString2(DataCore.Database.EnumOptions[enumDef.FirstValueIndex + i]);
            }

            result.Add(enumDef.GetName(DataCore.Database), enumValues);
        }

        return result;
    }

    public void ExtractAll(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        foreach (var (fileName, record) in recordsByFileName)
        {
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            DataCore.SaveToFile(record, filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 1000 records and when we are done
            if (currentProgress == total || currentProgress % 1000 == 0)
                progress?.Report(currentProgress / (double)total);
        }

        progress?.Report(1);
    }

    public void ExtractAllParallel(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        Parallel.ForEach(recordsByFileName, kvp =>
        {
            var (fileName, record) = kvp;
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            DataCore.SaveToFile(record, filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 1000 records and when we are done
            if (currentProgress == total || currentProgress % 1000 == 0)
                progress?.Report(currentProgress / (double)total);
        });

        progress?.Report(1);
    }
}