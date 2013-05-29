﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BCad.Iges.Directory;
using BCad.Iges.Entities;
using BCad.Iges.Parameter;

namespace BCad.Iges
{
    internal class IgesFileReader
    {
        public IgesFile Load(Stream stream)
        {
            var file = new IgesFile();
            var allLines = new StreamReader(stream).ReadToEnd().Split("\n".ToCharArray()).Select(s => s.TrimEnd());
            string terminateLine = null;
            var startLines = new List<string>();
            var globalLines = new List<string>();
            var directoryLines = new List<string>();
            var parameterLines = new List<string>();
            var parameterData = new Dictionary<int, IgesParameterData>();
            var sectionLines = new Dictionary<IgesSectionType, List<string>>()
                {
                    { IgesSectionType.Start, startLines },
                    { IgesSectionType.Global, globalLines },
                    { IgesSectionType.Directory, directoryLines },
                    { IgesSectionType.Parameter, parameterLines }
                };

            foreach (var line in allLines)
            {
                if (line.Length != 80)
                    throw new IgesException("Expected line length of 80 characters.");
                var data = line.Substring(0, IgesFile.MaxDataLength);
                var sectionType = SectionTypeFromCharacter(line[IgesFile.MaxDataLength]);
                var lineNumber = int.Parse(line.Substring(IgesFile.MaxDataLength + 1).TrimStart());

                if (sectionType == IgesSectionType.Terminate)
                {
                    if (terminateLine != null)
                        throw new IgesException("Unexpected duplicate terminate line");
                    terminateLine = data;

                    // verify terminate data and quit
                    var startCount = int.Parse(terminateLine.Substring(1, 7));
                    var globalCount = int.Parse(terminateLine.Substring(9, 7));
                    var directoryCount = int.Parse(terminateLine.Substring(17, 7));
                    var parameterCount = int.Parse(terminateLine.Substring(25, 7));
                    if (startLines.Count != startCount)
                        throw new IgesException("Incorrect number of start lines reported");
                    if (globalLines.Count != globalCount)
                        throw new IgesException("Incorrect number of global lines reported");
                    if (directoryLines.Count != directoryCount)
                        throw new IgesException("Incorrect number of directory lines reported");
                    if (parameterLines.Count != parameterCount)
                        throw new IgesException("Incorrect number of parameter lines reported");
                    break;
                }
                else
                {
                    if (sectionType == IgesSectionType.Parameter)
                        data = data.Substring(0, data.Length - 8); // parameter data doesn't need its last 8 bytes
                    sectionLines[sectionType].Add(data);
                    if (sectionLines[sectionType].Count != lineNumber)
                        throw new IgesException("Unordered line number");
                }
            }

            // don't worry if terminate line isn't present

            ParseGlobalLines(file, globalLines);
            ParseParameterLines(file, parameterLines, parameterData);
            ParseDirectoryLines(file, directoryLines, parameterData);

            return file;
        }

        private static void ParseGlobalLines(IgesFile file, List<string> globalLines)
        {
            var fullString = string.Join(string.Empty, globalLines).TrimEnd();
            if (string.IsNullOrEmpty(fullString))
                return;

            string temp;
            int index = 0;
            for (int field = 1; field <= 26; field++)
            {
                switch (field)
                {
                    case 1:
                        temp = ParseString(file, fullString, ref index, file.FieldDelimiter.ToString());
                        if (temp == null || temp.Length != 1)
                            throw new IgesException("Expected delimiter of length 1");
                        file.FieldDelimiter = temp[0];
                        break;
                    case 2:
                        temp = ParseString(file, fullString, ref index, file.RecordDelimiter.ToString());
                        if (temp == null || temp.Length != 1)
                            throw new IgesException("Expected delimiter of length 1");
                        file.RecordDelimiter = temp[0];
                        break;
                    case 3:
                        file.Identification = ParseString(file, fullString, ref index);
                        break;
                    case 4:
                        file.FullFileName = ParseString(file, fullString, ref index);
                        break;
                    case 5:
                        file.SystemIdentifier = ParseString(file, fullString, ref index);
                        break;
                    case 6:
                        file.SystemVersion = ParseString(file, fullString, ref index);
                        break;
                    case 7:
                        file.IntegerSize = ParseInt(file, fullString, ref index);
                        break;
                    case 8:
                        file.SingleSize = ParseInt(file, fullString, ref index);
                        break;
                    case 9:
                        file.DecimalDigits = ParseInt(file, fullString, ref index);
                        break;
                    case 10:
                        file.DoubleMagnitude = ParseInt(file, fullString, ref index);
                        break;
                    case 11:
                        file.DoublePrecision = ParseInt(file, fullString, ref index);
                        break;
                    case 12:
                        file.Identifier = ParseString(file, fullString, ref index);
                        break;
                    case 13:
                        file.ModelSpaceScale = ParseDouble(file, fullString, ref index);
                        break;
                    case 14:
                        file.ModelUnits = (IgesUnits)ParseInt(file, fullString, ref index, (int)file.ModelUnits);
                        break;
                    case 15:
                        file.CustomModelUnits = ParseString(file, fullString, ref index);
                        break;
                    case 16:
                        file.MaxLineWeightGraduations = ParseInt(file, fullString, ref index);
                        break;
                    case 17:
                        file.MaxLineWeight = ParseDouble(file, fullString, ref index);
                        break;
                    case 18:
                        file.TimeStamp = ParseDateTime(ParseString(file, fullString, ref index), file.TimeStamp);
                        break;
                    case 19:
                        file.MinimumResolution = ParseDouble(file, fullString, ref index);
                        break;
                    case 20:
                        file.MaxCoordinateValue = ParseDouble(file, fullString, ref index);
                        break;
                    case 21:
                        file.Author = ParseString(file, fullString, ref index);
                        break;
                    case 22:
                        file.Organization = ParseString(file, fullString, ref index);
                        break;
                    case 23:
                        file.IgesVersion = (IgesVersion)ParseInt(file, fullString, ref index);
                        break;
                    case 24:
                        file.DraftingStandard = (IgesDraftingStandard)ParseInt(file, fullString, ref index);
                        break;
                    case 25:
                        file.ModifiedTime = ParseDateTime(ParseString(file, fullString, ref index), file.ModifiedTime);
                        break;
                    case 26:
                        file.ApplicationProtocol = ParseString(file, fullString, ref index);
                        break;
                }
            }
        }

        private static void ParseParameterLines(IgesFile file, List<string> parameterLines, Dictionary<int, IgesParameterData> parameterData)
        {
            // group parameter lines together
            int index = 1;
            var sb = new StringBuilder();
            for (int i = 0; i < parameterLines.Count; i++)
            {
                var line = parameterLines[i].Substring(0, 64); // last 16 bytes aren't needed
                sb.Append(line);
                if (line.TrimEnd().EndsWith(file.RecordDelimiter.ToString())) // TODO: string may contain delimiter
                {
                    var fullLine = sb.ToString();
                    var fields = SplitFields(fullLine, file.FieldDelimiter, file.RecordDelimiter);
                    if (fields.Count < 2)
                        throw new IgesException("At least two fields necessary");
                    var entityType = (IgesEntityType)int.Parse(fields[0]);
                    var data = IgesParameterData.ParseFields(entityType, fields.Skip(1).ToList());
                    if (data != null)
                        parameterData.Add(index, data);
                    index = i + 2; // +1 for zero offset, +1 to skip to the next line
                    sb.Clear();
                }
            }
        }

        private static void ParseDirectoryLines(IgesFile file, List<string> directoryLines, Dictionary<int, IgesParameterData> parameterData)
        {
            if (directoryLines.Count % 2 != 0)
                throw new IgesException("Expected an even number of lines");

            var transformationMatricies = new Dictionary<int, IgesTransformationMatrix>();

            for (int i = 0; i < directoryLines.Count; i += 2)
            {
                var lineNumber = i + 1;
                var line1 = directoryLines[i];
                var line2 = directoryLines[i + 1];
                var entityTypeNumber = int.Parse(GetField(line1, 1));
                if (entityTypeNumber != 0)
                {
                    var dir = new IgesDirectoryData();
                    dir.EntityType = (IgesEntityType)entityTypeNumber;
                    dir.ParameterPointer = int.Parse(GetField(line1, 2));
                    dir.Structure = int.Parse(GetField(line1, 3));
                    dir.LineFontPattern = int.Parse(GetField(line1, 4));
                    dir.Level = int.Parse(GetField(line1, 5));
                    dir.View = int.Parse(GetField(line1, 6));
                    dir.TransformationMatrixPointer = int.Parse(GetField(line1, 7));
                    dir.LableDisplay = int.Parse(GetField(line1, 8));
                    dir.StatusNumber = int.Parse(GetField(line1, 9));

                    dir.LineWeight = int.Parse(GetField(line2, 2));
                    dir.Color = (IgesColorNumber)int.Parse(GetField(line2, 3)); // TODO: could be a negative pointer
                    dir.LineCount = int.Parse(GetField(line2, 4));
                    dir.FormNumber = int.Parse(GetField(line2, 5));
                    dir.EntityLabel = GetField(line2, 8, null);
                    dir.EntitySubscript = int.Parse(GetField(line2, 9));

                    if (dir.TransformationMatrixPointer >= lineNumber)
                        throw new IgesException("Pointer must point back");

                    if (parameterData.ContainsKey(dir.ParameterPointer))
                    {
                        var data = parameterData[dir.ParameterPointer];
                        var entity = IgesEntity.CreateEntity(data, dir, transformationMatricies);
                        if (entity.Type == IgesEntityType.TransformationMatrix)
                        {
                            transformationMatricies.Add(lineNumber, (IgesTransformationMatrix)entity);
                        }
                        file.Entities.Add(entity);
                    }
                }
            }
        }

        private static string GetField(string str, int field, string defaultValue = "0")
        {
            var size = 8;
            var offset = (field - 1) * size;
            var value = str.Substring(offset, size).Trim();
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private static List<string> SplitFields(string input, char fieldDelimiter, char recordDelimiter)
        {
            // TODO: watch for strings containing delimiters
            var fields = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (c == fieldDelimiter || c == recordDelimiter)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                    if (c == recordDelimiter)
                    {
                        break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return fields;
        }

        private static string ParseString(IgesFile file, string str, ref int index, string defaultValue = null)
        {
            if (index < str.Length && (str[index] == file.FieldDelimiter || str[index] == file.RecordDelimiter))
            {
                // swallow the delimiter and return the default
                index++;
                return defaultValue;
            }

            SwallowWhitespace(str, ref index);

            var sb = new StringBuilder();

            // parse length
            for (; index < str.Length; index++)
            {
                var c = str[index];
                if (c == 'H')
                {
                    index++; // swallow H
                    break;
                }
                if (!char.IsDigit(c))
                    throw new IgesException("Expected digit");
                sb.Append(c);
            }

            var lengthString = sb.ToString();
            if (string.IsNullOrWhiteSpace(lengthString))
            {
                return defaultValue;
            }

            int length = int.Parse(lengthString);
            sb.Clear();

            // parse content
            var value = str.Substring(index, length);
            index += length;

            // verify delimiter and swallow
            if (index == str.Length - 1)
                SwallowDelimiter(str, file.RecordDelimiter, ref index);
            else
                SwallowDelimiter(str, file.FieldDelimiter, ref index);

            return value;
        }

        private static int ParseInt(IgesFile file, string str, ref int index, int defaultValue = 0)
        {
            if (index < str.Length && (str[index] == file.FieldDelimiter || str[index] == file.RecordDelimiter))
            {
                // swallow the delimiter and return the default
                index++;
                return defaultValue;
            }

            SwallowWhitespace(str, ref index);

            var sb = new StringBuilder();
            for (; index < str.Length; index++)
            {
                var c = str[index];
                if (c == file.FieldDelimiter || c == file.RecordDelimiter)
                {
                    index++; // swallow it
                    break;
                }
                if (!char.IsDigit(c))
                    throw new IgesException("Expected digit");
                sb.Append(c);
            }

            return int.Parse(sb.ToString());
        }

        private static double ParseDouble(IgesFile file, string str, ref int index, double defaultValue = 0.0)
        {
            if (index < str.Length && (str[index] == file.FieldDelimiter || str[index] == file.RecordDelimiter))
            {
                // swallow the delimiter and return the default
                index++;
                return defaultValue;
            }

            SwallowWhitespace(str, ref index);

            var sb = new StringBuilder();
            for (; index < str.Length; index++)
            {
                var c = str[index];
                if (c == file.FieldDelimiter || c == file.RecordDelimiter)
                {
                    index++; // swallow it
                    break;
                }
                sb.Append(c);
            }

            return double.Parse(sb.ToString());
        }

        private static DateTime ParseDateTime(string value, DateTime defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DateTime.Now;
            }

            var match = dateTimeReg.Match(value);
            if (!match.Success)
                throw new IgesException("Invalid date/time format");
            Debug.Assert(match.Groups.Count == 9);
            int year = int.Parse(match.Groups[1].Value);
            int month = int.Parse(match.Groups[4].Value);
            int day = int.Parse(match.Groups[5].Value);
            int hour = int.Parse(match.Groups[6].Value);
            int minute = int.Parse(match.Groups[7].Value);
            int second = int.Parse(match.Groups[8].Value);
            if (match.Groups[1].Value.Length == 2)
                year += 1900;
            return new DateTime(year, month, day, hour, minute, second);
        }

        private static void SwallowWhitespace(string str, ref int index)
        {
            for (; index < str.Length; index++)
            {
                var c = str[index];
                if (!char.IsWhiteSpace(c))
                    break;
            }
        }

        private static Regex dateTimeReg = new Regex(@"((\d{2})|(\d{4}))(\d{2})(\d{2})\.(\d{2})(\d{2})(\d{2})", RegexOptions.Compiled);
        //                                             12       3       4      5        6      7      8

        private static void SwallowDelimiter(string str, char delim, ref int index)
        {
            if (index >= str.Length)
                throw new IgesException("Unexpected end of string");
            if (str[index++] != delim)
                throw new IgesException("Expected delimiter");
        }

        private static char SectionTypeChar(IgesSectionType type)
        {
            switch (type)
            {
                case IgesSectionType.Start: return 'S';
                case IgesSectionType.Global: return 'G';
                case IgesSectionType.Directory: return 'D';
                case IgesSectionType.Parameter: return 'P';
                case IgesSectionType.Terminate: return 'T';
                default:
                    throw new IgesException("Unexpected section type " + type);
            }
        }

        private static IgesSectionType SectionTypeFromCharacter(char c)
        {
            switch (c)
            {
                case 'S': return IgesSectionType.Start;
                case 'G': return IgesSectionType.Global;
                case 'D': return IgesSectionType.Directory;
                case 'P': return IgesSectionType.Parameter;
                case 'T': return IgesSectionType.Terminate;
                default:
                    throw new IgesException("Invalid section type " + c);
            }
        }
    }
}
