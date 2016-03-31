using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class MrSource : MrAbstractBlock
    {        
        public InputFile InputFile;
        

        public void Post(string value)
        {
            try
            {
                this.InputFile = new InputFile(value);

                this.Start();
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Блок чтения данных из файла {0}", value), exception);
            }
        }


        protected override void Start()
        {
            this.DoMainWork(BlockBody);
        }


        private void BlockBody()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Normal;

            PieceOf newPiece;
            while ((newPiece = this.InputFile.Fetch(inputFragmentSize: null)) != null && this.Status == ProjectStatusEnum.InProgress)
            {
                this.AddToBuffer(newPiece);
            }

            if (this.PostDone() != ProjectStatusEnum.Done)
            {
                throw new InvalidAsynchronousStateException(string.Format("После окончания считывания статус не был переведен в {0}", ProjectStatusEnum.Done));
            }
        }
    }
}
