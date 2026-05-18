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
        /// TODO for scryber (InlinePropertyValueBase doesn't exist in core reporthandler, only in wpf implementation) - need to find workaround for this
        /// add to print queue
        /// text data
        /// DownloadTextValue == sending text to printer -> printer downloads text
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="text"></param>
        // private void AddTextValueToPrintMessage(ILinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, string aggregateGroup, string text)
        // {
        //     _shared.AddTextValueToPrintMessage(linxPrintJob, inlineProp, aggregateGroup, text);
        // }


        /// <summary>
        /// add to print queue
        /// barcode data
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineBarcode"></param>
        private void AddBarcodeValueToPrintMessage(ILinxPrintJob linxPrintJob, string aggregateGroup, string barcodeValue)
        {
            _shared.AddBarcodeValueToPrintMessage(linxPrintJob, aggregateGroup, barcodeValue);
        }

        #endregion

        #region Build Objects

        /// <summary>
        /// TODO for scryber (InlinePropertyValueBase doesn't exist in core reporthandler, only in wpf implementation) - need to find workaround for this
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineProp"></param>
        /// <param name="dataSet"></param>
        /// <param name="value"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        // public virtual LinxField GetLinxField(Encoding encoding, ILinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, LinxDataSetData dataSet, string value, byte fieldType = 0x00)
        // {
        //     return _shared.GetLinxField(encoding, linxPrintJob, inlineProp, dataSet, value, fieldType);
        // }

        /// <summary>
        /// TODO for scryber (InlinePropertyValueBase doesn't exist in core reporthandler, only in wpf implementation) - need to find workaround for this
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineProp"></param>
        /// <param name="valueLength"></param>
        /// <param name="valueByteLength"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        // public virtual LinxFieldHeader GetLinxFieldHeader(LinxDataSetData dataSet, ILinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, short valueLength, short valueByteLength, byte fieldType = 0x00)
        // {
        //     return _shared.GetLinxFieldHeader(dataSet, linxPrintJob, inlineProp, valueLength, valueByteLength, fieldType);
        // }

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

        /// <summary>
        /// TODO for scryber (InlinePropertyValueBase doesn't exist in core reporthandler, only in wpf implementation) - need to find workaround for this
        /// </summary>
        /// <param name="linxPrintJob"></param>
        /// <param name="inlineProp"></param>
        /// <param name="dataSetData"></param>
        /// <param name="numberOfCharacters"></param>
        /// <returns></returns>
        // public virtual byte[] GetFieldLengthInRasters(ILinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, LinxDataSetData dataSetData, short numberOfCharacters)
        // {
        //     return _shared.GetFieldLengthInRasters(linxPrintJob, inlineProp, dataSetData, numberOfCharacters);
        // }

        /// <summary>       
        /// TODO for scryber (InlinePropertyValueBase doesn't exist in core reporthandler, only in wpf implementation) - need to find workaround for this
        /// </summary>
        // public byte GetFieldHeightInDrops(ILinxPrintJob linxPrintJob, InlinePropertyValueBase inlineProp, LinxDataSetData dataSetData, short numberOfCharacters)
        // {
        //     return _shared.GetFieldHeightInDrops(linxPrintJob, inlineProp, dataSetData, numberOfCharacters);
        // }
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
