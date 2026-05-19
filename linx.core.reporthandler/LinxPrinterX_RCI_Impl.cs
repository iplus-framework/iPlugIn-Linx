using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;


namespace linx.core.reporthandler
{
    public partial class LinxPrinterX
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


        protected void AddDeleteReportToJob(ILinxPrintJob linxPrintJob)
        {
            _shared.AddDeleteReportToJob(linxPrintJob);
        }


        /// <summary>
        /// Loading remote layout
        /// </summary>
        /// <param name="linxPrintJob"></param>
        private void AddPrintMessageToJob(ILinxPrintJob linxPrintJob)
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
        private void AddTextValueForRemoteField(ILinxPrintJob linxPrintJob, string name, string text)
        {
            _shared.AddTextValueForRemoteField(linxPrintJob, name, text);
        }


        /// <summary>
        /// Add one text field to a direct LINX message payload.
        /// </summary>
        private void AddTextValueToPrintMessage(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, string aggregateGroup, string text)
        {
            _shared.AddTextValueToPrintMessage(linxPrintJob, fieldOptions, aggregateGroup, text);
        }


        /// <summary>
        /// add to print queue
        /// barcode data
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineBarcode"></param>
        private void AddBarcodeValueToPrintMessage(ILinxPrintJob linxPrintJob, string aggregateGroup, string barcodeValue)
        {
            AddBarcodeValueToPrintMessage(linxPrintJob, null, aggregateGroup, barcodeValue);
        }


        /// <summary>
        /// Adds a direct LINX barcode field with explicit render options.
        /// </summary>
        private void AddBarcodeValueToPrintMessage(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, string aggregateGroup, string barcodeValue)
        {
            _shared.AddBarcodeValueToPrintMessage(linxPrintJob, fieldOptions, aggregateGroup, barcodeValue);
        }

        #endregion

        #region Build Objects

        public virtual LinxField GetLinxField(Encoding encoding, ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, LinxDataSetData dataSet, string value, byte fieldType = 0x00)
        {
            return _shared.GetLinxField(encoding, linxPrintJob, fieldOptions, dataSet, value, fieldType);
        }

        public virtual LinxFieldHeader GetLinxFieldHeader(LinxDataSetData dataSet, ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, short valueLength, short valueByteLength, byte fieldType = 0x00)
        {
            return _shared.GetLinxFieldHeader(dataSet, linxPrintJob, fieldOptions, valueLength, valueByteLength, fieldType);
        }

        public virtual LinxMessageHeader GetLinxMessageHeader(string messageName, string rasterName, short numOfMessages, short msgLengthInBytes, short msgLengthInRasters)
        {
            return _shared.GetLinxMessageHeader(messageName, rasterName, numOfMessages, msgLengthInBytes, msgLengthInRasters);
        }

        #endregion

        #region Calculation methods
        public List<LinxDataSetData> LoadDataSets()
        {
            return _shared.LoadDataSets();
        }

        public virtual byte[] GetFieldLengthInRasters(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, LinxDataSetData dataSetData, short numberOfCharacters)
        {
            return _shared.GetFieldLengthInRasters(linxPrintJob, fieldOptions, dataSetData, numberOfCharacters);
        }

        public byte GetFieldHeightInDrops(ILinxPrintJob linxPrintJob, LinxFieldRenderOptionsX fieldOptions, LinxDataSetData dataSetData, short numberOfCharacters)
        {
            return _shared.GetFieldHeightInDrops(linxPrintJob, fieldOptions, dataSetData, numberOfCharacters);
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
