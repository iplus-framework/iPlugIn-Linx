using gip.core.datamodel;
using gip.core.reporthandlerwpf.Flowdoc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using linx.core.reporthandler;

namespace linx.core.reporthandlerwpf
{
    public partial class LinxPrinter
    {
        #region Properties
        public List<LinxDataSetData> DataSets { get; set; }
        #endregion

        #region RCI-Functions

        protected void AddCheckStatusToJob(ILinxPrintJob linxPrintJob)
        {
            _shared.AddCheckStatusToJob(linxPrintJob);
        }


        /// <summary>
        /// Add to message queue
        /// starting print command
        /// </summary>
        /// <param name="linxPrintJob"></param>
        protected void AddPrintCommandToJob(ILinxPrintJob linxPrintJob, bool bStop = false)
        {
            _shared.AddPrintCommandToJob(linxPrintJob, bStop);
        }

        /// <summary>
        /// add to print queue
        /// Switch on Printer
        /// </summary>
        /// <param name="linxPrintJob"></param>
        protected void AddJetCommandToJob(ILinxPrintJob linxPrintJob, bool bStop = false)
        {
            _shared.AddJetCommandToJob(linxPrintJob, bStop);
        }


        protected void AddGetRasterDataToJob(ILinxPrintJob linxPrintJob)
        {
            _shared.AddGetRasterDataToJob(linxPrintJob);
        }


        protected void AddDeleteReportToJob(ILinxPrintJob linxPrintJob, FlowDocument flowDoc)
        {
            _shared.AddDeleteReportToJob(linxPrintJob);
        }


        /// <summary>
        /// Loading remote layout
        /// </summary>
        /// <param name="linxPrintJob"></param>
        private void AddPrintMessageToJob(ILinxPrintJob linxPrintJob, FlowDocument flowDoc)
        {
            _shared.AddPrintMessageToJob(linxPrintJob);
        }
        #endregion

        #region Add Field Values

        /// <summary>
        /// Add remote field value for post processing to LinxPrintJob feed
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="name"></param>
        /// <param name="text"></param>
        private void AddTextValueForRemoteField(LinxPrintJob linxPrintJob, string name, string text)
        {
            _shared.AddTextValueForRemoteField(linxPrintJob, name, text);
        }


        /// <summary>
        /// add to print queue
        /// text data
        /// DownloadTextValue == sending text to printer -> printer downloads text
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="text"></param>
        private void AddTextValueToPrintMessage(LinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, string aggregateGroup, string text)
        {
            /*
                19	;Command ID - Download Message
                01	;Number of messages
                4E 00	;Message length in bytes
                17 00	;Message length in rasters
                06	;EHT setting
                00 00	;Inter-raster width
                10 00	;Print Delay
                6D 65 73 73 61 67 65 31	;Message name - message1.pat
                2E 70 61 74 00 00 00 00
                31 36 20 47 45 4E 20 53	;Raster name - 16 GEN STD
                54 44 00 00 00 00 00 00
                1C	;Field header
                00	;Field type – Text field
                25 00	;Field length in bytes
                00	;Y position
                00 00	;X position
                17 00	;Field length in rasters
                07	;Field height in drops
                00	;Format 3
                01	;Bold multiplier
 
                04	;String length (excluding null)
                00	;Format 1 – set to null
                00	;Format 2
                00	;Linkage
                37 20 48 69 67 68 20 46	;Data set name - 7 High Full
                75 6C 6C 00 00 00 00 00
                4C 69 6E 78 00	;Data - Linx (note the null terminator)



                Printer Reply:
                1B 06	;ESC ACK sequence
                00	;P-Status - No printer errors
                00	;C-Status - No command errors
                19	;Command ID sent
                1B 03	;ESC ETX sequence
                DE	;Checksum

            */
            LinxDataSetData dataSet = DataSets.Where(c => c.DataSetName == aggregateGroup).FirstOrDefault();
            if (dataSet == null)
            {
                dataSet = DataSets.FirstOrDefault();
            }
            LinxField linxField = GetLinxField(linxPrintJob.Encoding, linxPrintJob, inlineProp, dataSet, text);
            linxPrintJob.LinxFields.Add(linxField);
        }


        /// <summary>
        /// add to print queue
        /// barcode data
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineBarcode"></param>
        private void AddBarcodeValueToPrintMessage(LinxPrintJob linxPrintJob, string aggregateGroup, string barcodeValue)
        {
            _shared.AddBarcodeValueToPrintMessage(linxPrintJob, aggregateGroup, barcodeValue);
        }

        #endregion

        #region Build Objects

        public virtual LinxField GetLinxField(Encoding encoding, LinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, LinxDataSetData dataSet, string value, byte fieldType = 0x00)
        {
            LinxField field = new LinxField();
            byte[] tmp = encoding.GetBytes(value);
            byte[] valueByte = new byte[tmp.Length + 1];
            Array.Copy(tmp, valueByte, tmp.Length);
            field.ValueByte = valueByte;
            field.Header = GetLinxFieldHeader(dataSet, linxPrintJob, inlineProp, (short)value.Length, (short)field.ValueByte.Length, fieldType);
            field.Value = value;
            return field;
        }

        public virtual LinxFieldHeader GetLinxFieldHeader(LinxDataSetData dataSet, LinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, short valueLength, short valueByteLength, byte fieldType = 0x00)
        {
            LinxFieldHeader linxFieldHeader = new LinxFieldHeader();
            linxFieldHeader.FieldType = fieldType;

            byte[] fieldLengthInBytes = BitConverter.GetBytes(valueByteLength + LinxFieldHeader.ConstHeaderLength);
            Array.Copy(fieldLengthInBytes, linxFieldHeader.FieldLengthInBytes, System.Math.Min(linxFieldHeader.FieldLengthInBytes.Length, fieldLengthInBytes.Length));

            byte[] fieldLengthInRasters = GetFieldLengthInRasters(linxPrintJob, inlineProp, dataSet, valueLength);
            Array.Copy(fieldLengthInRasters, linxFieldHeader.FieldLengthInRasters, System.Math.Min(linxFieldHeader.FieldLengthInRasters.Length, fieldLengthInRasters.Length));

            linxFieldHeader.TextLength = (byte)valueLength;
            linxFieldHeader.FieldHeightInDrops = GetFieldHeightInDrops(linxPrintJob, inlineProp, dataSet, valueLength);

            byte[] xpos = BitConverter.GetBytes(inlineProp.XPos);
            Array.Copy(xpos, linxFieldHeader.XPosition, System.Math.Min(linxFieldHeader.XPosition.Length, xpos.Length));
            linxFieldHeader.YPosition = BitConverter.GetBytes(inlineProp.YPos)[0];

            // Data set name	15 bytes + null*
            Array.Copy(Encoding.ASCII.GetBytes(dataSet.DataSetName), linxFieldHeader.DataSetName, System.Math.Min(linxFieldHeader.DataSetName.Length - 1, dataSet.DataSetName.Length));
            linxFieldHeader.DataSetName[15] = 0x00;

            return linxFieldHeader;
        }

        public virtual LinxMessageHeader GetLinxMessageHeader(string messageName, string rasterName, short numOfMessages, short msgLengthInBytes, short msgLengthInRasters)
        {
            return _shared.GetLinxMessageHeader(messageName, rasterName, numOfMessages, msgLengthInBytes, msgLengthInRasters);
        }

        #endregion

        #region Calculation methods
        public virtual List<LinxDataSetData> LoadDataSets()
        {
            return _shared.LoadDataSets();
        }

        public virtual byte[] GetFieldLengthInRasters(LinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, LinxDataSetData dataSetData, short numberOfCharacters)
        {
            // CustomInt01 = character width
            // CustomInt02 = InterCharacterSpace
            int interCharacterSpace = -1;
            int characterWidth = -1;
            if (inlineProp.CustomInt01 > 0 && inlineProp.CustomInt02 > 0)
            {
                characterWidth = inlineProp.CustomInt01;
                interCharacterSpace = inlineProp.CustomInt02;
            }
            if (interCharacterSpace <= -1 || characterWidth <= -1)
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

            // field length in rasters can be calculated by multiplying the number of characters in the field by the width of the character in rasters (including the inter-character space), minus one inter-character space.
            int length = (numberOfCharacters * (characterWidth + interCharacterSpace)) - interCharacterSpace;
            return BitConverter.GetBytes((short)length);
        }

        public byte GetFieldHeightInDrops(LinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, LinxDataSetData dataSetData, short numberOfCharacters)
        {
            int height = -1;
            if (inlineProp.CustomInt03 > 0)
                height = inlineProp.CustomInt03;
            if (height <= -1)
                height = linxPrintJob.FieldHeightDrop;
            if (height <= -1 && dataSetData != null)
                height = dataSetData.Height;
            if (height <= -1)
                height = 5;
            return (byte)height;
        }
        #endregion

        #region Binary Serialization
        private string ByteStrPresentation(List<byte[]> downloadData)
        {
            return _shared.ByteStrPresentation(downloadData);
        }

        public byte[] GetData(LinxPrinterCommandCodeEnum commandCode, byte[] data)
        {
            return _shared.GetData((byte)commandCode, data);
        }

        public byte[] GetData(LinxASCIControlCharacterEnum commandCode, byte[] data)
        {
            return _shared.GetData((byte)commandCode, data);
        }

        public byte[] GetData(byte commandCode, byte[] data)
        {
            return _shared.GetData(commandCode, data);
        }

        #endregion
    }
}
