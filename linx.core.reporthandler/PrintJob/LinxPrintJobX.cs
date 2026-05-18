using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using gip.core.reporthandler;

namespace linx.core.reporthandler
{
    public interface ILinxPrintJob
    {
        Guid PrintJobID { get; set; }
        string Name { get; set; }
        DateTime InsertDate { get; set; }
        PrintJobStateEnum State { get; set; }
        byte[] Main { get; set; }
        Encoding Encoding { get; set; }
        int ColumnMultiplier { get; set; }
        int ColumnDivisor { get; set; }

        string RasterName { get; }
        int CharacterWidth { get; }
        bool IsOneLine { get; }
        int InterCharSpace { get; }
        int FieldHeightDrop { get; }

        List<Telegram> PacketsForPrint { get; }
        List<byte[]> RemoteFieldValues { get; }
        List<LinxField> LinxFields { get; set; }

        string GetJobInfo();
        void WriteTelegramsToFiles(string rootPath);
    }

    public class LinxPrintJobX : PrintJob, ILinxPrintJob
    {

        private string _rasterNameOverride;
        private int _characterWidthOverride;
        private bool _isOneLineOverride;
        private int _interCharSpaceOverride;
        private int _fieldHeightDropOverride;

        #region ctor's
        public LinxPrintJobX()
        {
        }


        #endregion

        #region Properies

        public string RasterName
        {
            get
            {
                return _rasterNameOverride;
            }
            set
            {
                _rasterNameOverride = value;
            }
        }

        public int CharacterWidth
        {
            get
            {
                return _characterWidthOverride;
            }
            set
            {
                _characterWidthOverride = value;
            }
        }

        public bool IsOneLine
        {
            get
            {
                return _isOneLineOverride;
            }
            set
            {
                _isOneLineOverride = value;
            }
        }

        public int InterCharSpace
        {
            get
            {
                return _interCharSpaceOverride;
            }
            set
            {
                _interCharSpaceOverride = value;
            }
        }

        public int FieldHeightDrop
        {
            get
            {
                return _fieldHeightDropOverride;
            }
            set
            {
                _fieldHeightDropOverride = value;
            }
        }

        public string RasterNameOverride
        {
            get { return _rasterNameOverride; }
            set { _rasterNameOverride = value; }
        }

        public int CharacterWidthOverride
        {
            get { return _characterWidthOverride; }
            set { _characterWidthOverride = value; }
        }

        public bool IsOneLineOverride
        {
            get { return _isOneLineOverride; }
            set { _isOneLineOverride = value; }
        }

        public int InterCharSpaceOverride
        {
            get { return _interCharSpaceOverride; }
            set { _interCharSpaceOverride = value; }
        }

        public int FieldHeightDropOverride
        {
            get { return _fieldHeightDropOverride; }
            set { _fieldHeightDropOverride = value; }
        }

        //public LinxPrintJobTypeEnum LinxPrintJobType { get; set; }


        private List<Telegram> _PacketsForPrint;
        public List<Telegram> PacketsForPrint
        {
            get
            {
                if (_PacketsForPrint == null)
                {
                    _PacketsForPrint = new List<Telegram>();
                }
                return _PacketsForPrint;
            }
        }


        private List<byte[]> _RemoteFieldValues = new List<byte[]>();
        public List<byte[]> RemoteFieldValues
        {
            get
            {
                return _RemoteFieldValues;
            }
        }

        public List<LinxField> LinxFields { get; set; } = new List<LinxField>();

        #endregion

        #region Methods

        public string GetJobInfo()
        {
            return $"PrintJobID: {PrintJobID} Name:{Name}";
        }

        public void WriteTelegramsToFiles(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
                throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            for (int i = 0; i < PacketsForPrint.Count; i++)
            {
                var telegram = PacketsForPrint[i];
                string filename = $"{timestamp}_{telegram.LinxPrintJobType}_{i:D3}.bin";
                string filePath = System.IO.Path.Combine(rootPath, filename);

                File.WriteAllBytes(filePath, telegram.Packet);
            }
        }

        #endregion
    }
}
