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


        protected override void MainAction()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Normal;

            

            PieceOf newPiece;
            DateTime? now = null;
            while ((newPiece = this.InputFile.Fetch(inputFragmentSize: null)) != null && this.Status == ProjectStatusEnum.InProgress)
            {
                this.AddToBuffer((PieceOf)newPiece);
            }

            if (this.PostDone() != ProjectStatusEnum.Done)
            {
                throw new InvalidAsynchronousStateException(string.Format("После окончания считывания статус не был переведен в {0}", ProjectStatusEnum.Done));
            }

            this.InputFile.Dispose();
            this.InputFile = null;
        }
    }
}
