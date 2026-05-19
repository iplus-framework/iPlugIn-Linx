using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;


namespace linx.core.reporthandler
{
    public partial class LinxPrinterXShared
    {

        #region RCI-Functions

        public void AddCheckStatusToJob(ILinxPrintJob linxPrintJob)
        {
            byte[] data = GetData(LinxPrinterCommandCodeEnum.Printer_Status_Request, null);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.CheckStatus, data));
        }


        /// <summary>
        /// Add to message queue
        /// starting print command
        /// </summary>
        /// <param name="linxPrintJob"></param>
        public void AddPrintCommandToJob(ILinxPrintJob linxPrintJob, bool bStop = false)
        {
            /*
                 E.1.10	Start Print Command
                 1B 02	;ESC STX sequence
                 11	;Command ID
                 1B 03	;ESC ETX sequence
                 EA	;Checksum

                 Printer Reply
                 1B 06	;ESC ACK sequence
                 00	;P-Status - No printer errors
                 00	;C-Status - No command errors
                 11	;Command ID sent
                 1B 03	;ESC ETX sequence
                 E6	;Checksum

             */
            // LinxASCIControlCharacterEnum.VT == 0xB == 11
            byte[] data = GetData(bStop ? LinxASCIControlCharacterEnum.DC2 : LinxASCIControlCharacterEnum.DC1, null);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.ControlCmd, data));
        }

        /// <summary>
        /// add to print queue
        /// Switch on Printer
        /// </summary>
        /// <param name="linxPrintJob"></param>
        public void AddJetCommandToJob(ILinxPrintJob linxPrintJob, bool bStop = false)
        {
            /*
                E.1.9	Start Jet Command
                1B 02	;ESC STX sequence
                0F	;Command ID
                1B 03	;ESC ETX sequence
                EC	;Checksum

                Printer Reply
                1B 06	;ESC ACK sequence
                00	;P-Status - No printer errors
                00	;C-Status - No command errors
                0F	;Command ID sent
                1B 03	;ESC ETX sequence
                E8	;Checksum
            */
            // LinxASCIControlCharacterEnum.SI == 0xF
            byte[] data = GetData(bStop ? LinxASCIControlCharacterEnum.DLE : LinxASCIControlCharacterEnum.SI, null);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.ControlCmd, data));
        }


        public void AddGetRasterDataToJob(ILinxPrintJob linxPrintJob)
        {
            byte[] data = GetData(LinxASCIControlCharacterEnum.CAN, null);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.RasterData, data));
        }


        public void AddDeleteReportToJob(ILinxPrintJob linxPrintJob)
        {
            string reportName = linxPrintJob.Name; // flowDoc.Name;
            byte[] reportNameByte = Encoding.ASCII.GetBytes(reportName);
            byte[] telegram = new byte[17];
            telegram[0] = 0x01;
            Array.Copy(reportNameByte, 0, telegram, 1, reportNameByte.Length);
            // LinxASCIControlCharacterEnum.RS == 0x1E
            byte[] data = GetData(LinxASCIControlCharacterEnum.ESC, telegram);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.DeleteReport, data));
        }


        /// <summary>
        /// Loading remote layout
        /// </summary>
        /// <param name="linxPrintJob"></param>
        public void AddPrintMessageToJob(ILinxPrintJob linxPrintJob)
        {
            string reportName = linxPrintJob.Name; // flowDoc.Name;
            byte[] reportNameByte = Encoding.ASCII.GetBytes(reportName);
            byte[] telegram = new byte[18];
            Array.Copy(reportNameByte, telegram, reportNameByte.Length);
            // LinxASCIControlCharacterEnum.RS == 0x1E
            byte[] data = GetData(LinxASCIControlCharacterEnum.RS, telegram);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.LoadReport, data));
        }
        #endregion

        #region Add Field Values

        /// <summary>
        /// Add remote field value for post processing to LinxPrintJob feed
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="name"></param>
        /// <param name="text"></param>
        public void AddTextValueForRemoteField(ILinxPrintJob linxPrintJob, string name, string text)
        {
            byte[] textByte = linxPrintJob.Encoding.GetBytes(text);
            linxPrintJob.RemoteFieldValues.Add(textByte);
        }


        /// <summary>
        /// Adds one text field to a direct LINX download message.
        /// </summary>
        public void AddTextValueToPrintMessage(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, string aggregateGroup, string text)
        {
            if (linxPrintJob == null || string.IsNullOrWhiteSpace(text))
                return;

            LinxDataSetData dataSet = PrintServer.DataSets?.FirstOrDefault(c =>
                string.Equals(c.DataSetName, aggregateGroup, StringComparison.OrdinalIgnoreCase));

            if (dataSet == null)
                dataSet = PrintServer.DataSets?.FirstOrDefault();

            LinxField linxField = GetLinxField(linxPrintJob.Encoding, linxPrintJob, fieldOptions, dataSet, text);
            if (linxField != null)
                linxPrintJob.LinxFields.Add(linxField);
        }


        /// <summary>
        /// add to print queue
        /// barcode data
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineBarcode"></param>
        public void AddBarcodeValueToPrintMessage(ILinxPrintJob linxPrintJob, string aggregateGroup, string barcodeValue)
        {
            // TODO: @aagincic LinxPrinter DownloadBarcodeValue

            /*
                7C	;Command ID - Download Message Data
                01	;Number of messages
                79 00	;Message length in bytes
                5C 01	;Message length in rasters
                00	;EHT setting - set to null
                00 00	;Inter-raster width
                10 00	;Print delay
                6D 65 73 73 61 67 65 37	;Message name - message7.pat
                2E 70 61 74 00 00 00 00
                35 30 30 20 48 69 67 68	;Raster name - 500 High
                00 00 00 00 00 00 00 00
                1C	;Field header
                C0	;Field type - Text field (not rendered)
                2C 00	;Field length in bytes
                FA FF	;Y position
                00 00	;X position
                40 01	;Field length in rasters
                2D 00	;Field height in drops
                00	;Format 3
                01	;Bold multiplier
                07	;String length (excluding null)
                00	;Format 1
                00	;Format 2
                01	;Linkage - points to Bar Code
                00	;Reserved - set to null
                00	;Reserved - set to null
                34 35 20 48 69 20 42 61	;Data set name - 45 Hi Barcode
                72 63 6F 64 65 00 00 00
                31 32 33 34 35 36 37 00	;Data - 1234567
 
                1C	;Field header
                46	;Field type - Bar code field
                24 00	;Field length in bytes
                00 00	;Y position
                00 00	;X position
                5C 01	;Field length in rasters
                E5 00	;Field height in drops
                00	;Format 3
                04	;Bold multiplier
                00	;Text length (excluding null)
                00	;Format 1
                03	;Format 2 - Checksum on, attached text; on
                00	;Linkage
                00	;Reserved
                00	;Reserved
                45 41 4E 2D 38 20 20 20	;Data set name - EAN-8
                20 20 20 20 20 20 20 00

         */
            // LinxDataSetData dataSet = PrintServer.DataSets.Where(c => c.DataSetName == aggregateGroup).FirstOrDefault();
            // if (dataSet == null)
            // {
            //     dataSet = PrintServer.DataSets.FirstOrDefault();
            // }
            // LinxField linxField = GetLinxField(linxPrintJob.Encoding, linxPrintJob, null, dataSet, barcodeValue, 0x46);
            // linxPrintJob.LinxFields.Add(linxField);
        }

        #endregion

        #region Build Objects

        public virtual LinxField GetLinxField(Encoding encoding, ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, LinxDataSetData dataSet, string value, byte fieldType = 0x00)
        {
            if (string.IsNullOrWhiteSpace(value) || linxPrintJob == null)
                return null;

            LinxField field = new LinxField();
            Encoding encoder = encoding ?? linxPrintJob.Encoding ?? Encoding.ASCII;
            byte[] tmp = encoder.GetBytes(value);
            byte[] valueByte = new byte[tmp.Length + 1];
            Array.Copy(tmp, valueByte, tmp.Length);
            field.ValueByte = valueByte;
            field.Header = GetLinxFieldHeader(dataSet, linxPrintJob, fieldOptions, (short)value.Length, (short)field.ValueByte.Length, fieldType);
            field.Value = value;
            return field;
        }
    
    
        public virtual LinxFieldHeader GetLinxFieldHeader(LinxDataSetData dataSet, ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, short valueLength, short valueByteLength, byte fieldType = 0x00)
        {
            LinxFieldHeader linxFieldHeader = new LinxFieldHeader();
            linxFieldHeader.FieldType = fieldType;

            byte[] fieldLengthInBytes = BitConverter.GetBytes(valueByteLength + LinxFieldHeader.ConstHeaderLength);
            Array.Copy(fieldLengthInBytes, linxFieldHeader.FieldLengthInBytes, System.Math.Min(linxFieldHeader.FieldLengthInBytes.Length, fieldLengthInBytes.Length));

            byte[] fieldLengthInRasters = GetFieldLengthInRasters(linxPrintJob, fieldOptions, dataSet, valueLength);
            Array.Copy(fieldLengthInRasters, linxFieldHeader.FieldLengthInRasters, System.Math.Min(linxFieldHeader.FieldLengthInRasters.Length, fieldLengthInRasters.Length));

            linxFieldHeader.TextLength = (byte)valueLength;
            linxFieldHeader.FieldHeightInDrops = GetFieldHeightInDrops(linxPrintJob, fieldOptions, dataSet, valueLength);

            int xPos = Math.Max(0, fieldOptions?.XPos ?? 0);
            int yPos = Math.Max(0, fieldOptions?.YPos ?? 0);

            byte[] xpos = BitConverter.GetBytes((short)Math.Min(short.MaxValue, xPos));
            Array.Copy(xpos, linxFieldHeader.XPosition, System.Math.Min(linxFieldHeader.XPosition.Length, xpos.Length));
            linxFieldHeader.YPosition = (byte)Math.Min(byte.MaxValue, yPos);

            string dataSetName = dataSet?.DataSetName
                ?? PrintServer.DataSets?.FirstOrDefault()?.DataSetName
                ?? string.Empty;

            byte[] dataSetNameBytes = Encoding.ASCII.GetBytes(dataSetName);
            Array.Copy(dataSetNameBytes, linxFieldHeader.DataSetName, System.Math.Min(linxFieldHeader.DataSetName.Length - 1, dataSetNameBytes.Length));
            linxFieldHeader.DataSetName[15] = 0x00;

            return linxFieldHeader;
        }

        public LinxMessageHeader GetLinxMessageHeader(string messageName, string rasterName, short numOfMessages, short msgLengthInBytes, short msgLengthInRasters)
        {
            LinxMessageHeader header = new LinxMessageHeader();

            // Message name	16 
            byte[] messageNameBytes = Encoding.ASCII.GetBytes(messageName);
            Array.Copy(messageNameBytes, header.MessageName, System.Math.Min(header.MessageName.Length, messageNameBytes.Length));

            byte[] rasterNameBytes = Encoding.ASCII.GetBytes(rasterName);
            Array.Copy(rasterNameBytes, header.RasterName, System.Math.Min(header.RasterName.Length, rasterNameBytes.Length));

            header.NumberOfMessages = (byte)numOfMessages;

            byte[] messageLengthInBytes = BitConverter.GetBytes(msgLengthInBytes);
            Array.Copy(messageLengthInBytes, header.MessageLengthInBytes, System.Math.Min(header.MessageLengthInBytes.Length, messageLengthInBytes.Length));

            byte[] messageLengthInRasters = BitConverter.GetBytes(msgLengthInRasters);
            Array.Copy(messageLengthInRasters, header.MessageLengthInRasters, System.Math.Min(header.MessageLengthInRasters.Length, messageLengthInRasters.Length));

            return header;
        }

        #endregion

        #region Calculation methods

        public List<LinxDataSetData> LoadDataSets()
        {
            return new List<LinxDataSetData>()
            {
                new LinxDataSetData()
                {
                    DataSetName = "5 High Caps",
                    Height= 5,
                    Width = 5,
                    InterCharacterSpace = 1
                },
                new LinxDataSetData()
                {
                    DataSetName = "6 High Full",
                    Height= 6,
                    Width = 5,
                    InterCharacterSpace = 1
                },
                new LinxDataSetData()
                {
                    DataSetName = "7 High Full",
                    Height= 7,
                    Width = 5,
                    InterCharacterSpace = 1
                },
                new LinxDataSetData()
                {
                    DataSetName = "9 High Caps",
                    Height= 9,
                    Width = 7,
                    InterCharacterSpace = 1
                },
                new LinxDataSetData()
                {
                    DataSetName = "9 High Full",
                    Height= 9,
                    Width = 5,
                    InterCharacterSpace = 1
                },
                new LinxDataSetData()
                {
                    DataSetName = "15 High Full",
                    Height= 15,
                    Width = 10,
                    InterCharacterSpace = 2
                },
                new LinxDataSetData()
                {
                    DataSetName = "15 High Caps",
                    Height= 15,
                    Width = 10,
                    InterCharacterSpace = 2
                },
                new LinxDataSetData()
                {
                    DataSetName = "23 High Caps",
                    Height= 23,
                    Width = 16,
                    InterCharacterSpace = 2
                }
                ,
                new LinxDataSetData()
                {
                    DataSetName = "32 High Caps",
                    Height= 32,
                    Width = 24,
                    InterCharacterSpace = 3
                }
            };
        }

        public virtual byte[] GetFieldLengthInRasters(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, LinxDataSetData dataSetData, short numberOfCharacters)
        {
            int interCharacterSpace = -1;
            int characterWidth = -1;

            if (fieldOptions != null && fieldOptions.CustomInt01 > 0 && fieldOptions.CustomInt02 > 0)
            {
                characterWidth = fieldOptions.CustomInt01;
                interCharacterSpace = fieldOptions.CustomInt02;
            }

            if ((interCharacterSpace <= -1 || characterWidth <= -1) && linxPrintJob != null)
            {
                characterWidth = linxPrintJob.CharacterWidth;
                interCharacterSpace = linxPrintJob.InterCharSpace;
            }

            if ((interCharacterSpace <= -1 || characterWidth <= -1) && dataSetData != null)
            {
                characterWidth = dataSetData.Width;
                interCharacterSpace = dataSetData.InterCharacterSpace;
            }

            if (interCharacterSpace <= -1 || characterWidth <= -1)
            {
                characterWidth = 5;
                interCharacterSpace = 1;
            }

            int length = (numberOfCharacters * (characterWidth + interCharacterSpace)) - interCharacterSpace;
            return BitConverter.GetBytes((short)length);
        }

 
        public byte GetFieldHeightInDrops(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, LinxDataSetData dataSetData, short numberOfCharacters)
        {
            int height = -1;

            if (fieldOptions != null && fieldOptions.CustomInt03 > 0)
                height = fieldOptions.CustomInt03;

            if (height <= -1 && linxPrintJob != null)
                height = linxPrintJob.FieldHeightDrop;

            if (height <= -1 && dataSetData != null)
                height = dataSetData.Height;

            if (height <= -1)
                height = 5;

            return (byte)Math.Min(byte.MaxValue, Math.Max(0, height));
        }
        #endregion

        #region Binary Serialization
        public string ByteStrPresentation(List<byte[]> downloadData)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var test in downloadData)
            {
                foreach (byte by in test)
                {
                    sb.Append(((short)by).ToString("X"));
                    sb.Append(" ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public byte[] GetData(LinxPrinterCommandCodeEnum commandCode, byte[] data)
        {
            return GetData((byte)commandCode, data);
        }

        public byte[] GetData(LinxASCIControlCharacterEnum commandCode, byte[] data)
        {
            return GetData((byte)commandCode, data);
        }

        public byte[] GetData(byte commandCode, byte[] data)
        {
            List<byte> result = new List<byte>();
            List<byte> checkSumList = new List<byte>();

            result.Add((byte)LinxASCIControlCharacterEnum.ESC);
            result.Add((byte)LinxASCIControlCharacterEnum.STX);
            checkSumList.Add((byte)LinxASCIControlCharacterEnum.STX);

            result.Add((byte)commandCode);
            checkSumList.Add((byte)commandCode);
            // If delete command, than add as escape sequence, without checksum-calc
            if (commandCode == (byte)LinxASCIControlCharacterEnum.ESC)
                result.Add((byte)commandCode);

            if (data != null)
            {
                result.AddRange(data);
                checkSumList.AddRange(data);
            }

            result.Add((byte)LinxASCIControlCharacterEnum.ESC);
            result.Add((byte)LinxASCIControlCharacterEnum.ETX);
            checkSumList.Add((byte)LinxASCIControlCharacterEnum.ETX);

            byte[] checkSumArr = checkSumList.ToArray();
            byte checkSum = LinxHelper.GetCheckSum(checkSumArr);
            result.Add(checkSum);
            if (checkSum == (byte)LinxASCIControlCharacterEnum.ESC)
                result.Add((byte)LinxASCIControlCharacterEnum.ESC);

            return result.ToArray();
        }

        #endregion
    }
}
