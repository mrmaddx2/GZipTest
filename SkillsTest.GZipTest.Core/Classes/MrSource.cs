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
        #region Delegates

        protected delegate void FillingActionHandler();
        #endregion

        
        protected InputFile inputFile;
        

        public void Post(string value)
        {
            try
            {
                this.inputFile = new InputFile(value);

                this.Start();
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Блок чтения данных из файла {0}", value), exception);
            }
        }


        private readonly object startDummy = new object();
        protected override void Start()
        {
            lock (startDummy)
            {
                try
                {
                    if (this.Status != ProjectStatusEnum.Unknown)
                    {
                        return;
                    }

                    this.Status = ProjectStatusEnum.InProgress;

                    FillingActionHandler fillingAction = this.Filling;

                    fillingAction.BeginInvoke(null, null);

                    this.PostStart();
                }
                catch (Exception exception)
                {
                    this.PostError(exception);
                }
            }
        }


        protected virtual void Filling()
        {
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                PieceOf newPiece;
                while ((newPiece = this.inputFile.Fetch(inputFragmentSize: null)) != null && this.Status == ProjectStatusEnum.InProgress)
                {
                    this.AddToBuffer(newPiece);
                }

                if (this.PostDone() != ProjectStatusEnum.Done)
                {
                    throw new InvalidAsynchronousStateException(string.Format("После окончания считывания статус не был переведен в {0}", ProjectStatusEnum.Done));
                }
            }
            catch (Exception exception)
            {
                this.PostError(exception);
            }
        }
    }
}
